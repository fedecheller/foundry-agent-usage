using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace FoundryAgentUsage;

#pragma warning disable OPENAI001

public partial class Form1 : Form
{
    private CancellationTokenSource? _cts;
    private readonly IConfiguration _configuration;
    private readonly FoundryAgentSettings _settings;
    private AIProjectClient? _projectClient;
    private ProjectResponsesClient? _responseClient;
    private string? _previousResponseId;
    private bool _isSending;

    public Form1()
    {
        InitializeComponent();

        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        _settings = _configuration.GetSection("FoundryAgent").Get<FoundryAgentSettings>() ?? new FoundryAgentSettings();

        Load += Form1_Load;
        FormClosing += Form1_FormClosing;
    }

    private async void Form1_Load(object? sender, EventArgs e)
    {
        SetStatus("Connecting to Foundry Agent…");
        SetInputEnabled(false);
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint) ||
                string.IsNullOrWhiteSpace(_settings.AgentName) ||
                string.IsNullOrWhiteSpace(_settings.AgentVersion))
            {
                throw new InvalidOperationException("Missing FoundryAgent settings. Please configure Endpoint, AgentName, and AgentVersion in appsettings.json.");
            }

            _projectClient = new AIProjectClient(new Uri(_settings.Endpoint), new DefaultAzureCredential());
            _responseClient = _projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(
                new AgentReference(name: _settings.AgentName, version: _settings.AgentVersion));

            AppendMessage("System", "Connected. Ask anything. Local tool support is enabled for function: sample.");
            SetStatus("Connected");
            SetInputEnabled(true);
            txtInput.Focus();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            AppendMessage("System", $"Failed to connect: {ex.Message}");
        }
    }

    private async void BtnSend_Click(object? sender, EventArgs e) => await SendMessageAsync();

    private async void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift && txtInput.Focused)
        {
            e.SuppressKeyPress = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        if (_isSending)
            return;
        if (_responseClient is null)
        {
            SetStatus("Not connected");
            return;
        }

        var userText = txtInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(userText))
            return;

        _isSending = true;
        _cts = new CancellationTokenSource();
        SetInputEnabled(false);
        SetStatus("Sending…");

        AppendMessage("You", userText);
        txtInput.Clear();
        AppendMessage("Assistant", string.Empty);

        try
        {
            await foreach (var item in StreamFoundryAgentMessageAsync(_previousResponseId, userText, _cts.Token))
            {
                if (!string.IsNullOrEmpty(item.Text))
                    AppendToCurrentAssistantLine(item.Text);

                if (!string.IsNullOrWhiteSpace(item.ResponseId))
                    _previousResponseId = item.ResponseId;

                if (!string.IsNullOrWhiteSpace(item.Error))
                {
                    AppendToCurrentAssistantLine($"\r\n[Error] {item.Error}");
                    SetStatus("Error");
                    return;
                }
            }

            AppendToCurrentAssistantLine(Environment.NewLine + Environment.NewLine);
            SetStatus("Ready");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Canceled");
        }
        catch (Exception ex)
        {
            AppendToCurrentAssistantLine($"\r\n[Error] {ex.Message}{Environment.NewLine}{Environment.NewLine}");
            SetStatus("Error");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _isSending = false;
            SetInputEnabled(true);
            txtInput.Focus();
        }
    }

    private void AppendMessage(string sender, string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendMessage(sender, text));
            return;
        }

        txtChat.AppendText($"{sender}: {text}{Environment.NewLine}");
        txtChat.SelectionStart = txtChat.TextLength;
        txtChat.ScrollToCaret();
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text));
            return;
        }

        lblStatus.Text = text;
    }

    private void SetInputEnabled(bool enabled)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetInputEnabled(enabled));
            return;
        }

        txtInput.Enabled = enabled;
        btnSend.Enabled = enabled;
    }

    private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            await Task.Yield();
        }
    }

    private void AppendToCurrentAssistantLine(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendToCurrentAssistantLine(text));
            return;
        }

        txtChat.AppendText(text);
        txtChat.SelectionStart = txtChat.TextLength;
        txtChat.ScrollToCaret();
    }

    private async IAsyncEnumerable<FoundryStreamingEvent> StreamFoundryAgentMessageAsync(
        string? previousResponseId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (_responseClient is null)
            yield break;

        var options = CreateInitialResponseOptions(previousResponseId, userMessage);

        while (true)
        {
            ResponseResult? response = null;

            await foreach (var update in _responseClient.CreateResponseStreamingAsync(options).WithCancellation(ct))
            {
                if (update is StreamingResponseOutputTextDeltaUpdate textDelta && !string.IsNullOrEmpty(textDelta.Delta))
                {
                    yield return new FoundryStreamingEvent(textDelta.Delta, null, null);
                }
                else if (update is StreamingResponseCompletedUpdate completedUpdate)
                {
                    response = completedUpdate.Response;
                }
                else if (update is StreamingResponseErrorUpdate errorUpdate)
                {
                    yield return new FoundryStreamingEvent(null, null, errorUpdate.Message);
                    yield break;
                }
            }

            if (response is null)
            {
                yield return new FoundryStreamingEvent(null, null, "No response received from Foundry agent.");
                yield break;
            }

            if (response.Status != ResponseStatus.Completed)
            {
                yield return new FoundryStreamingEvent(null, response.Id, $"Response ended with status {response.Status}.");
                yield break;
            }

            var functionCalls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();
            if (functionCalls.Count == 0)
            {
                yield return new FoundryStreamingEvent(null, response.Id, null);
                yield break;
            }

            var toolOutputs = ExecuteToolCalls(functionCalls);
            options = CreateFollowUpOptions(response.Id, toolOutputs);
        }
    }

    private CreateResponseOptions CreateInitialResponseOptions(string? previousResponseId, string userMessage)
    {
        var options = new CreateResponseOptions();
        if (!string.IsNullOrWhiteSpace(previousResponseId))
            options.PreviousResponseId = previousResponseId;

        options.InputItems.Add(ResponseItem.CreateUserMessageItem(userMessage));

        if (_settings.MaxCompletionTokens > 0)
            options.MaxOutputTokenCount = _settings.MaxCompletionTokens;

        return options;
    }

    private CreateResponseOptions CreateFollowUpOptions(string previousResponseId, IReadOnlyList<ResponseItem> toolOutputs)
    {
        var options = new CreateResponseOptions
        {
            PreviousResponseId = previousResponseId
        };

        if (_settings.MaxCompletionTokens > 0)
            options.MaxOutputTokenCount = _settings.MaxCompletionTokens;

        foreach (var output in toolOutputs)
            options.InputItems.Add(output);

        return options;
    }

    private static List<ResponseItem> ExecuteToolCalls(IReadOnlyList<FunctionCallResponseItem> toolCalls)
    {
        var outputs = new List<ResponseItem>(toolCalls.Count);

        foreach (var toolCall in toolCalls)
        {
            if (!string.Equals(toolCall.FunctionName, "sample", StringComparison.OrdinalIgnoreCase))
            {
                outputs.Add(ResponseItem.CreateFunctionCallOutputItem(toolCall.CallId, "{\"error\":\"Only tool 'sample' is allowed.\"}"));
                continue;
            }

            try
            {
                var result = ExecuteSample(toolCall.FunctionArguments.ToString());
                outputs.Add(ResponseItem.CreateFunctionCallOutputItem(toolCall.CallId, result));
            }
            catch (Exception ex)
            {
                var error = JsonSerializer.Serialize(new { error = ex.Message });
                outputs.Add(ResponseItem.CreateFunctionCallOutputItem(toolCall.CallId, error));
            }
        }

        return outputs;
    }

    private static string ExecuteSample(string arguments)
    {
        string input = string.Empty;

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            using var doc = JsonDocument.Parse(arguments);
            var root = doc.RootElement;
            input =
                (root.TryGetProperty("input", out var i) ? i.GetString() : null) ??
                (root.TryGetProperty("message", out var m) ? m.GetString() : null) ??
                (root.TryGetProperty("value", out var v) ? v.GetString() : null) ??
                string.Empty;
        }

        return JsonSerializer.Serialize(new
        {
            tool = "sample",
            echoedInput = input,
            timestampUtc = DateTime.UtcNow
        });
    }
}

public readonly record struct FoundryStreamingEvent(string? Text, string? ResponseId, string? Error);
