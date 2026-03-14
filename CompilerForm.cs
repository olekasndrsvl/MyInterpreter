using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScintillaNET;
using MyInterpreter.Common;
using MyInterpreter.SemanticCheck;

namespace MyInterpreter;

public partial class CompilerForm : Form
{
    public static CompilerForm? Instance;
    
    // Для подсветки синтаксиса через ваш лексер
    private Lexer _lexer = null!;
    private List<Token> _tokens = new List<Token>();
    private System.Windows.Forms.Timer _highlightTimer = null!;
    private bool _needsHighlighting = false;
    
    // Для отслеживания текущего файла
    private string _currentFilePath = string.Empty;
    private bool _isFileModified = false;

    public CompilerForm()
    {
        InitializeComponent();
        Instance = this;

        SetupEditor();
        SetupContextMenu();
        SetupHighlightTimer();
        UpdateWindowTitle();
    }

    private void SetupHighlightTimer()
    {
        _highlightTimer = new System.Windows.Forms.Timer();
        _highlightTimer.Interval = 500;
        _highlightTimer.Tick += HighlightTimer_Tick;
    }

    private void SetupEditor()
    {
        // Базовая настройка
        codeTextBox.Font = new Font("Consolas", 10);
        
        // Настройка отступов - базовые свойства Scintilla
        codeTextBox.UseTabs = false;           // Использовать пробелы вместо табуляции
        codeTextBox.TabWidth = 2;               // Ширина табуляции 2 пробела
        
        // Настройка полей (margins)
        codeTextBox.Margins.Left = 4;
        codeTextBox.Margins[0].Width = 50;
        codeTextBox.Margins[0].Type = MarginType.Number;
        codeTextBox.Margins[0].Mask = 0;

        // Поле для маркеров ошибок
        codeTextBox.Margins[1].Type = MarginType.Symbol;
        codeTextBox.Margins[1].Width = 16;
        codeTextBox.Margins[1].Mask = (1 << 0); // Используем бит 0 для маркеров

        // Настройка маркеров для ошибок
        codeTextBox.Markers[0].SetBackColor(Color.Red);
        codeTextBox.Markers[0].SetForeColor(Color.White);
        codeTextBox.Markers[0].Symbol = MarkerSymbol.Circle;

        // Настройка выделения
        codeTextBox.SetSelectionBackColor(true, Color.FromArgb(50, 100, 150));
        codeTextBox.SetSelectionForeColor(true, Color.White);

        // Подсветка текущей строки
        codeTextBox.CaretLineVisible = true;
        codeTextBox.CaretLineBackColor = Color.FromArgb(240, 240, 240);

        // Базовая настройка стилей
        codeTextBox.Styles[Style.Default].Font = "Consolas";
        codeTextBox.Styles[Style.Default].Size = 10;
        codeTextBox.Styles[Style.Default].ForeColor = Color.Black;
        codeTextBox.Styles[Style.Default].BackColor = Color.White;

        // Применяем базовый стиль
        codeTextBox.StyleClearAll();

        // Настройка пользовательских стилей
        ConfigureCustomStyles();

        // Подписка на события
        codeTextBox.TextChanged += CodeTextBox_TextChanged;
        codeTextBox.KeyDown += CodeTextBox_KeyDown;
        codeTextBox.StyleNeeded += CodeTextBox_StyleNeeded;
    }

