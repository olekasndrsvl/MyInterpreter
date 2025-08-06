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
            sb.Append("или");
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
    public ASTNodes.StatementNode StatementList()
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
        return null;
    }
}