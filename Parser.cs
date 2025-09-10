using System.Data.Common;
using System.Text;

namespace MyInterpreter;

public class ParserBase
{
    private Lexer lex;
    protected int current = 0;
    protected Token curToken;

    public ParserBase(Lexer lex)
    {
        this.lex = lex;
        NextLexem();
    }
    /// Вернуть текущий токен и перейти к следующему
    public Token NextLexem()
    {
        var result = curToken;
        curToken = lex.NextToken();
        if(!IsAtEnd())
            current++;
        return result;
    }

    /// Проверить, что тип текушего токена совпадает с одним из данных типов
    public bool At(params TokenType[] types)
    {
        return types.Any(t => PeekToken().type == t);
    }
    
    public void Check(params TokenType[] types)
    {
        if (!At(types))
        {
            ExpectedError(types);
        }
    }

    /// Проверить, что тип текушего токена совпадает с одним из данных типов, 
    /// и в случае успеха перейти к следующему символу
    public bool IsMatch(params TokenType[] types)
    {
        var res = At(types);
        if (res)
            NextLexem();
        return res;
    }

    /// Проверить на соответствие и вернуть токен или выбросить ошибку
    /// В отличие от At в случае неуспеха бросает ошибку
    public Token Requires(params TokenType[] types)
    {
        if (At(types))
        {
            return NextLexem();
        }
        else
        {
            ExpectedError(types);
        }
        return null;
    }
    public Token PeekToken()
    {
        return curToken;
    }

    public Token CurrentToken()
    {
        return curToken;
    }
    public bool IsAtEnd()
    {
        return PeekToken().type == TokenType.Eof;
    }

    public void ExpectedError(params TokenType[] types)
    {
        var sb = new StringBuilder();
        foreach (var type in types)
        {
            sb.Append(type.ToString());
            sb.Append(" или ");
        }
        
        throw new MyInterpreter.ComplierExceptions.SyntaxException($"{sb.ToString()} ожидалось, но {PeekToken().type} найдено",
            PeekToken().pos);
    }
}


// Program := StatementList
// StatementList := Statement (';' Statement)*
// Statement := Assign | ProcCall | IfStatement | WhileStatement | BlockStatement 
// Assign := Id ('=' | '+=' | '-=' | '*=' | '/=') Expr 
// ProcCall := Id '(' ExprList ')
// FuncCall := Id '(' ExprList ')
// WhileStatement := while Expr do Statement
// IfStatement := if Expr then Statement [else Statement]
// BlockStatement := '{' StatementList '}' 
// Expr := Comp (CompOp Comp)*
// CompOp := '<' | '>' | '<=' | '>=' | '==' | '!='
// Comp := Term (AddOp Term)*
// AddOp := '+' | '-' | '||'
// Term := Factor (MultOp Factor)*
// MultOp := '*' | '/' | '&&'
// Factor := IntNum | DoubleNum | FuncCall | '(' Expr ') 
// ExprList := Expr (',' Expr)*

public class Parser: ParserBase
{
    public Parser(Lexer lex) : base(lex)
    {
    }
    
    // Program := StatementList  
    public ASTNodes.StatementNode MainProgram()
    {
        current = 0;
        var result = StatementList();
        Requires(TokenType.Eof);
        return result;
    }
    
    // StatementList := Statement (';' Statement)*
    public ASTNodes.StatementListNode StatementList()
    {
        var stl = new ASTNodes.StatementListNode();
        stl.Add(Statement());
        while (IsMatch(TokenType.Semicolon))
        {
            stl.Add(Statement());
        }
        return stl;
    }
    