    private void ConfigureCustomStyles()
    {
        // Стиль 10: Ключевые слова
        codeTextBox.Styles[10].ForeColor = Color.Blue;
        codeTextBox.Styles[10].Bold = true;
        
        // Стиль 11: Идентификаторы
        codeTextBox.Styles[11].ForeColor = Color.Black;
        
        // Стиль 12: Числа (int)
        codeTextBox.Styles[12].ForeColor = Color.DarkGreen;
        
        // Стиль 13: Числа с плавающей точкой
        codeTextBox.Styles[13].ForeColor = Color.DarkGreen;
        
        // Стиль 14: Строковые литералы
        codeTextBox.Styles[14].ForeColor = Color.DarkRed;
        
        // Стиль 15: Операторы
        codeTextBox.Styles[15].ForeColor = Color.Purple;
        
        // Стиль 16: Комментарии
        codeTextBox.Styles[16].ForeColor = Color.Green;
        codeTextBox.Styles[16].Italic = true;
        
        // Стиль 17: Логические значения
        codeTextBox.Styles[17].ForeColor = Color.Teal;
        codeTextBox.Styles[17].Bold = true;
        
        // Стиль 18: Типы
        codeTextBox.Styles[18].ForeColor = Color.Teal;
        codeTextBox.Styles[18].Bold = true;
        
        // Стиль 19: Ключевые слова управления
        codeTextBox.Styles[19].ForeColor = Color.Blue;
        codeTextBox.Styles[19].Bold = true;
    }

    private int GetStyleForTokenType(TokenType type)
    {
        return type switch
        {
            TokenType.tkIf or TokenType.tkThen or TokenType.tkElse or 
            TokenType.tkWhile or TokenType.tkDo or TokenType.tkFor or
            TokenType.tkDef or TokenType.tkReturn or TokenType.tkVar => 19,
            
            TokenType.tkInt or TokenType.tkBool or TokenType.tkDbl => 18,
            
            TokenType.tkTrue or TokenType.tkFalse => 17,
            
            TokenType.Id => 11,
            
            TokenType.Int => 12,
            TokenType.DoubleLiteral => 13,
            
            TokenType.StringLiteral => 14,
            
            TokenType.Plus or TokenType.Minus or TokenType.Multiply or 
            TokenType.Divide or TokenType.Assign or TokenType.AssignPlus or
            TokenType.AssignMinus or TokenType.AssignMult or TokenType.AssignDiv or
            TokenType.Equal or TokenType.Less or TokenType.LessEqual or
            TokenType.Greater or TokenType.GreaterEqual or TokenType.NotEqual or
            TokenType.tkAnd or TokenType.tkOr or TokenType.tkNot or
            TokenType.Dot or TokenType.Comma or TokenType.Colon or
            TokenType.Semicolon or TokenType.LPar or TokenType.RPar or
            TokenType.LBrace or TokenType.RBrace => 15,
            
            _ => 11
        };
    }

    private void CodeTextBox_TextChanged(object? sender, EventArgs e)
    {
        _needsHighlighting = true;
        _highlightTimer.Stop();
        _highlightTimer.Start();
        
        // Если текст изменился и это не загрузка файла, помечаем как измененный
        if (!_isFileModified)
        {
            _isFileModified = true;
            UpdateWindowTitle();
        }
    }

    private void HighlightTimer_Tick(object? sender, EventArgs e)
    {
        _highlightTimer.Stop();
        if (_needsHighlighting)
        {
            _needsHighlighting = false;
            PerformSyntaxHighlighting();
        }
    }

    private void PerformSyntaxHighlighting()
    {
        try
        {
            _lexer = new Lexer(codeTextBox.Text);
            _tokens = new List<Token>();
            
            TokenT<TokenType> token;
            do
            {
                token = _lexer.NextToken();
                if (token != null)
                {
                    _tokens.Add(new Token(token.Typ, 
                        new Position(_lexer.GetLineNumber(), token.Pos), 
                        token.Value ?? ""));
                }
            } while (token != null && token.Typ != TokenType.Eof);
            
            ApplyHighlighting();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Highlighting error: {ex.Message}");
        }
    }

