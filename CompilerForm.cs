using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static MyInterpreter.FormatCodeVisitor;
using System.Threading;
using MyInterpreter.Common;
using SmallMachine;

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
                     
                     Parser parser = new Parser(lex);
                     var progr = parser.MainProgram();
                     SymbolTable.ResetSymbolTable();
                     var  sv = new SemanticCheckVisitor();
                     progr.VisitP(sv);
                     
                     // var generator = new GenerateVirtualMachineVisitor();
                     // List<ThreeAddr> machineCode = generator.VisitNode(progr);
                     //
                     // VirtualMachine.RunProgram(machineCode);
                     var gen = new ThreeAddressCodeVisitor();
                     progr.VisitP(gen);
                     gen.FinalizeCode();
                     VirtualMachine.LoadProgram(gen.Code);
                     
                     var sw = new Stopwatch();
                     sw.Start();
                     VirtualMachine.Run();
                     sw.Stop();
                     CompilerForm.Instance.ChangeOutputBoxText($"Programm elapsed time: {sw.Elapsed}\n"); // Здесь логируем
                     //var generator = new CodeGenerator();
                     //List<SmallMachine.ThreeAddr> generatedCode = progr.Visit(generator);
                     //generatedCode.Add(new SmallMachine.ThreeAddr(SmallMachine.Commands.stop));
                     //SmallVirtualMachine.RunProgram(generatedCode);

                     //SmallVirtualMachine.MemoryDump(5);
                     
                     //var rooti =progr.Visit(new ConvertASTToInterpretTreeVisitor()) as InterpreterTree.StatementNodeI;
                     
                     
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

             private async Task RunProgramm(InterpreterTree.StatementNodeI r) => r.Execute();
             
             // Кнопка рефакторинга
             private void RefactorButton_Click(object sender, EventArgs e)
             {
                 Lexer lex =new Lexer(codeTextBox.Text);


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