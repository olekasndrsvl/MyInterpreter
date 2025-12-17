using System.Data.Common;
using System.Text;

namespace MyInterpreter;

public abstract class ParserBase<TokenType>
{
    protected ILexer<TokenType> lex;
    protected int current = 0;
    protected TokenT<TokenType> curToken;

    public ParserBase(ILexer<TokenType> lex)
    {
        this.lex = lex;
        NextLexem();
    }

    public TokenT<TokenType> PeekToken() => curToken;
    public TokenT<TokenType> CurrentToken() => curToken;
    public bool IsAtEnd() => lex.TokenTypeIsEof(CurrentToken().Typ);

    public TokenT<TokenType> NextLexem()
    {
        var result = curToken;
        curToken = lex.NextToken();
        if (!IsAtEnd())
            current++;
        return result;
    }

    public bool At(params TokenType[] types)
    {
        return types.Any(typ => PeekToken().Typ.Equals(typ));
    }

    public void Check(params TokenType[] types)
    {
        if (!At(types))
            ExpectedError(types);
    }

    public bool IsMatch(params TokenType[] types)
    {
        bool result = At(types);
        if (result)
            NextLexem();
        return result;
    }

    public TokenT<TokenType> Requires(params TokenType[] types)
    {
        if (At(types))
            return NextLexem();
        else
        {
            ExpectedError(types);
            return default; // This line will never be reached due to exception
        }
    }

    protected void ExpectedError(params TokenType[] types)
    {
        string expected = string.Join(" или ", types);
        throw new CompilerExceptions.SyntaxException(
            $"{expected} ожидалось, но {PeekToken().Typ} найдено",
            new Position(lex.GetLineNumber(), PeekToken().Pos)
        );
    }
}

// Новая грамматика:
// Program := FuncDefAndStatements
// FuncDefAndStatements := (VarAssignList | E) ( FunDefList StatementList | StatementList | FunDefList | E)
// FunDefList := FuncDef+
// StatementList := Statement (';' Statement)*
// Statement := Assign | ProcCall | IfStatement | WhileStatement | ForStatement | BlockStatement | ReturnStatement
// FuncDef := def Id '(' IdList ')' Statement
// Assign := Id ('=' | '+=' | '-=' | '*=' | '/=') Expr 
// VarAssign := var Id = Expr
// ProcCall := Id '(' ExprList ')
// FuncCall := Id '(' ExprList ')
// WhileStatement := while Expr do Statement
// ForStatement := for '(' Assign ';' Expr ';' Assign ')' do Statement
// IfStatement := if Expr then Statement [else Statement]
// BlockStatement := '{' StatementList '}' 
// ReturnStatement := return Expr
// Expr := Comp (CompOp Comp)*
// CompOp := '<' | '>' | '<=' | '>=' | '==' | '!='
// Comp := Term (AddOp Term)*
// AddOp := '+' | '-' | '||'
// Term := Factor (MultOp Factor)*
// MultOp := '*' | '/' | '&&'
// Factor := IntNum | DoubleNum | FuncCall | '(' Expr ') | Id
// ExprList := Expr (',' Expr)*
// IdList := Id (',' Id)*


public class Parser : ParserBase<TokenType>
{
    public Parser(ILexer<TokenType> lex) : base(lex) { }

    public FuncDefAndStatements MainProgram()
    {
        var pos = CurrentToken().Pos;

        var globalVarList = GlobalVarList(); 
        
        // Parse function definitions using FuncDefList
        var funcDefList = FuncDefList();

        // Parse main statements
        var statementList = new StatementNode();
        if (!IsAtEnd() && !At(TokenType.Eof))
        {
            statementList = BlockStatement();
        }

        Requires(TokenType.Eof);
        return new FuncDefAndStatements(globalVarList,funcDefList, statementList, new Position(lex.GetLineNumber(), pos));
    }

    // Отдельная функция для разбора списка определений функций
    public FuncDefListNode FuncDefList()
    {
        var funcDefList = new FuncDefListNode();
        
        while (At(TokenType.tkDef))
        {
            funcDefList.Add(FuncDef());
        }
        
        return funcDefList;
    }

