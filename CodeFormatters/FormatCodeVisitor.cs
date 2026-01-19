using System.Text;

namespace MyInterpreter;

public class FormatCodeVisitor : IVisitor<string>
{
    private int _indentLevel = 0;
    private bool _inStatementList = false;
    
    private string Indent()
    {
        return new string(' ', _indentLevel);
    }

    private string IndentInc()
    {
        _indentLevel += 2;
        return "";
    }
    
    private string IndentDec()
    {
        _indentLevel -= 2;
        if (_indentLevel < 0) _indentLevel = 0;
        return "";
    }

    public string VisitNode(Node n) => n.Visit(this);
    public string VisitDefinitionNode(DefinitionNode def) => def.Visit(this);
    public string VisitExprNode(ExprNode ex) => ex.Visit(this);
    public string VisitStatementNode(StatementNode st) => st.Visit(this);
    
    public string VisitBinOp(BinOpNode bin)
    {
        // Добавляем пробелы вокруг оператора для читаемости
        string left = VisitNode(bin.Left);
        string op = bin.OpToStr();
        string right = VisitNode(bin.Right);
        
        // Для логических операторов добавляем пробелы
        if (op == "&&" || op == "||" || op.Length > 1)
        {
            return $"{left} {op} {right}";
        }
        else
        {
            return $"{left}{op}{right}";
        }
    }

    public string VisitInt(IntNode n) => n.Val.ToString();
    public string VisitDouble(DoubleNode d) => d.Val.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string VisitId(IdNode id) => id.Name;

    public string VisitAssign(AssignNode ass)
    {
        // Для простого присваивания
        return $"{ass.Ident.Name} = {VisitNode(ass.Expr)}";
    }

    public string VisitVarAssign(VarAssignNode ass)
    {
        // Для объявления переменной с присваиванием
        return $"var {ass.Ident.Name} = {VisitNode(ass.Expr)}";
    }

    public string VisitAssignOp(AssignOpNode ass)
    {
        // Для составных операторов присваивания: +=, -=, *=, /=
        string op = ass.Op switch
        {
            '+' => "+=",
            '-' => "-=",
            '*' => "*=",
            '/' => "/=",
            _ => "="
        };
        return $"{ass.Ident.Name} {op} {VisitNode(ass.Expr)}";
    }

    public string VisitIf(IfNode ifn)
    {
        string condition = VisitNode(ifn.Condition);
        string thenBranch = VisitNode(ifn.ThenStat);
        
        string result = $"{Indent()}if {condition} then";
        
        // Проверяем, является ли then ветка блоком
        if (ifn.ThenStat is BlockNode)
        {
            result += " ";
        }
        else
        {
            result += "\n" + IndentInc();
        }
        
        result += thenBranch;
        
        if (!(ifn.ThenStat is BlockNode))
        {
            result += IndentDec();
        }
        
        // Обрабатываем else ветку
        if (ifn.ElseStat != null)
        {
            string elseBranch = VisitNode(ifn.ElseStat);
            
            if (ifn.ThenStat is BlockNode)
            {
                result += " ";
            }
            else
            {
                result += "\n" + Indent();
            }
            
            result += "else";
            
            if (ifn.ElseStat is BlockNode)
            {
                result += " ";
            }
            else
            {
                result += "\n" + IndentInc();
            }
            
            result += elseBranch;
            
            if (!(ifn.ElseStat is BlockNode))
            {
                result += IndentDec();
            }
        }
        
        return result;
    }

    public string VisitWhile(WhileNode whn)
    {
        string condition = VisitNode(whn.Condition);
        string body = VisitNode(whn.Stat);
        
        string result = $"{Indent()}while {condition} do";
        
        // Если тело - блок, ставим его на той же строке
        if (whn.Stat is BlockNode)
        {
            result += " ";
        }
        else
        {
            result += "\n" + IndentInc();
        }
        
        result += body;
        
        if (!(whn.Stat is BlockNode))
        {
            result += IndentDec();
        }
        
        return result;
    }

