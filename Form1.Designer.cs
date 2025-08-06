using System.ComponentModel;

namespace MyInterpreter;

partial class Form1
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        textBox1 = new System.Windows.Forms.TextBox();
        CompileButton = new System.Windows.Forms.Button();
        SuspendLayout();
        // 
        // textBox1
        // 
        textBox1.Dock = System.Windows.Forms.DockStyle.Top;
        textBox1.Location = new System.Drawing.Point(0, 0);
        textBox1.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
        textBox1.Multiline = true;
        textBox1.Name = "textBox1";
        textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        textBox1.Size = new System.Drawing.Size(784, 374);
        textBox1.TabIndex = 0;
        // 
        // CompileButton
        // 
        CompileButton.Location = new System.Drawing.Point(0, 380);
        CompileButton.Name = "CompileButton";
        CompileButton.Size = new System.Drawing.Size(131, 34);
        CompileButton.TabIndex = 1;
        CompileButton.Text = "Скомпилировать!";
        CompileButton.UseVisualStyleBackColor = true;
        CompileButton.Click += CompileButton_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        AutoSize = true;
        ClientSize = new System.Drawing.Size(784, 538);
        Controls.Add(CompileButton);
        Controls.Add(textBox1);
        Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        Text = "Form1";
        WindowState = System.Windows.Forms.FormWindowState.Minimized;
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button CompileButton;

    private System.Windows.Forms.TextBox textBox1;

    #endregion
}