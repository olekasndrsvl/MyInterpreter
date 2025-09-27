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
                new Position(lex.GetLineNumber(),PeekToken().Pos)
            );
        }
    }

    // Supporting types (assuming these are defined elsewhere)
    

   


// Program := StatementList
// StatementList := Statement (';' Statement)*
// Statement := Assign | ProcCall | IfStatement | WhileStatement | BlockStatement 
// Assign := Id ('=' | '+=' | '-=' | '*=' | '/=') Expr 
// ProcCall := Id '(' ExprList ')
// FuncCall := Id '(' ExprList ')
// WhileStatement := while Expr do Statement
// ForStatement := for Assign ';' Expr ';' Assign do Statement

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

public class Parser : ParserBase<TokenType>
    {
        public Parser(ILexer<TokenType> lex) : base(lex) { }

        private StatementNode ParseAssignment(IdNode id, int pos)
        {
            if (IsMatch(TokenType.Assign))
            {
                var ex = Expr();
                return new AssignNode(id, ex, new Position(lex.GetLineNumber(),CurrentToken().Pos));
            }
            else if (IsMatch(TokenType.AssignPlus))
            {
                var ex = Expr();
                return new AssignOpNode(id, ex,'+', new Position(lex.GetLineNumber(),pos));
            }
            else
            {
                ExpectedError(TokenType.Assign, TokenType.AssignPlus);
                return null; // Will never be reached due to exception
            }
        }

        private StatementNode ParseProcedureCall(IdNode id, int pos)
        {
            Requires(TokenType.LPar);
            var exlst = ExprList();
            Requires(TokenType.RPar);
            return new ProcCallNode(id, exlst, new Position(lex.GetLineNumber(),CurrentToken().Pos));
        }

        private BinOpNode CreateBinaryOperation(ExprNode left, ExprNode right, TokenT<TokenType> op)
        {
            return new BinOpNode(left, right, op.Typ, new Position(lex.GetLineNumber(),CurrentToken().Pos));
        }

        public StatementNode MainProgram()
        {
            current = 0;
            var result = StatementList();
            Requires(TokenType.Eof);
            return result;
        }

        public StatementNode StatementList()
        {
            var stl = new StatementListNode();
            stl.Add(Statement());
            while (IsMatch(TokenType.Semicolon))
                stl.Add(Statement());
            return stl;
        }

        public StatementNode IfStatement()
        {
            var pos = CurrentToken().Pos;
            Requires(TokenType.tkIf);
            var cond = Expr();
            Requires(TokenType.tkThen);
            var thenstat = Statement();
            var elsestat = IsMatch(TokenType.tkElse) ? Statement() : null;
            return new IfNode(cond, thenstat, elsestat, new Position(lex.GetLineNumber(),CurrentToken().Pos));
        }

        public StatementNode WhileStatement()
        {
            var pos = CurrentToken().Pos;
            Requires(TokenType.tkWhile);
            var cond = Expr();
            Requires(TokenType.tkDo);
            var stat = Statement();
            return new WhileNode(cond, stat, new Position(lex.GetLineNumber(),CurrentToken().Pos));
        }

        public StatementNode ForStatement()
        {
            // for (i =0; i<10; i+=1) do
            var pos = CurrentToken().Pos;
            Requires(TokenType.tkFor);
            Requires(TokenType.LPar);
            var counter_variable = Ident();
            
            
            // i=0 разбираем
            AssignNode count;
            if (At(TokenType.Assign))
            {
                count = ParseAssignment(counter_variable, pos) as AssignNode;
            }
            else
            {
                throw new CompilerExceptions.SyntaxException($"Expected variable assignment, but {PeekToken().Typ} found", counter_variable.Pos);
            }
            Requires(TokenType.Semicolon);
            // i<10 разбираем
            var cond = Expr();
            Requires(TokenType.Semicolon);
            // i+=1
            var counter_variable_1 = Ident();

            AssignOpNode inc;
            if (At( TokenType.AssignPlus, TokenType.AssignMinus))
            {
                 inc = ParseAssignment(counter_variable, pos) as AssignOpNode;
            }
            else
            {
                throw new CompilerExceptions.SyntaxException($"Expected variable assignment, but {PeekToken().Typ} found", counter_variable.Pos);
            }
            // do
            Requires(TokenType.RPar);
            Requires(TokenType.tkDo);
            var stat = Statement();
            return new ForNode(count, cond, inc,stat);
        }

        public StatementNode BlockStatement()
        {
            var pos = CurrentToken().Pos;
            Requires(TokenType.LBrace);
            var stl = StatementList();
            Requires(TokenType.RBrace);
            
            return stl;
        }

        public StatementNode Statement()
        {
            var pos = CurrentToken().Pos;
            
            if (At(TokenType.tkIf))
                return IfStatement();
            else if (At(TokenType.tkWhile))
                return WhileStatement();
            else if (At(TokenType.LBrace))
                return BlockStatement();
            else if (At(TokenType.tkFor))
                return ForStatement();
            else if (At(TokenType.Id))
            {
                var id = Ident();
                if (At(TokenType.Assign, TokenType.AssignPlus))
                    return ParseAssignment(id, pos);
                else if (At(TokenType.LPar))
                    return ParseProcedureCall(id, pos);
                else
                {
                    ExpectedError(TokenType.Assign, TokenType.LPar);
                    return null; // Will never be reached due to exception
                }
            }
            else
            {
                ExpectedError(TokenType.Id, TokenType.tkIf, TokenType.tkWhile, TokenType.LBrace);
                return null; // Will never be reached due to exception
            }
        }

        public ExprListNode ExprList()
        {
            var lst = new ExprListNode();
            lst.Add(Expr());
            while (IsMatch(TokenType.Comma))
                lst.Add(Expr());
            return lst;
        }

        public IdNode Ident()
        {
            var id = Requires(TokenType.Id);
            return new IdNode(id.Value as string, new Position(lex.GetLineNumber(),CurrentToken().Pos));
        }

        public ExprNode Expr()
        {
            var ex = Comp();
            while (At(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less,
                    TokenType.LessEqual, TokenType.Equal, TokenType.NotEqual))
            {
                var op = NextLexem();
                var right = Comp();
                ex = CreateBinaryOperation(ex, right, op);
            }
            return ex;
        }

        public ExprNode Comp()
        {
            var ex = Term();
            while (At(TokenType.Plus, TokenType.Minus, TokenType.tkOr))
            {
                var op = NextLexem();
                var right = Term();
                ex = CreateBinaryOperation(ex, right, op);
            }
            return ex;
        }

        public ExprNode Term()
        {
            var ex = Factor();
            while (At(TokenType.Multiply, TokenType.Divide, TokenType.tkAnd))
            {
                var op = NextLexem();
                var right = Factor();
                ex = CreateBinaryOperation(ex, right, op);
            }
            return ex;
        }

        public ExprNode Factor()
        {
            var pos = CurrentToken().Pos;
            
            if (At(TokenType.Int))
            {
                var token = NextLexem();
                return new IntNode((int)token.Value, new Position(lex.GetLineNumber(),CurrentToken().Pos));
            }
            else if (At(TokenType.DoubleLiteral))
            {
                var token = NextLexem();
                return new DoubleNode((double)token.Value, new Position(lex.GetLineNumber(),CurrentToken().Pos));
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
                    var result = new FuncCallNode(id, exlst, new Position(lex.GetLineNumber(),CurrentToken().Pos));
                    Requires(TokenType.RPar);
                    return result;
                }
                else
                    return id;
            }
            else
                throw new CompilerExceptions.SyntaxException($"Expected INT or ( or id but {PeekToken().Typ} found", new Position(lex.GetLineNumber(),PeekToken().Pos));
        }

        // private OpType TokenToOp(TokenType t)
        // {
        //     switch (t)
        //     {
        //         case TokenType.Plus: return OpType.opPlus;
        //         case TokenType.Minus: return OpType.opMinus;
        //         case TokenType.Multiply: return OpType.opMultiply;
        //         case TokenType.Divide: return OpType.opDivide;
        //         case TokenType.Equal: return OpType.opEqual;
        //         case TokenType.Less: return OpType.opLess;
        //         case TokenType.LessEqual: return OpType.opLessEqual;
        //         case TokenType.Greater: return OpType.opGreater;
        //         case TokenType.GreaterEqual: return OpType.opGreaterEqual;
        //         case TokenType.NotEqual: return OpType.opNotEqual;
        //         case TokenType.tkAnd: return OpType.opAnd;
        //         case TokenType.tkOr: return OpType.opOr;
        //         case TokenType.tkNot: return OpType.opNot;
        //         default: return OpType.opBad;
        //     }
        // }
    }