    private void ApplyHighlighting()
    {
        if (_tokens == null || _tokens.Count == 0) return;
        
        var text = codeTextBox.Text;
        
        // Сбрасываем стили
        codeTextBox.StartStyling(0);
        
        int currentPos = 0;
        foreach (var token in _tokens)
        {
            if (token?.value == null) continue;
            
            string? tokenValue = token.value.ToString();
            if (string.IsNullOrEmpty(tokenValue)) continue;
            
            int pos = text.IndexOf(tokenValue, currentPos, StringComparison.Ordinal);
            if (pos >= 0)
            {
                int styleIndex = GetStyleForTokenType(token.type);
                codeTextBox.StartStyling(pos);
                codeTextBox.SetStyling(tokenValue.Length, styleIndex);
                currentPos = pos + tokenValue.Length;
            }
        }
    }

    private void CodeTextBox_StyleNeeded(object? sender, StyleNeededEventArgs e)
    {
        if (_tokens != null && _tokens.Count > 0)
        {
            ApplyHighlighting();
        }
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

    private void CodeTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control)
        {
            switch (e.KeyCode)
            {
                case Keys.S:
                    if (sender != null)
                        SaveMenuItem_Click(sender, e);
                    e.Handled = true;
                    break;
                case Keys.O:
                    if (sender != null)
                        OpenMenuItem_Click(sender, e);
                    e.Handled = true;
                    break;
                case Keys.N:
                    if (sender != null)
                        NewMenuItem_Click(sender, e);
                    e.Handled = true;
                    break;
                case Keys.F when e.Shift:
                    ShowFindDialog();
                    e.Handled = true;
                    break;
                case Keys.F:
                    if (sender != null)
                        RefactorButton_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    if (sender != null)
                        RunButton_Click(sender, e);
                    e.Handled = true;
                    break;
                case Keys.F6:
                    if (sender != null)
                        CompileButton_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }
    }

    private void ClearErrorMarkers()
    {
        codeTextBox.MarkerDeleteAll(0);
    }

    private void AddErrorMarker(int lineNumber)
    {
        if (lineNumber >= 0 && lineNumber < codeTextBox.Lines.Count)
        {
            codeTextBox.Lines[lineNumber].MarkerAdd(0);
        }
    }

    private void HighlightError(Position pos)
    {
        if (pos == null) return;
        
        int scintillaLine = pos.Line - 1;
        int column = pos.Column - 1;
        
        if (scintillaLine >= 0 && scintillaLine < codeTextBox.Lines.Count)
        {
            AddErrorMarker(scintillaLine);

            var line = codeTextBox.Lines[scintillaLine];
            int position = line.Position + Math.Max(0, column);

            codeTextBox.CurrentPosition = position;
            codeTextBox.ScrollCaret();
        }
    }

