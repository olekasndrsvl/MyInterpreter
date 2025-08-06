using System.ComponentModel;

namespace MyInterpreter;

partial class CompilerForm
{
     private System.ComponentModel.IContainer components = null;
     protected override void Dispose(bool disposing)
            {
                if (disposing && (components != null))
                {
                    components.Dispose();
                }
                base.Dispose(disposing);
            }
    
            private void InitializeComponent()
            {
                this.menuStrip = new System.Windows.Forms.MenuStrip();
                this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
                this.newMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
                this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                this.compileButton = new System.Windows.Forms.Button();
                this.runButton = new System.Windows.Forms.Button();
                this.refactorButton = new System.Windows.Forms.Button();
                this.splitContainer = new System.Windows.Forms.SplitContainer();
                this.codeTextBox = new System.Windows.Forms.RichTextBox();
                this.outputTextBox = new System.Windows.Forms.RichTextBox();
                this.menuStrip.SuspendLayout();
                ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
                this.splitContainer.Panel1.SuspendLayout();
                this.splitContainer.Panel2.SuspendLayout();
                this.splitContainer.SuspendLayout();
                this.SuspendLayout();
                // 
                // menuStrip
                // 
                this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileMenu});
                this.menuStrip.Location = new System.Drawing.Point(0, 0);
                this.menuStrip.Name = "menuStrip";
                this.menuStrip.Size = new System.Drawing.Size(800, 24);
                this.menuStrip.TabIndex = 0;
                // 
                // fileMenu
                // 
                this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.newMenuItem,
                this.openMenuItem,
                this.saveMenuItem,
                this.toolStripSeparator1,
                this.exitMenuItem});
                this.fileMenu.Name = "fileMenu";
                this.fileMenu.Size = new System.Drawing.Size(48, 20);
                this.fileMenu.Text = "Файл";
                // 
                // newMenuItem
                // 
                this.newMenuItem.Name = "newMenuItem";
                this.newMenuItem.Size = new System.Drawing.Size(180, 22);
                this.newMenuItem.Text = "Новый";
                this.newMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
                // 
                // openMenuItem
                // 
                this.openMenuItem.Name = "openMenuItem";
                this.openMenuItem.Size = new System.Drawing.Size(180, 22);
                this.openMenuItem.Text = "Открыть";
                this.openMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
                // 
                // saveMenuItem
                // 
                this.saveMenuItem.Name = "saveMenuItem";
                this.saveMenuItem.Size = new System.Drawing.Size(180, 22);
                this.saveMenuItem.Text = "Сохранить";
                this.saveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
                // 
                // toolStripSeparator1
                // 
                this.toolStripSeparator1.Name = "toolStripSeparator1";
                this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
                // 
                // exitMenuItem
                // 
                this.exitMenuItem.Name = "exitMenuItem";
                this.exitMenuItem.Size = new System.Drawing.Size(180, 22);
                this.exitMenuItem.Text = "Выход";
                //this.exitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
                // 
                // compileButton
                // 
                this.compileButton.Location = new System.Drawing.Point(12, 30);
                this.compileButton.Name = "compileButton";
                this.compileButton.Size = new System.Drawing.Size(100, 30);
                this.compileButton.TabIndex = 1;
                this.compileButton.Text = "Компиляция";
                this.compileButton.UseVisualStyleBackColor = true;
                this.compileButton.Click += new System.EventHandler(this.CompileButton_Click);
                // 
                // runButton
                // 
                this.runButton.Location = new System.Drawing.Point(118, 30);
                this.runButton.Name = "runButton";
                this.runButton.Size = new System.Drawing.Size(100, 30);
                this.runButton.TabIndex = 2;
                this.runButton.Text = "Запуск";
                this.runButton.UseVisualStyleBackColor = true;
                this.runButton.Click += new System.EventHandler(this.RunButton_Click);
                // 
                // refactorButton
                // 
                this.refactorButton.Location = new System.Drawing.Point(224, 30);
                this.refactorButton.Name = "refactorButton";
                this.refactorButton.Size = new System.Drawing.Size(100, 30);
                this.refactorButton.TabIndex = 3;
                this.refactorButton.Text = "Рефакторинг";
                this.refactorButton.UseVisualStyleBackColor = true;
                this.refactorButton.Click += new System.EventHandler(this.RefactorButton_Click);
                // 
                // splitContainer
                // 
                this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
                this.splitContainer.Location = new System.Drawing.Point(12, 70);
                this.splitContainer.Name = "splitContainer";
                this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
                // 
                // splitContainer.Panel1
                // 
                this.splitContainer.Panel1.Controls.Add(this.codeTextBox);
                // 
                // splitContainer.Panel2
                // 
                this.splitContainer.Panel2.Controls.Add(this.outputTextBox);
                this.splitContainer.Size = new System.Drawing.Size(776, 368);
                this.splitContainer.SplitterDistance = 258;
                this.splitContainer.TabIndex = 4;
                // 
                // codeTextBox
                // 
                this.codeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
                this.codeTextBox.Font = new System.Drawing.Font("Consolas", 10F);
                this.codeTextBox.Location = new System.Drawing.Point(0, 0);
                this.codeTextBox.Name = "codeTextBox";
                this.codeTextBox.Size = new System.Drawing.Size(776, 258);
                this.codeTextBox.TabIndex = 0;
                this.codeTextBox.Text = "";
                // 
                // outputTextBox
                // 
                this.outputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
                this.outputTextBox.Font = new System.Drawing.Font("Consolas", 10F);
                this.outputTextBox.Location = new System.Drawing.Point(0, 0);
                this.outputTextBox.Name = "outputTextBox";
                this.outputTextBox.Size = new System.Drawing.Size(776, 106);
                this.outputTextBox.TabIndex = 0;
                this.outputTextBox.Text = "";
                // 
                // MainForm
                // 
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.ClientSize = new System.Drawing.Size(800, 450);
                this.Controls.Add(this.splitContainer);
                this.Controls.Add(this.refactorButton);
                this.Controls.Add(this.runButton);
                this.Controls.Add(this.compileButton);
                this.Controls.Add(this.menuStrip);
                this.MainMenuStrip = this.menuStrip;
                this.Name = "MainForm";
                this.Text = "Компилятор";
                //this.Load += new System.EventHandler(this.MainForm_Load);
                this.menuStrip.ResumeLayout(false);
                this.menuStrip.PerformLayout();
                this.splitContainer.Panel1.ResumeLayout(false);
                this.splitContainer.Panel2.ResumeLayout(false);
                ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
                this.splitContainer.ResumeLayout(false);
                this.ResumeLayout(false);
                this.PerformLayout();
    
            }
    
            //#endregion
    
            private MenuStrip menuStrip;
            private ToolStripMenuItem fileMenu;
            private ToolStripMenuItem newMenuItem;
            private ToolStripMenuItem openMenuItem;
            private ToolStripMenuItem saveMenuItem;
            private ToolStripSeparator toolStripSeparator1;
            private ToolStripMenuItem exitMenuItem;
            private Button compileButton;
            private Button runButton;
            private Button refactorButton;
            private SplitContainer splitContainer;
            private RichTextBox codeTextBox;
            private RichTextBox outputTextBox;
        
        
}
