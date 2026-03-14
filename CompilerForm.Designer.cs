using System.ComponentModel;
using System.Windows.Forms;
using ScintillaNET;

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

            /// <summary>
            /// Required method for Designer support - do not modify
            /// the contents of this method with the code editor.
            /// </summary>
            private void InitializeComponent()
            {
                menuStrip = new System.Windows.Forms.MenuStrip();
                fileMenu = new System.Windows.Forms.ToolStripMenuItem();
                newMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
                exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                compileButton = new System.Windows.Forms.Button();
                runButton = new System.Windows.Forms.Button();
                refactorButton = new System.Windows.Forms.Button();
                splitContainer = new System.Windows.Forms.SplitContainer();
                // Replaced RichTextBox with ScintillaNET editor for MVP
                codeTextBox = new ScintillaNET.Scintilla();
                outputTextBox = new System.Windows.Forms.RichTextBox();
                button1 = new System.Windows.Forms.Button();
                button2 = new System.Windows.Forms.Button();
                button3 = new System.Windows.Forms.Button();
                button4 = new System.Windows.Forms.Button();
                menuStrip.SuspendLayout();
                ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
                splitContainer.Panel1.SuspendLayout();
                splitContainer.Panel2.SuspendLayout();
                splitContainer.SuspendLayout();
                SuspendLayout();
                // 
                // menuStrip
                // 
                menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileMenu });
                menuStrip.Location = new System.Drawing.Point(0, 0);
                menuStrip.Name = "menuStrip";
                menuStrip.Padding = new System.Windows.Forms.Padding(8, 3, 0, 3);
                menuStrip.Size = new System.Drawing.Size(1067, 29);
                menuStrip.TabIndex = 0;
                // 
                // fileMenu
                // 
                fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newMenuItem, openMenuItem, saveMenuItem, toolStripSeparator1, exitMenuItem });
                fileMenu.Name = "fileMenu";
                fileMenu.Size = new System.Drawing.Size(53, 23);
                fileMenu.Text = "Файл";
                // 
                // newMenuItem
                // 
                newMenuItem.Name = "newMenuItem";
                newMenuItem.Size = new System.Drawing.Size(145, 24);
                newMenuItem.Text = "Новый";
                newMenuItem.Click += NewMenuItem_Click;
                // 
                // openMenuItem
                // 
                openMenuItem.Name = "openMenuItem";
                openMenuItem.Size = new System.Drawing.Size(145, 24);
                openMenuItem.Text = "Открыть";
                openMenuItem.Click += OpenMenuItem_Click;
                // 
                // saveMenuItem
                // 
                saveMenuItem.Name = "saveMenuItem";
                saveMenuItem.Size = new System.Drawing.Size(145, 24);
                saveMenuItem.Text = "Сохранить";
                saveMenuItem.Click += SaveMenuItem_Click;
                // 
                // toolStripSeparator1
                // 
                toolStripSeparator1.Name = "toolStripSeparator1";
                toolStripSeparator1.Size = new System.Drawing.Size(142, 6);
                // 
                // exitMenuItem
                // 
                exitMenuItem.Name = "exitMenuItem";
                exitMenuItem.Size = new System.Drawing.Size(145, 24);
                exitMenuItem.Text = "Выход";
                // 
                // compileButton
                // 
                compileButton.Location = new System.Drawing.Point(16, 44);
                compileButton.Margin = new System.Windows.Forms.Padding(4);
                compileButton.Name = "compileButton";
                compileButton.Size = new System.Drawing.Size(133, 44);
                compileButton.TabIndex = 1;
                compileButton.Text = "Компиляция";
                compileButton.UseVisualStyleBackColor = true;
                compileButton.Click += CompileButton_Click;
                // 
                // runButton
                // 
                runButton.Location = new System.Drawing.Point(157, 44);
                runButton.Margin = new System.Windows.Forms.Padding(4);
                runButton.Name = "runButton";
                runButton.Size = new System.Drawing.Size(133, 44);
                runButton.TabIndex = 2;
                runButton.Text = "Запуск";
                runButton.UseVisualStyleBackColor = true;
                runButton.Click += RunButton_Click;
                // 
                // refactorButton
                // 
                refactorButton.Location = new System.Drawing.Point(299, 44);
                refactorButton.Margin = new System.Windows.Forms.Padding(4);
                refactorButton.Name = "refactorButton";
                refactorButton.Size = new System.Drawing.Size(133, 44);
                refactorButton.TabIndex = 3;
                refactorButton.Text = "Форматирование кода";
                refactorButton.UseVisualStyleBackColor = true;
                refactorButton.Click += RefactorButton_Click;
                // 
                // splitContainer
                // 
                splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
                splitContainer.Location = new System.Drawing.Point(16, 102);
                splitContainer.Margin = new System.Windows.Forms.Padding(4);
                splitContainer.Name = "splitContainer";
                splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
                // 
                // splitContainer.Panel1
                // 
                splitContainer.Panel1.Controls.Add(codeTextBox);
                // 
                // splitContainer.Panel2
                // 
                splitContainer.Panel2.Controls.Add(outputTextBox);
                splitContainer.Size = new System.Drawing.Size(1035, 538);
                splitContainer.SplitterDistance = 377;
                splitContainer.SplitterWidth = 6;
                splitContainer.TabIndex = 4;
                // 
                // codeTextBox
                // 
                codeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
                codeTextBox.Font = new System.Drawing.Font("Consolas", 10F);
                codeTextBox.Location = new System.Drawing.Point(0, 0);
                codeTextBox.Margin = new System.Windows.Forms.Padding(4);
                codeTextBox.Name = "codeTextBox";
                codeTextBox.Size = new System.Drawing.Size(1035, 377);
                codeTextBox.TabIndex = 0;
                codeTextBox.Text = "";
                // 
                // outputTextBox
                // 
                outputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
                outputTextBox.Font = new System.Drawing.Font("Consolas", 10F);
                outputTextBox.Location = new System.Drawing.Point(0, 0);
                outputTextBox.Margin = new System.Windows.Forms.Padding(4);
                outputTextBox.Name = "outputTextBox";
                outputTextBox.Size = new System.Drawing.Size(1035, 155);
                outputTextBox.TabIndex = 0;
                outputTextBox.Text = "";
                
                #if DEBUG
                // 
                // button1
                // 
                
                button1.Location = new System.Drawing.Point(569, 44);
                button1.Margin = new System.Windows.Forms.Padding(4);
                button1.Name = "button1";
                button1.Size = new System.Drawing.Size(133, 44);
                button1.TabIndex = 5;
                button1.Text = "VM DEBUG";
                button1.UseVisualStyleBackColor = true;
                button1.Click += button1_Click;
                // 
                // button2
                // 
                button2.Location = new System.Drawing.Point(710, 44);
                button2.Margin = new System.Windows.Forms.Padding(4);
                button2.Name = "button2";
                button2.Size = new System.Drawing.Size(82, 44);
                button2.TabIndex = 6;
                button2.Text = "RUN";
                button2.UseVisualStyleBackColor = true;
                button2.Click += button2_Click;
                // 
                // button3
                // 
                button3.Location = new System.Drawing.Point(800, 44);
                button3.Margin = new System.Windows.Forms.Padding(4);
                button3.Name = "button3";
                button3.Size = new System.Drawing.Size(82, 44);
                button3.TabIndex = 7;
                button3.Text = "STEP";
                button3.UseVisualStyleBackColor = true;
                button3.Click += button3_Click;
                // 
                // button4
                // 
                button4.Location = new System.Drawing.Point(890, 44);
                button4.Margin = new System.Windows.Forms.Padding(4);
                button4.Name = "button4";
                button4.Size = new System.Drawing.Size(82, 44);
                button4.TabIndex = 8;
                button4.Text = "STOP";
                button4.UseVisualStyleBackColor = true;
                button4.Click += button4_Click;
                #endif
                // 
                // CompilerForm
                // 
                AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
                AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                ClientSize = new System.Drawing.Size(1067, 658);
                Controls.Add(button4);
                Controls.Add(button3);
                Controls.Add(button2);
                Controls.Add(button1);
                Controls.Add(splitContainer);
                Controls.Add(refactorButton);
                Controls.Add(runButton);
                Controls.Add(compileButton);
                Controls.Add(menuStrip);
                MainMenuStrip = menuStrip;
                Margin = new System.Windows.Forms.Padding(4);
                Text = "Компилятор";
                menuStrip.ResumeLayout(false);
                menuStrip.PerformLayout();
                splitContainer.Panel1.ResumeLayout(false);
                splitContainer.Panel2.ResumeLayout(false);
                ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
                splitContainer.ResumeLayout(false);
                ResumeLayout(false);
                PerformLayout();
            }

            private System.Windows.Forms.Button button1;
            private System.Windows.Forms.Button button2;
            private System.Windows.Forms.Button button3;
            private System.Windows.Forms.Button button4;

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
            private System.Windows.Forms.Button refactorButton;
            private SplitContainer splitContainer;
            private Scintilla codeTextBox;
            private RichTextBox outputTextBox;
        
        
}
