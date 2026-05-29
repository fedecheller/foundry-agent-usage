namespace FoundryAgentUsage;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        txtChat = new RichTextBox();
        txtInput = new TextBox();
        btnSend = new Button();
        lblStatus = new Label();
        tableLayout = new TableLayoutPanel();
        tableLayout.SuspendLayout();
        SuspendLayout();
        // 
        // txtChat
        // 
        txtChat.BackColor = Color.White;
        tableLayout.SetColumnSpan(txtChat, 2);
        txtChat.Dock = DockStyle.Fill;
        txtChat.Font = new Font("Segoe UI", 10F);
        txtChat.Location = new Point(3, 4);
        txtChat.Margin = new Padding(3, 4, 3, 4);
        txtChat.Name = "txtChat";
        txtChat.ReadOnly = true;
        txtChat.ScrollBars = RichTextBoxScrollBars.Vertical;
        txtChat.Size = new Size(794, 582);
        txtChat.TabIndex = 0;
        txtChat.Text = "";
        // 
        // txtInput
        // 
        txtInput.Dock = DockStyle.Fill;
        txtInput.Font = new Font("Segoe UI", 10F);
        txtInput.Location = new Point(3, 597);
        txtInput.Margin = new Padding(3, 7, 3, 4);
        txtInput.Name = "txtInput";
        txtInput.Size = new Size(691, 30);
        txtInput.TabIndex = 1;
        // 
        // btnSend
        // 
        btnSend.Dock = DockStyle.Fill;
        btnSend.Font = new Font("Segoe UI", 10F);
        btnSend.Location = new Point(697, 597);
        btnSend.Margin = new Padding(0, 7, 3, 4);
        btnSend.Name = "btnSend";
        btnSend.Size = new Size(100, 37);
        btnSend.TabIndex = 2;
        btnSend.Text = "Send";
        btnSend.Click += BtnSend_Click;
        // 
        // lblStatus
        // 
        tableLayout.SetColumnSpan(lblStatus, 2);
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Font = new Font("Segoe UI", 8F);
        lblStatus.ForeColor = Color.Gray;
        lblStatus.Location = new Point(3, 638);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(794, 29);
        lblStatus.TabIndex = 3;
        lblStatus.Text = "Ready";
        // 
        // tableLayout
        // 
        tableLayout.ColumnCount = 2;
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 103F));
        tableLayout.Controls.Add(txtChat, 0, 0);
        tableLayout.Controls.Add(txtInput, 0, 1);
        tableLayout.Controls.Add(btnSend, 1, 1);
        tableLayout.Controls.Add(lblStatus, 0, 2);
        tableLayout.Dock = DockStyle.Fill;
        tableLayout.Location = new Point(0, 0);
        tableLayout.Margin = new Padding(3, 4, 3, 4);
        tableLayout.Name = "tableLayout";
        tableLayout.RowCount = 3;
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29F));
        tableLayout.Size = new Size(800, 667);
        tableLayout.TabIndex = 0;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 667);
        Controls.Add(tableLayout);
        KeyPreview = true;
        Margin = new Padding(3, 4, 3, 4);
        MinimumSize = new Size(569, 451);
        Name = "Form1";
        Text = "Foundry Agent Chatbot";
        KeyDown += Form1_KeyDown;
        tableLayout.ResumeLayout(false);
        tableLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private RichTextBox txtChat;
    private TextBox txtInput;
    private Button btnSend;
    private Label lblStatus;
    private TableLayoutPanel tableLayout;
}