    // Statement := Assign | ProcCall | IfStatement | WhileStatement | BlockStatement 
    public ASTNodes.StatementNode Statement()
    {
        // id = expr id += expr
        // id(exprlist)
        // if expr then stat [else stat]
        // while expr do stat
        var pos = CurrentToken().pos;
        Check(new TokenType[]{TokenType.Id, TokenType.tkIf, TokenType.tkWhile, TokenType.LBrace});

        if (IsMatch(TokenType.tkIf))
        {
            var cond = Expr();
            Requires(TokenType.tkThen);
            var thenstat = Statement();
            var elsestat = IsMatch(TokenType.tkElse)?Statement():null;
            return new ASTNodes.IfNode(cond,thenstat,elsestat);
        }
        else if (IsMatch(TokenType.tkWhile))
        {
            var cond = Expr();
            Requires(TokenType.tkDo);
            var stat = Statement();
            return new ASTNodes.WhileNode(cond,stat);
        }
        else if (IsMatch(TokenType.LBrace))
        {
            var stl = StatementList();
            Requires(TokenType.RBrace);
            return stl;
        }

        var id = Ident();
        if (IsMatch(TokenType.Assign))
        {
            var ex = Expr();
            return new ASTNodes.AssignNode(id, ex);
        }
        else   if (IsMatch(TokenType.AssignPlus))
        {
            var ex = Expr();
            return new ASTNodes.AssignPlusNode(id, ex, pos);
        }
        else if(IsMatch(TokenType.LPar))
        {
            var exlst = ExprList();
            Requires(TokenType.RPar);
            return new ASTNodes.ProcCallNode(id, exlst);
        }
        else
        {
            ExpectedError(TokenType.Assign,TokenType.LPar);
        }
        return null;
    }

    public ASTNodes.ExprNode Factor()
    {
        var pos = CurrentToken().pos;
        if (At(TokenType.Int))
        {
            return new ASTNodes.IntNode(int.Parse(NextLexem().value as string));
        }
        else if (At(TokenType.DoubleLiteral))
        {
            return new ASTNodes.DoubleNode((double)NextLexem().value);
        }
        else if (IsMatch(TokenType.LPar))
        {
            var result = Expr();
            Requires(TokenType.RPar);
        }
        else if(At(TokenType.Id))
        {
            // id
            // id ( exprlist )
            var id = Ident();
            if (IsMatch(TokenType.LPar))
            {
                 var exlst = ExprList(); 
                 var result = new ASTNodes.FuncCallNode(id,exlst);
                 Requires(TokenType.RPar);
                 return result;
            }
            else
            {
                return id;
            }
        }
        else
        {
            throw new MyInterpreter.ComplierExceptions.SyntaxException(
                $"Expected INT or ( or id but {PeekToken().type} found", PeekToken().pos);
        }
        return null;
    }

    public ASTNodes.ExprNode Term()
    {
        var ex =  Factor();
        while (At(TokenType.Multiply,TokenType.Divide,TokenType.tkAnd))
        {
            var op = NextLexem();
            var right = Factor();
            ex = new ASTNodes.BinOpNode(ex, right, (op.value as string).First());
        }

        return ex;
    }
    public ASTNodes.ExprNode Comp()
    {
        var ex = Term();
        while (At(TokenType.Plus, TokenType.Minus, TokenType.tkOr))
        {
            var op = NextLexem();
            var right = Term();
            ex = new ASTNodes.BinOpNode(ex, right, (op.value as string).First());
        }

        return ex;
    }
    
    public ASTNodes.ExprNode Expr()
    {
        var ex = Comp();
        while (At(TokenType.Greater,TokenType.GreaterEqual,TokenType.Less,TokenType.LessEqual,TokenType.Equal,TokenType.NotEqual))
        {
            var op = NextLexem();
            var right = Comp();
            ex = new ASTNodes.BinOpNode(ex, right, (op.value as string).First());

        }
        return ex;
    }

    public ASTNodes.ExprListNode ExprList()
    {
        // expr (',' expr)*
        var lst = new ASTNodes.ExprListNode();
        lst.Add(Expr());
        while (IsMatch(TokenType.Comma))
        {
            lst.Add(Expr());
        }
        return lst;
    }
    public ASTNodes.IdNode Ident()
    {
        var id = Requires(TokenType.Id);
        return new ASTNodes.IdNode(id.value as string);
    }
}