    public FuncDefNode FuncDef()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.tkDef);
        
        var name = Ident();
        Requires(TokenType.LPar);
        
        var parameters = IdList();
        Requires(TokenType.RPar);
        
        var body = Statement();
        
        return new FuncDefNode(name, parameters, body, new Position(lex.GetLineNumber(), pos));
    }

    public StatementListNode StatementList()
    {
        var stl = new StatementListNode();
        
        if (!At(TokenType.RBrace))
        {
            stl.Add(Statement());
        }
        
        while (IsMatch(TokenType.Semicolon))
        {
            if (At(TokenType.RBrace) || IsAtEnd()) break;
            stl.Add(Statement());
        }
        
        return stl;
    }

    public StatementNode Statement()
    {
        if (At(TokenType.tkIf))
            return IfStatement();
        else if (At(TokenType.tkWhile))
            return WhileStatement();
        else if (At(TokenType.tkFor))
            return ForStatement();
        else if (At(TokenType.LBrace))
            return BlockStatement();
        else if (At(TokenType.tkReturn))
            return ReturnStatement();
        else if (At(TokenType.tkVar))
             return VariableDeclaration();
        else if (At(TokenType.Id))
        {
            var id = Ident();
            var pos = CurrentToken().Pos;

            if (At(TokenType.Assign) || At(TokenType.AssignPlus) || At(TokenType.AssignMinus) || 
                At(TokenType.AssignMult) || At(TokenType.AssignDiv))
                return ParseAssignment(id, pos);
            else if (At(TokenType.LPar))
                return ParseProcedureCall(id, pos);
            else
                ExpectedError(TokenType.Assign, TokenType.AssignPlus, TokenType.AssignMinus, 
                            TokenType.AssignMult, TokenType.AssignDiv, TokenType.LPar);
        }
        
        ExpectedError(TokenType.Id, TokenType.tkIf, TokenType.tkWhile, 
                    TokenType.LBrace, TokenType.tkFor, TokenType.tkReturn);
        return null;
    }

    private StatementNode ParseAssignment(IdNode id, int pos)
    {
        if (IsMatch(TokenType.Assign))
        {
            var ex = Expr();
            return new AssignNode(id, ex, new Position(lex.GetLineNumber(), pos));
        }
        else if (IsMatch(TokenType.AssignPlus))
        {
            var ex = Expr();
            return new AssignOpNode(id, ex, '+', new Position(lex.GetLineNumber(), pos));
        }
        else if (IsMatch(TokenType.AssignMinus))
        {
            var ex = Expr();
            return new AssignOpNode(id, ex, '-', new Position(lex.GetLineNumber(), pos));
        }
        else if (IsMatch(TokenType.AssignMult))
        {
            var ex = Expr();
            return new AssignOpNode(id, ex, '*', new Position(lex.GetLineNumber(), pos));
        }
        else if (IsMatch(TokenType.AssignDiv))
        {
            var ex = Expr();
            return new AssignOpNode(id, ex, '/', new Position(lex.GetLineNumber(), pos));
        }
        else
        {
            ExpectedError(TokenType.Assign, TokenType.AssignPlus, TokenType.AssignMinus, 
                         TokenType.AssignMult, TokenType.AssignDiv);
            return null;
        }
    }

    private VarAssignListNode GlobalVarList()
    {
        var vass = new VarAssignListNode();
        while (At(TokenType.tkVar))
        {
            vass.Add(VariableDeclaration());
        }
        return vass;
    }
    private VarAssignNode VariableDeclaration()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.tkVar);
        var id = Ident();
        if (IsMatch(TokenType.Assign))
        {
            var ex = Expr();
            return new VarAssignNode(id, ex, new Position(lex.GetLineNumber(), pos));
        }
        else
        {
            ExpectedError(TokenType.Assign);
        }

        return null;
    }
    private StatementNode ParseProcedureCall(IdNode id, int pos)
    {
        Requires(TokenType.LPar);
        var exlst = ExprList();
        Requires(TokenType.RPar);
        return new ProcCallNode(id, exlst, new Position(lex.GetLineNumber(), pos));
    }

    public StatementNode IfStatement()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.tkIf);
        
        var cond = Expr();
        Requires(TokenType.tkThen);
        
        var thenstat = Statement();
        StatementNode elsestat = null;
        
        if (IsMatch(TokenType.tkElse))
        {
            elsestat = Statement();
        }
        
        return new IfNode(cond, thenstat, elsestat, new Position(lex.GetLineNumber(), pos));
    }

    public StatementNode WhileStatement()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.tkWhile);
        
        var cond = Expr();
        Requires(TokenType.tkDo);
        
        var stat = Statement();
        return new WhileNode(cond, stat, new Position(lex.GetLineNumber(), pos));
    }

    public StatementNode ForStatement()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.tkFor);
        Requires(TokenType.LPar);
        
        var counter = Statement();
        if (counter is not AssignNode assignCounter)
        {
            throw new CompilerExceptions.SyntaxException(
                "Expected assignment in for loop initialization",
                new Position(lex.GetLineNumber(), CurrentToken().Pos));
        }
        
        Requires(TokenType.Semicolon);
        
        var condition = Expr();
        Requires(TokenType.Semicolon);
        
        var increment = Statement();
        if (increment is not AssignOpNode assignIncrement)
        {
            throw new CompilerExceptions.SyntaxException(
                "Expected assignment operation in for loop increment",
                new Position(lex.GetLineNumber(), CurrentToken().Pos));
        }
        
        Requires(TokenType.RPar);
        Requires(TokenType.tkDo);
        
        var stat = Statement();
        return new ForNode(assignCounter, condition, assignIncrement, stat, new Position(lex.GetLineNumber(), pos));
    }

    public StatementNode BlockStatement()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.LBrace);
        
        var stl = StatementList();
        Requires(TokenType.RBrace);
        
        return new BlockNode(stl);
    }

    
    public StatementNode ReturnStatement()
    {
        var pos = CurrentToken().Pos;
        Requires(TokenType.tkReturn);
        
        var expr = Expr();
        return new ReturnNode(expr, new Position(lex.GetLineNumber(), pos));
    }

    private List<IdNode> IdList()
    {
        var parameters = new List<IdNode>();
        
        if (At(TokenType.Id))
        {
            parameters.Add(Ident());
            
            while (IsMatch(TokenType.Comma))
            {
                if (!At(TokenType.Id)) break;
                parameters.Add(Ident());
            }
        }
        
        return parameters;
    }

    public ExprListNode ExprList()
    {
        var lst = new ExprListNode();
        
        if (!At(TokenType.RPar))
        {
            lst.Add(Expr());
            
            while (IsMatch(TokenType.Comma))
            {
                if (At(TokenType.RPar)) break;
                lst.Add(Expr());
            }
        }
        
        return lst;
    }

    public IdNode Ident()
    {
        var id = Requires(TokenType.Id);
        return new IdNode(id.Value as string, new Position(lex.GetLineNumber(), id.Pos));
    }

    public ExprNode Expr()
    {
        return Comp();
    }

    public ExprNode Comp()
    {
        var ex = Term();
        
        while (At(TokenType.Less, TokenType.LessEqual, TokenType.Greater, 
                 TokenType.GreaterEqual, TokenType.Equal, TokenType.NotEqual))
        {
            var op = NextLexem();
            var right = Term();
            ex = new BinOpNode(ex, right, op.Typ, new Position(lex.GetLineNumber(), op.Pos));
        }
        
        return ex;
    }

    public ExprNode Term()
    {
        var ex = Factor();
        
        while (At(TokenType.Plus, TokenType.Minus, TokenType.tkOr))
        {
            var op = NextLexem();
            var right = Factor();
            ex = new BinOpNode(ex, right, op.Typ, new Position(lex.GetLineNumber(), op.Pos));
        }
        
        return ex;
    }

    public ExprNode Factor()
    {
        var ex = Primary();
        
        while (At(TokenType.Multiply, TokenType.Divide, TokenType.tkAnd))
        {
            var op = NextLexem();
            var right = Primary();
            ex = new BinOpNode(ex, right, op.Typ, new Position(lex.GetLineNumber(), op.Pos));
        }
        
        return ex;
    }

    private ExprNode Primary()
    {
        if (At(TokenType.Int))
        {
            var token = NextLexem();
            return new IntNode((int)token.Value, new Position(lex.GetLineNumber(), token.Pos));
        }
        else if (At(TokenType.DoubleLiteral))
        {
            var token = NextLexem();
            return new DoubleNode((double)token.Value, new Position(lex.GetLineNumber(), token.Pos));
        }
        else if (IsMatch(TokenType.LPar))
        {
            var result = Expr();
            Requires(TokenType.RPar);
            return result;
        }
        else if (At(TokenType.Id))
        {
            var id = Ident();
            
            if (IsMatch(TokenType.LPar))
            {
                var exlst = ExprList();
                Requires(TokenType.RPar);
                return new FuncCallNode(id, exlst, new Position(lex.GetLineNumber(), id.Pos.Column));
            }
            else
                return id;
        }
        else
        {
            throw new CompilerExceptions.SyntaxException(
                $"Expected expression but found {CurrentToken().Typ}",
                new Position(lex.GetLineNumber(), CurrentToken().Pos));
        }
    }
}