    public string VisitFor(ForNode forNode)
    {
        string init = forNode.Counter.Visit(this);
        string condition = forNode.Condition.Visit(this);
        string increment = forNode.Increment.Visit(this);
        string body = VisitNode(forNode.Stat);
        
        string result = $"{Indent()}for ({init}; {condition}; {increment}) do";
        
        // Если тело - блок, ставим его на той же строке
        if (forNode.Stat is BlockNode)
        {
            result += " ";
        }
        else
        {
            result += "\n" + IndentInc();
        }
        
        result += body;
        
        if (!(forNode.Stat is BlockNode))
        {
            result += IndentDec();
        }
        
        return result;
    }

    public string VisitStatementList(StatementListNode stl)
    {
        if (stl.lst == null || stl.lst.Count == 0)
            return "";
        
        var statements = new List<string>();
        bool oldInStatementList = _inStatementList;
        _inStatementList = true;
        
        foreach (var stmt in stl.lst)
        {
            if (stmt != null)
            {
                // Не добавляем отступ для первого оператора в блоке
                string formattedStmt;
                if (statements.Count == 0 && _indentLevel == 0)
                {
                    formattedStmt = stmt.Visit(this);
                }
                else
                {
                    formattedStmt = Indent() + stmt.Visit(this);
                }
                statements.Add(formattedStmt);
            }
        }
        
        _inStatementList = oldInStatementList;
        return string.Join(";\n", statements);
    }

    public string VisitBlockNode(BlockNode bin)
    {
        string oldIndent = "";
        if (!_inStatementList)
        {
            oldIndent = Indent();
        }
        
        string result = oldIndent + "{\n";
        
        // Увеличиваем отступ для содержимого блока
        IndentInc();
        string body = VisitNode(bin.lst);
        IndentDec();
        
        result += body;
        
        if (!string.IsNullOrEmpty(body) && !body.EndsWith("\n"))
        {
            result += "\n";
        }
        
        result += Indent() + "}";
        return result;
    }

    public string VisitExprList(ExprListNode exlist)
    {
        if (exlist.lst == null || exlist.lst.Count == 0)
            return "";
        
        return string.Join(", ", exlist.lst.Select(VisitNode));
    }

    public string VisitProcCall(ProcCallNode p)
    {
        return $"{p.Name.Name}({VisitNode(p.Pars)})";
    }

    public string VisitFuncCall(FuncCallNode f)
    {
        return $"{f.Name.Name}({VisitNode(f.Pars)})";
    }

    public string VisitFuncDef(FuncDefNode f)
    {
        string parameters = string.Join(", ", f.Params.Select(p => p.Name));
        string body = VisitNode(f.Body);
        
        // Функция всегда начинается с новой строки
        string result = $"{Indent()}def {f.Name.Name}({parameters})";
        
        // Если тело - блок, ставим его на той же строке
        if (f.Body is BlockNode)
        {
            result += " ";
        }
        else
        {
            result += "\n" + IndentInc();
        }
        
        result += body;
        
        if (!(f.Body is BlockNode))
        {
            result += IndentDec();
        }
        
        return result;
    }

    public string VisitReturn(ReturnNode r)
    {
        if (r.Expr != null)
            return $"{Indent()}return {VisitNode(r.Expr)}";
        else
            return $"{Indent()}return";
    }

    public string VisitDefinitionsAndStatements(DefinitionsAndStatements defsAndStmts)
    {
        string definitions = defsAndStmts.DefinitionsList.Visit(this);
        string mainProgram = defsAndStmts.MainProgram.Visit(this);
        
        // Добавляем пустую строку между определениями и основной программой
        if (!string.IsNullOrEmpty(definitions) && !string.IsNullOrEmpty(mainProgram))
        {
            return definitions + "\n\n" + mainProgram;
        }
        else if (!string.IsNullOrEmpty(definitions))
        {
            return definitions;
        }
        else
        {
            return mainProgram;
        }
    }

    public string VisitVariableDeclarationNode(VariableDeclarationNode varDecl)
    {
        return varDecl.vass.Visit(this);
    }

    public string VisitDefinitionsList(DefinitionsListNode defList)
    {
        if (defList.lst == null || defList.lst.Count == 0)
            return "";
        
        var definitions = new List<string>();
        
        foreach (var def in defList.lst)
        {
            if (def != null)
            {
                // Для определений функций добавляем отступ
                string formattedDef = Indent() + def.Visit(this);
                definitions.Add(formattedDef);
            }
        }
        
        return string.Join(";\n", definitions);
    }
}