    private void SimpleFind(string searchText, bool matchCase, bool wholeWord)
    {
        if (string.IsNullOrEmpty(searchText)) return;
        
        string text = codeTextBox.Text;
        int startPos = codeTextBox.CurrentPosition;
        
        StringComparison comparison = matchCase ? 
            StringComparison.Ordinal : 
            StringComparison.OrdinalIgnoreCase;
        
        int foundPos = text.IndexOf(searchText, startPos, comparison);
        
        if (foundPos >= 0)
        {
            codeTextBox.SetSelection(foundPos, foundPos + searchText.Length);
            codeTextBox.ScrollCaret();
        }
        else
        {
            foundPos = text.IndexOf(searchText, 0, comparison);
            if (foundPos >= 0)
            {
                codeTextBox.SetSelection(foundPos, foundPos + searchText.Length);
                codeTextBox.ScrollCaret();
            }
            else
            {
                MessageBox.Show("Текст не найден", "Поиск", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void ShowFindDialog()
    {
        using (var dialog = new Form())
        {
            dialog.Text = "Найти";
            dialog.Size = new Size(350, 180);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.StartPosition = FormStartPosition.CenterParent;

            var lblSearch = new Label() { Text = "Найти:", Location = new Point(10, 20), Size = new Size(50, 25) };
            var txtSearch = new TextBox() { Location = new Point(70, 20), Size = new Size(250, 25) };
            
            var chkMatchCase = new CheckBox() { Text = "Учитывать регистр", Location = new Point(70, 50), Size = new Size(150, 25) };
            var chkWholeWord = new CheckBox() { Text = "Только слово целиком", Location = new Point(70, 75), Size = new Size(150, 25) };
            
            var btnFind = new Button() { Text = "Найти далее", Location = new Point(120, 110), Size = new Size(100, 30) };

            btnFind.Click += (s, e) => {
                SimpleFind(txtSearch.Text, chkMatchCase.Checked, chkWholeWord.Checked);
            };

            dialog.Controls.Add(lblSearch);
            dialog.Controls.Add(txtSearch);
            dialog.Controls.Add(chkMatchCase);
            dialog.Controls.Add(chkWholeWord);
            dialog.Controls.Add(btnFind);

            dialog.AcceptButton = btnFind;
            dialog.ShowDialog(this);
        }
    }

    public void ChangeOutputBoxText(string text)
    {
        if (outputTextBox.InvokeRequired)
            outputTextBox.Invoke(new Action<string>(ChangeOutputBoxText), outputTextBox.Text + text);
        else
            outputTextBox.Text = outputTextBox.Text + text;
    }

    public void ClearOutputBoxText()
    {
        if (outputTextBox.InvokeRequired)
        {
            outputTextBox.Invoke(new Action(ClearOutputBoxText));
        }
        else
        {
            outputTextBox.Text = "";
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        outputTextBox.BackColor = Color.LightGray;
        outputTextBox.ReadOnly = true;
        outputTextBox.Font = new Font("Consolas", 10);
    }

    // НОВЫЙ МЕТОД: Обновление заголовка окна
    private void UpdateWindowTitle()
    {
        string fileName = string.IsNullOrEmpty(_currentFilePath) 
            ? "Безымянный" 
            : Path.GetFileName(_currentFilePath);
        
        string modifiedMark = _isFileModified ? "*" : "";
        
        this.Text = $"Компилятор - {fileName}{modifiedMark}";
    }

    // ОБНОВЛЕННЫЙ МЕТОД: Новый файл
    private void NewMenuItem_Click(object sender, EventArgs e)
    {
        if (CheckForUnsavedChanges())
        {
            codeTextBox.Text = "";
            outputTextBox.Text = "";
            ClearErrorMarkers();
            
            _currentFilePath = string.Empty;
            _isFileModified = false;
            UpdateWindowTitle();
        }
    }

    // ОБНОВЛЕННЫЙ МЕТОД: Открыть файл
    private void OpenMenuItem_Click(object sender, EventArgs e)
    {
        if (!CheckForUnsavedChanges()) return;
        
        using (var openDialog = new OpenFileDialog())
        {
            openDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            openDialog.Title = "Открыть файл с кодом";
            
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    codeTextBox.Text = File.ReadAllText(openDialog.FileName);
                    ClearErrorMarkers();
                    
                    _currentFilePath = openDialog.FileName;
                    _isFileModified = false;
                    UpdateWindowTitle();
                    
                    outputTextBox.Text = $"✅ Файл загружен: {openDialog.FileName}\n";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", 
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // ОБНОВЛЕННЫЙ МЕТОД: Сохранить файл
    private void SaveMenuItem_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            // Если файл не был сохранен ранее, вызываем Save As
            SaveAsMenuItem_Click(sender, e);
        }
        else
        {
            try
            {
                File.WriteAllText(_currentFilePath, codeTextBox.Text);
                _isFileModified = false;
                UpdateWindowTitle();
                outputTextBox.Text = $"✅ Файл сохранен: {_currentFilePath}\n";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // НОВЫЙ МЕТОД: Сохранить как
    private void SaveAsMenuItem_Click(object sender, EventArgs e)
    {
        using (var saveDialog = new SaveFileDialog())
        {
            saveDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            saveDialog.Title = "Сохранить файл с кодом как";
            saveDialog.DefaultExt = "txt";
            
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveDialog.FileName, codeTextBox.Text);
                    
                    _currentFilePath = saveDialog.FileName;
                    _isFileModified = false;
                    UpdateWindowTitle();
                    
                    outputTextBox.Text = $"✅ Файл сохранен: {saveDialog.FileName}\n";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", 
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // НОВЫЙ МЕТОД: Проверка несохраненных изменений
    private bool CheckForUnsavedChanges()
    {
        if (!_isFileModified) return true;
        
        var result = MessageBox.Show(
            "Файл был изменен. Сохранить изменения?", 
            "Несохраненные изменения", 
            MessageBoxButtons.YesNoCancel, 
            MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            SaveMenuItem_Click(this, EventArgs.Empty);
            return true;
        }
        else if (result == DialogResult.No)
        {
            return true;
        }
        
        return false; // Cancel
    }

    private void PrepareBeforeCompilation()
    {
        outputTextBox.Clear();
        ClearErrorMarkers();
        SymbolTree.Reset();
        SemanticCheckVisitor.Reset();
    }

    private void CompileButton_Click(object sender, EventArgs e)
    {
        var lex = new Lexer(codeTextBox.Text);
        PrepareBeforeCompilation();

        try
        {
            var parser = new Parser(lex);
            var progr = parser.MainProgram();
            var sv = new SemanticCheckVisitor();
            progr.VisitP(sv);

            outputTextBox.Text = "✅ Компиляция завершена! Ошибок: 0\n";
        }
        catch (CompilerExceptions.BaseCompilerException ex)
        {
            string errorText = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
            outputTextBox.Text = errorText;
            
            if (ex.Pos != null)
            {
                HighlightError(ex.Pos);
            }
        }
    }

    private void RunButton_Click(object sender, EventArgs e)
    {
        var lex = new Lexer(codeTextBox.Text);
        PrepareBeforeCompilation();

        try
        {
            var parser = new Parser(lex);
            var progr = parser.MainProgram();

#if DEBUG
            ASTPrinter.PrintAST(progr);
#endif

            var sv = new SemanticCheckVisitor();
            progr.VisitP(sv);
            SymbolTree.PrintNamespaceTree(SymbolTree.Global);

            var frame_gen = new FrameSizeVisitor();
            progr.VisitP(frame_gen);

            var gen = new ThreeAddressCodeVisitor();
            gen.GiveFrameSizes(frame_gen.GetFrameSizes());
            progr.VisitP(gen);

#if DEBUG
            frame_gen.PrintFrameSizes();
#endif

            VirtualMachine.GiveFrameSize(gen.GetFrameSizes());
            var code = gen.GetCode();

#if DEBUG
            GetDebugCodeAsMarkdownTable(code, "../../../DebugFile.md");
#endif

            VirtualMachine.LoadProgram(code);
            VirtualMachine.MemoryDump(1000);

            var sw = new Stopwatch();
            sw.Start();
            VirtualMachine.Run();
            sw.Stop();

            if (Instance != null)
            {
                Instance.ChangeOutputBoxText($"⏱️ Время выполнения: {sw.Elapsed}\n");
            }
            VirtualMachine.MemoryDump(1000);
            VirtualMachine.ResetVirtualMachine();
        }
        catch (CompilerExceptions.BaseCompilerException ex)
        {
            string errorText = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
            outputTextBox.Text = errorText;
            
            if (ex.Pos != null)
            {
                HighlightError(ex.Pos);
            }
        }
    }

    private void RefactorButton_Click(object sender, EventArgs e)
    {
        var lex = new Lexer(codeTextBox.Text);
        ClearErrorMarkers();

        try
        {
            var parser = new Parser(lex);
            var progr = parser.MainProgram();
            var pp = new FormatCodeVisitor();
            codeTextBox.Text = progr.Visit(pp);

            outputTextBox.Text = "✅ Код отформатирован!\n";
            
            // После форматирования помечаем файл как измененный
            _isFileModified = true;
            UpdateWindowTitle();
        }
        catch (CompilerExceptions.BaseCompilerException ex)
        {
            string errorText = CompilerExceptions.OutPutError(ex.GetType().ToString(), ex, lex.GetLines());
            outputTextBox.Text = errorText;
            
            if (ex.Pos != null)
            {
                HighlightError(ex.Pos);
            }
        }
    }

    public string GetDebugCodeAsMarkdownTable(List<ThreeAddr> allCode, string filePath = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| # | BValue | IValue | Label | MemIndex | Op1Index | Op2Index | RValue | Type | command | isInDirectAddressing1 | isInDirectAddressing2 | isInDirectAddressing3 |");
        sb.AppendLine("|---|--------|--------|-------|----------|----------|----------|--------|------|---------|----------------------|----------------------|----------------------|");

        int index = 0;
        foreach (var instruction in allCode)
        {
            string bValue = instruction.BValue.ToString().ToLower();
            string iValue = instruction.IValue.ToString();
            string rValue = instruction.RValue.ToString();
            string label = instruction.Label ?? "null";
            string type = instruction.Type.ToString();

            sb.AppendLine($"| {index} | {bValue} | {iValue} | {label} | {instruction.MemIndex} | {instruction.Op1Index} | {instruction.Op2Index} | {rValue} | {type} | {instruction.command} | {instruction.isInDirectAddressing1} | {instruction.isInDirectAddressing2} | {instruction.isInDirectAddressing3} |");

            index++;
        }

        string result = sb.ToString();

        if (!string.IsNullOrEmpty(filePath))
        {
            File.WriteAllText(filePath, result);
        }

        return result;
    }

#if DEBUG
    private void button1_Click(object sender, EventArgs e)
    {
        ClearOutputBoxText();
        ChangeOutputBoxText("🔍 Режим отладки запущен!\n");
        PrepareBeforeCompilation();
        
        try
        {
            var lex = new Lexer(codeTextBox.Text);
            var parser = new Parser(lex);
            var progr = parser.MainProgram();

            ASTPrinter.PrintAST(progr);

            var sv = new SemanticCheckVisitor();
            progr.VisitP(sv);
            SymbolTree.PrintNamespaceTree(SymbolTree.Global);

            var frame_gen = new FrameSizeVisitor();
            progr.VisitP(frame_gen);

            var gen = new ThreeAddressCodeVisitor();
            gen.GiveFrameSizes(frame_gen.GetFrameSizes());
            progr.VisitP(gen);

            VirtualMachine.GiveFrameSize(gen.GetFrameSizes());
            var code = gen.GetCode();

            GetDebugCodeAsMarkdownTable(code, "../../../DebugFile.md");

            VirtualMachine.LoadProgram(code);
            VirtualMachine.StartDebug();
            
            outputTextBox.AppendText("Отладчик готов. Используйте кнопки управления.\n");
        }
        catch (Exception ex)
        {
            outputTextBox.Text = $"Ошибка при запуске отладчика: {ex.Message}\n";
        }
    }

    private void button2_Click(object sender, EventArgs e)
    {
        try
        {
            VirtualMachine.Continue();
            outputTextBox.AppendText("▶ Продолжение выполнения...\n");
        }
        catch (Exception ex)
        {
            outputTextBox.AppendText($"Ошибка: {ex.Message}\n");
        }
    }

    private void button3_Click(object sender, EventArgs e)
    {
        try
        {
            ClearOutputBoxText();
            VirtualMachine.StepNext();
            outputTextBox.AppendText("⏩ Шаг выполнения\n");
        }
        catch (Exception ex)
        {
            outputTextBox.AppendText($"Ошибка: {ex.Message}\n");
        }
    }

    private void button4_Click(object sender, EventArgs e)
    {
        try
        {
            ChangeOutputBoxText("⏹ Отладка остановлена!\n");
            VirtualMachine.Stop();
            VirtualMachine.ResetVirtualMachine();
        }
        catch (Exception ex)
        {
            outputTextBox.AppendText($"Ошибка: {ex.Message}\n");
        }
    }
#endif
}