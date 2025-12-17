using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MyInterpreter.Common;
using MyInterpreter.SemanticCheck;

namespace MyInterpreter;

public partial class CompilerForm : Form
{
    public static CompilerForm Instance;
    private bool _ctrlPressed = false;
    private bool _isHighlighting = false;
    
    // Список ключевых слов для подсветки
    private readonly HashSet<string> _keywords = new HashSet<string>
    {
        "if", "then", "else", "while", "do", "for", 
        "def", "return", "var", "print"
    };

    public CompilerForm()
    {
        InitializeComponent();
        Instance = this;
        
        // Настраиваем RichTextBox для подсветки синтаксиса
        codeTextBox.Font = new Font("Consolas", 10);
        codeTextBox.TextChanged += CodeTextBox_TextChanged;
        codeTextBox.KeyDown += CodeTextBox_KeyDown;
        codeTextBox.KeyUp += CodeTextBox_KeyUp;
        
        // Настраиваем контекстное меню для копирования/вставки
        SetupContextMenu();
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip();
        
        var copyItem = new ToolStripMenuItem("Копировать");
        copyItem.Click += (s, e) => codeTextBox.Copy();
        copyItem.ShortcutKeys = Keys.Control | Keys.C;
        
        var cutItem = new ToolStripMenuItem("Вырезать");
        cutItem.Click += (s, e) => codeTextBox.Cut();
        cutItem.ShortcutKeys = Keys.Control | Keys.X;
        
        var pasteItem = new ToolStripMenuItem("Вставить");
        pasteItem.Click += (s, e) => codeTextBox.Paste();
        pasteItem.ShortcutKeys = Keys.Control | Keys.V;
        
        var selectAllItem = new ToolStripMenuItem("Выделить все");
        selectAllItem.Click += (s, e) => codeTextBox.SelectAll();
        selectAllItem.ShortcutKeys = Keys.Control | Keys.A;
        
        contextMenu.Items.Add(copyItem);
        contextMenu.Items.Add(cutItem);
        contextMenu.Items.Add(pasteItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(selectAllItem);
        
        codeTextBox.ContextMenuStrip = contextMenu;
    }

    private void CodeTextBox_TextChanged(object sender, EventArgs e)
    {
        if (_isHighlighting) return;
        
        // Запоминаем текущую позицию курсора и выделение
        var selectionStart = codeTextBox.SelectionStart;
        var selectionLength = codeTextBox.SelectionLength;
        
        _isHighlighting = true;
        
        try
        {
            // Подсвечиваем синтаксис
            HighlightSyntax();
        }
        finally
        {
            _isHighlighting = false;
            
            // Восстанавливаем позицию курсора и выделение
            codeTextBox.SelectionStart = selectionStart;
            codeTextBox.SelectionLength = selectionLength;
            
            // Сбрасываем цвет выделения
            codeTextBox.SelectionColor = codeTextBox.ForeColor;
        }
    }

    private void HighlightSyntax()
    {
        var text = codeTextBox.Text;
        if (string.IsNullOrEmpty(text)) return;
        
        // Сохраняем текущие настройки
        var originalColor = codeTextBox.SelectionColor;
        var originalFont = codeTextBox.SelectionFont;
        
        // Устанавливаем базовый стиль
        codeTextBox.Select(0, text.Length);
        codeTextBox.SelectionColor = Color.Black;
        codeTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Regular);
        
        // Подсвечиваем ключевые слова
        foreach (var keyword in _keywords)
        {
            var pattern = $@"\b{keyword}\b";
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                codeTextBox.Select(match.Index, match.Length);
                codeTextBox.SelectionColor = Color.Blue;
                codeTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Bold);
            }
        }
        
        // Подсвечиваем строковые литералы (в двойных кавычках)
        var stringPattern = @"""(?:[^""\\]|\\.)*""";
        var stringMatches = Regex.Matches(text, stringPattern);
        foreach (Match match in stringMatches)
        {
            codeTextBox.Select(match.Index, match.Length);
            codeTextBox.SelectionColor = Color.DarkRed;
            codeTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Regular);
        }
        
        // Подсвечиваем числовые литералы
        var numberPattern = @"\b\d+(\.\d+)?\b";
        var numberMatches = Regex.Matches(text, numberPattern);
        foreach (Match match in numberMatches)
        {
            codeTextBox.Select(match.Index, match.Length);
            codeTextBox.SelectionColor = Color.DarkGreen;
            codeTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Regular);
        }
        
        // Подсвечиваем комментарии (однострочные)
        var commentPattern = @"//.*";
        var commentMatches = Regex.Matches(text, commentPattern);
        foreach (Match match in commentMatches)
        {
            codeTextBox.Select(match.Index, match.Length);
            codeTextBox.SelectionColor = Color.Green;
            codeTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Italic);
        }
        
        // Восстанавливаем оригинальный цвет
        codeTextBox.SelectionColor = originalColor;
        if (originalFont != null)
            codeTextBox.SelectionFont = originalFont;
    }

    private void CodeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Отслеживаем состояние Ctrl
        if (e.KeyCode == Keys.ControlKey)
        {
            _ctrlPressed = true;
        }
        
        if (e.KeyCode == Keys.Enter)
        {
            HandleEnterKey();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Back && _ctrlPressed)
        {
            // Ctrl+Backspace - удалить предыдущее слово
            HandleCtrlBackspace();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete && _ctrlPressed)
        {
            // Ctrl+Delete - удалить следующее слово
            HandleCtrlDelete();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Back)
        {
            // Обычный Backspace с проверкой отступа
            if (HandleSmartBackspace())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        else if (e.KeyCode == Keys.Delete)
        {
            // Обычный Delete с проверкой отступа
            if (HandleSmartDelete())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        else if (e.KeyCode == Keys.Tab)
        {
            // Tab - добавить отступ
            HandleTabKey();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.S && _ctrlPressed)
        {
            // Ctrl+S - сохранить
            SaveMenuItem_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.O && _ctrlPressed)
        {
            // Ctrl+O - открыть
            OpenMenuItem_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.N && _ctrlPressed)
        {
            // Ctrl+N - новый файл
            NewMenuItem_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F && _ctrlPressed)
        {
            // Ctrl+F - форматирование
            RefactorButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F5)
        {
            // F5 - запуск
            RunButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F6)
        {
            // F6 - компиляция
            CompileButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void CodeTextBox_KeyUp(object sender, KeyEventArgs e)
    {
        // Сбрасываем состояние Ctrl
        if (e.KeyCode == Keys.ControlKey)
        {
            _ctrlPressed = false;
        }
    }

    private void HandleEnterKey()
    {
        var currentPos = codeTextBox.SelectionStart;
        var text = codeTextBox.Text;

        // Находим начало текущей строки
        var lineStart = currentPos;
        while (lineStart > 0 && text[lineStart - 1] != '\n')
            lineStart--;

        // Получаем текущую строку до курсора
        var currentLine = text.Substring(lineStart, currentPos - lineStart);

        // Находим отступ текущей строки
        var baseIndent = "";
        for (var i = 0; i < currentLine.Length; i++)
        {
            if (currentLine[i] != ' ' && currentLine[i] != '\t')
                break;
            baseIndent += currentLine[i];
        }

        // Определяем, нужно ли увеличивать отступ
        var increaseIndent = ShouldIncreaseIndent(currentLine.Trim());
        var decreaseIndent = ShouldDecreaseIndent(currentLine.Trim());

        // Определяем отступ для новой строки
        var newIndent = baseIndent;

        if (increaseIndent)
            // Увеличиваем отступ для if, while, for, def и т.д.
            newIndent += new string(' ', 2);
        else if (decreaseIndent && baseIndent.Length >= 2)
            // Уменьшаем отступ после закрывающей скобки
            newIndent = baseIndent.Substring(0, baseIndent.Length - 2);

        // Вставляем новую строку
        codeTextBox.Text = text.Insert(currentPos, "\n" + newIndent);
        codeTextBox.SelectionStart = currentPos + 1 + newIndent.Length;
    }

   private bool HandleSmartBackspace()
{
    var currentPos = codeTextBox.SelectionStart;
    var text = codeTextBox.Text;
    
    if (currentPos == 0) return false;
    
    // Находим начало текущей строки
    var lineStart = currentPos;
    while (lineStart > 0 && text[lineStart - 1] != '\n')
        lineStart--;
    
    // Получаем текущую строку от начала до курсора
    var lineToCursor = text.Substring(lineStart, currentPos - lineStart);
    
    // Случай 1: Курсор в самом начале строки (сразу после \n)
    if (currentPos == lineStart && lineStart > 0)
    {
        // Удаляем символ новой строки - курсор перейдет в конец предыдущей строки
        codeTextBox.Text = text.Remove(lineStart - 1, 1);
        codeTextBox.SelectionStart = lineStart - 1;
        return true;
    }
    
    // Случай 2: В строке только пробелы/табы (пустая строка с отступами)
    var isOnlyWhitespace = true;
    for (var i = 0; i < lineToCursor.Length; i++)
    {
        if (lineToCursor[i] != ' ' && lineToCursor[i] != '\t')
        {
            isOnlyWhitespace = false;
            break;
        }
    }
    
    if (isOnlyWhitespace && lineToCursor.Length > 0)
    {
        // Удаляем отступ кратно 2 пробелам
        var indentToRemove = 2;
        if (lineToCursor.Length >= indentToRemove)
        {
            // Удаляем 2 пробела
            var charsToRemove = Math.Min(indentToRemove, lineToCursor.Length);
            codeTextBox.Text = text.Remove(currentPos - charsToRemove, charsToRemove);
            codeTextBox.SelectionStart = currentPos - charsToRemove;
        }
        else
        {
            // Если отступ меньше 2 пробелов, удаляем все и символ новой строки
            var charsToRemove = lineToCursor.Length;
            codeTextBox.Text = text.Remove(currentPos - charsToRemove, charsToRemove);
            codeTextBox.SelectionStart = currentPos - charsToRemove;
            
            // Теперь курсор в начале строки, можно удалить \n если он есть
            if (codeTextBox.SelectionStart > 0 && codeTextBox.Text[codeTextBox.SelectionStart - 1] == '\n')
            {
                codeTextBox.Text = codeTextBox.Text.Remove(codeTextBox.SelectionStart - 1, 1);
                codeTextBox.SelectionStart--;
            }
        }
        return true;
    }
    
    return false;
}

    private bool HandleSmartDelete()
    {
        var currentPos = codeTextBox.SelectionStart;
        var text = codeTextBox.Text;
        
        if (currentPos >= text.Length) return false; // В конце текста
        
        // Проверяем, находимся ли мы на отступе в начале строки
        var lineEnd = currentPos;
        while (lineEnd < text.Length && text[lineEnd] != '\n')
            lineEnd++;
        
        // Получаем текст от курсора до конца строки
        var lineFromCursor = text.Substring(currentPos, lineEnd - currentPos);
        
        // Проверяем, состоит ли текст после курсора только из пробелов/табов
        var isOnlyWhitespace = true;
        for (var i = 0; i < lineFromCursor.Length; i++)
        {
            if (lineFromCursor[i] != ' ' && lineFromCursor[i] != '\t')
            {
                isOnlyWhitespace = false;
                break;
            }
        }
        
        if (isOnlyWhitespace && lineFromCursor.Length > 0)
        {
            // Удаляем отступ кратно 2 пробелам
            var indentToRemove = 2;
            if (lineFromCursor.Length >= indentToRemove)
            {
                // Удаляем 2 пробела (или все, если меньше 2)
                var charsToRemove = Math.Min(indentToRemove, lineFromCursor.Length);
                codeTextBox.Text = text.Remove(currentPos, charsToRemove);
                codeTextBox.SelectionStart = currentPos;
                return true;
            }
        }
        
        return false;
    }

    private void HandleCtrlBackspace()
    {
        var currentPos = codeTextBox.SelectionStart;
        var text = codeTextBox.Text;
        
        if (currentPos == 0) return;
        
        // Ищем начало слова
        var startPos = currentPos - 1;
        
        // Пропускаем пробелы/табы
        while (startPos >= 0 && char.IsWhiteSpace(text[startPos]))
            startPos--;
        
        // Ищем начало слова (не буквенно-цифровые символы или начало текста)
        while (startPos >= 0 && !char.IsWhiteSpace(text[startPos]) && 
               (char.IsLetterOrDigit(text[startPos]) || text[startPos] == '_'))
            startPos--;
        
        startPos++; // Корректируем позицию
        
        if (startPos < currentPos)
        {
            // Удаляем слово
            codeTextBox.Text = text.Remove(startPos, currentPos - startPos);
            codeTextBox.SelectionStart = startPos;
        }
    }

    private void HandleCtrlDelete()
    {
        var currentPos = codeTextBox.SelectionStart;
        var text = codeTextBox.Text;
        
        if (currentPos >= text.Length) return;
        
        // Ищем конец слова
        var endPos = currentPos;
        
        // Пропускаем пробелы/табы
        while (endPos < text.Length && char.IsWhiteSpace(text[endPos]))
            endPos++;
        
        // Ищем конец слова (буквенно-цифровые символы или подчеркивания)
        while (endPos < text.Length && !char.IsWhiteSpace(text[endPos]) && 
               (char.IsLetterOrDigit(text[endPos]) || text[endPos] == '_'))
            endPos++;
        
        if (endPos > currentPos)
        {
            // Удаляем слово
            codeTextBox.Text = text.Remove(currentPos, endPos - currentPos);
            codeTextBox.SelectionStart = currentPos;
        }
    }

    private void HandleTabKey()
    {
        var currentPos = codeTextBox.SelectionStart;
        var text = codeTextBox.Text;
        
        // Вставляем 2 пробела (стандартный отступ)
        codeTextBox.Text = text.Insert(currentPos, "  ");
        codeTextBox.SelectionStart = currentPos + 2;
    }

    private bool ShouldIncreaseIndent(string line)
    {
        // Проверяем, заканчивается ли строка на символы, требующие увеличения отступа
        return line.EndsWith("{") ||
               line.EndsWith("then") ||
               line.EndsWith("do") ||
               (line.Contains("def ") && !line.Contains("{"));
    }

    private bool ShouldDecreaseIndent(string line)
    {
        // Проверяем, начинается ли строка с символов, требующих уменьшения отступа
        return line.StartsWith("}") ||
               line.StartsWith("else") ||
               line.StartsWith("} else");
    }

    public void ChangeOutputBoxText(string text)
    {
        if (outputTextBox.InvokeRequired)
            outputTextBox.Invoke(new Action<string>(ChangeOutputBoxText), outputTextBox.Text + text);
        else
            outputTextBox.Text = outputTextBox.Text + text;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        outputTextBox.BackColor = Color.LightGray;
        outputTextBox.ReadOnly = true;
        outputTextBox.Font = new Font("Consolas", 10);
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
                // После загрузки файла подсвечиваем синтаксис
                HighlightSyntax();
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
                File.WriteAllText(saveDialog.FileName, codeTextBox.Text);
        }
    }

    // Кнопка компиляции
    private void CompileButton_Click(object sender, EventArgs e)
    {
        var lex = new Lexer(codeTextBox.Text);
        outputTextBox.Clear();
        try
        {
            var parser = new Parser(lex);
            var progr = parser.MainProgram();
            var sv = new SemanticCheckVisitor();
            progr.VisitP(sv);

            outputTextBox.Text = "Компиляция завершена! Ошибок: 0 \n";
        }
        catch (CompilerExceptions.BaseCompilerException ex)
        {
            outputTextBox.Text = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
        }
    }

    // Кнопка запуска
    private async void RunButton_Click(object sender, EventArgs e)
    {
        var lex = new Lexer(codeTextBox.Text);
        outputTextBox.Clear();
        try
        {
            SemanticCheckVisitor.Reset();

            var parser = new Parser(lex);
            var progr = parser.MainProgram();
            SymbolTree.PrintNamespaceTree(SymbolTree.Global);
            var sv = new SemanticCheckVisitor();
            progr.VisitP(sv);

            SymbolTree.Reset();
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
                Console.WriteLine(VARIABLE.Key + " " + VARIABLE.Value);

            foreach (var VARIABLE in gen._currentTempIndexes) 
                Console.WriteLine(VARIABLE.Key + " " + VARIABLE.Value);

            Instance.ChangeOutputBoxText($"Programm elapsed time: {sw.Elapsed}\n");
            VirtualMachine.MemoryDump(1000);
            VirtualMachine.ResetVirtualMachine();
        }
        catch (CompilerExceptions.BaseCompilerException ex)
        {
            outputTextBox.Text = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
        }
    }

    // Кнопка рефакторинга
    private void RefactorButton_Click(object sender, EventArgs e)
    {
        var lex = new Lexer(codeTextBox.Text);

        try
        {
            var parser = new Parser(lex);
            var progr = parser.MainProgram();
            var pp = new FormatCodeVisitor();
            codeTextBox.Text = progr.Visit(pp);
            
            // После форматирования подсвечиваем синтаксис
            HighlightSyntax();
            
            outputTextBox.Text = "Код отформатирован!";
        }
        catch (CompilerExceptions.LexerException ex)
        {
            outputTextBox.Text =
                "Lex ERROR:" + CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
        }
        catch (CompilerExceptions.BaseCompilerException ex)
        {
            outputTextBox.Text = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
        }
    }
}