using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MyInterpreter
{
    public partial class CompilerForm : Form
    {
        public CompilerForm()
        {
            InitializeComponent();
        }

      private void MainForm_Load(object sender, EventArgs e)
             {
                 outputTextBox.BackColor = Color.LightGray;
                 outputTextBox.ReadOnly = true;
             }
     
             // Меню: Новый файл
             private void NewMenuItem_Click(object sender, EventArgs e)
             {
                 codeTextBox.Clear();
                 outputTextBox.Clear();
             }
     
             // Меню: Открыть файл
             private void OpenMenuItem_Click(object sender, EventArgs e)
             {
                 using (var openDialog = new OpenFileDialog())
                 {
                     openDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                     if (openDialog.ShowDialog() == DialogResult.OK)
                     {
                         codeTextBox.Text = File.ReadAllText(openDialog.FileName);
                     }
                 }
             }
     
             // Меню: Сохранить файл
             private void SaveMenuItem_Click(object sender, EventArgs e)
             {
                 using (var saveDialog = new SaveFileDialog())
                 {
                     saveDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                     if (saveDialog.ShowDialog() == DialogResult.OK)
                     {
                         File.WriteAllText(saveDialog.FileName, codeTextBox.Text);
                     }
                 }
             }
     
             // Кнопка компиляции
             private void CompileButton_Click(object sender, EventArgs e)
             {
                 Lexer lex =new Lexer(codeTextBox.Text);
             
                 
                 try
                 {
                     Parser parser = new Parser(lex);
                     var progr = parser.MainProgram();
                     outputTextBox.Text = "Компиляция завершена! Ошибок: 0";
                     
                 }
                 catch (ComplierExceptions.BaseCompilerException ex)
                 {
                     outputTextBox.Text = ComplierExceptions.OutPutError(ex.GetType().ToString(), ex, lex.Lines);
                     //  MessageBox.Show();
                 }

             }
     
             // Кнопка запуска
             private void RunButton_Click(object sender, EventArgs e)
             {
                 Lexer lex =new Lexer(codeTextBox.Text);
             
                 
                 try
                 {
                     Parser parser = new Parser(lex);
                     var progr = parser.MainProgram();
                     progr.Execute();
                     outputTextBox.Text = "Компиляция завершена! Ошибок: 0";
                     
                 }
                 catch (ComplierExceptions.BaseCompilerException ex)
                 {
                     outputTextBox.Text = ComplierExceptions.OutPutError(ex.GetType().ToString(), ex, lex.Lines);
                     //  MessageBox.Show();
                 }
                 
             }
     
             // Кнопка рефакторинга
             private void RefactorButton_Click(object sender, EventArgs e)
             {
                 outputTextBox.Text = "Код отрефакторен!";
             }
    }
}