using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static MyInterpreter.FormatCodeVisitor;
using System.Threading;
using MyInterpreter.Common;
using MyInterpreter.SemanticCheck;


namespace MyInterpreter
{
    public partial class CompilerForm : Form
    {
        public static CompilerForm Instance;
        public CompilerForm()
        {
            InitializeComponent();
            Instance = this;
        }

      
        public void ChangeOutputBoxText(string text)
        {
         //   outputTextBox.Text = text;
            if (outputTextBox.InvokeRequired)
            {
                outputTextBox.Invoke(new Action<string>(ChangeOutputBoxText), outputTextBox.Text+text);
            }
            else
            {
                outputTextBox.Text = outputTextBox.Text+ text;
            }
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
                 SymbolTable.ResetSymbolTable();
                 outputTextBox.Clear();
                 try
                 {
                     SymbolTable.ResetSymbolTable();
                     Parser parser = new Parser(lex);
                     var progr = parser.MainProgram();
                     var sv = new SemanticCheckVisitor();
                     progr.VisitP(sv);
                  
                     outputTextBox.Text = "Компиляция завершена! Ошибок: 0 \n";
                     
                 }
                 catch (CompilerExceptions.BaseCompilerException ex)
                 {
                     outputTextBox.Text = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
                     //  MessageBox.Show();
                 }
                 

             }
     
             // Кнопка запуска
             private async void RunButton_Click(object sender, EventArgs e)
             {
                 Lexer lex =new Lexer(codeTextBox.Text);
                 outputTextBox.Clear();
                 try
                 {
                     SymbolTable.ResetSymbolTable();
                     SemanticCheckVisitor.Reset();
                     
                     
                     Parser parser = new Parser(lex);
                     var progr = parser.MainProgram();
                  
                     var  sv = new SemanticCheckVisitor();
                     progr.VisitP(sv);
                     
                     
                     SymbolTable.PrintFunctionTable();
                     var frame_gen = new FrameSizeVisitor();
                     progr.VisitP(frame_gen);
                     
                     var gen = new ThreeAddressCodeVisitor();
                     gen.GiveFrameSizes(frame_gen.GetFrameSizes());
                     progr.VisitP(gen);
                    
    
                     var framesize = frame_gen.GetFrameSizes();
                     
                     VirtualMachine.GiveFrameSize(frame_gen.GetFrameSizes());
                     var code = gen.GetCode();
                     VirtualMachine.LoadProgram(code);
                     VirtualMachine.MemoryDump(1000);
                    
                     var sw = new Stopwatch();
                     sw.Start();
                     VirtualMachine.Run();
                     sw.Stop();
                     foreach (var VARIABLE in frame_gen.GetFrameSizes())
                     {
                         Console.WriteLine(VARIABLE.Key+" "+VARIABLE.Value);
                     }
                     
                     foreach (var VARIABLE in gen._currentTempIndexes)
                     {
                         Console.WriteLine(VARIABLE.Key+" "+VARIABLE.Value);
                     }
                     
                     CompilerForm.Instance.ChangeOutputBoxText($"Programm elapsed time: {sw.Elapsed}\n"); // Здесь логируем
                     // var sw1 = new Stopwatch();
                     // sw1.Start();
                     // var x = 1;
                     // while (x < 100000000)
                     // {
                     //     x += 1;
                     //     if(x<1000 && x>1)
                     //         Console.WriteLine(x);
                     // }
                     //
                     // Console.WriteLine(sw1.Elapsed);
                     // sw1.Stop();
                     //
                     
                     
                    // outputTextBox.Text = "Компиляция завершена! Ошибок: 0 \n";
                     VirtualMachine.MemoryDump(1000);
                     //await RunProgramm(rooti);
                     VirtualMachine.ResetVirtualMachine();
                     
                     
                     
                 }
                 catch (CompilerExceptions.BaseCompilerException ex)
                 {
                     outputTextBox.Text = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
                     //  MessageBox.Show();
                 }
                 
             }

            
             
             // Кнопка рефакторинга
             private void RefactorButton_Click(object sender, EventArgs e)
             {
                 Lexer lex =new Lexer(codeTextBox.Text);
                 SymbolTable.ResetSymbolTable();
               
                 try
                 {
                     Parser parser = new Parser(lex);
                     var progr = parser.MainProgram();
                     var pp = new FormatCodeVisitor();
                     codeTextBox.Text = progr.Visit(pp);
                     //Console.WriteLine(progr.Visit(pp));
                     outputTextBox.Text = "Код отформатирован!";
                 }
                 catch (CompilerExceptions.LexerException ex)
                 {
                     outputTextBox.Text = "Lex ERROR:" + CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
                 }
                 catch (CompilerExceptions.BaseCompilerException ex)
                 {
                     outputTextBox.Text = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
                     //  MessageBox.Show();
                 }
                 
             }
    }
}
// 