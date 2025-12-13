using System.Globalization;
using System.Text.RegularExpressions;

namespace MyInterpreter;
public class TokenBase
{
    public Position pos;
    public Object value;

    public TokenBase(Position pos, Object value)
    {
        this.pos = pos;
        this.value = value;
    }
}



public class TokenT<TokenType>
{
    public TokenType Typ { get; set; }
    public int Pos { get; set; } 
    public object Value { get; set; }

    public TokenT(TokenType typ, int pos, object value = null)
    {
        Typ = typ;
        Pos = pos;
        Value = value;
    }
}


    public interface ILexer<TokenType>
    {
        string[] GetLines();
        TokenT<TokenType> NextToken();
        bool TokenTypeIsEof(TokenType tt);
        int GetLineNumber();
    }

    public abstract class LexerBase<TokenType> : ILexer<TokenType>
    {
        protected string code;
        protected int line = 1;
        protected int column = 1;
        protected int cur = 0;
        protected int tokenStart = 0;
        protected bool atEoln = false;

        protected Position CurrentPosition() => new Position(line,column); // Simple position representation

        protected bool IsAtEnd() => cur >= code.Length;

        protected string GetTokenText() => code.Substring(tokenStart, cur - tokenStart);

        protected void StartToken() => tokenStart = cur;

        protected void SkipWhitespace()
        {
            while (IsWhitespace(PeekChar()) && !IsAtEnd())
                NextChar();
        }

        protected char NextChar()
        {
            char result = PeekChar();
            if (result == '\0')
                return result;

            if (result == '\n')
            {
                atEoln = false;
                line += 1;
                column = 1;
            }
            else
            {
                column += 1;
            }

            if (result == '\n')
                atEoln = true;

            cur += 1;
            return result;
        }

        protected bool IsMatch(char expected)
        {
            if (PeekChar() != expected)
                return false;

            NextChar();
            return true;
        }

        protected char PeekChar() => IsAtEnd() ? '\0' : code[cur];

        protected char PeekNextChar() => (cur + 1) >= code.Length ? '\0' : code[cur + 1];

        protected static bool IsAlpha(char c)
        {
            return Regex.IsMatch(c.ToString(), @"[A-Za-zА-Яа-яёЁ_]");
        }

        protected static bool IsAlphaNumeric(char c) => IsAlpha(c) || char.IsDigit(c);

        protected static bool IsWhitespace(char c)
        {
            return c == '\r' || c == '\u0007' || c == ' ' || c == '\n' || c == '\t';
        }

        protected string ReadNumber()
        {
            StartToken();
            while (char.IsDigit(PeekChar()))
                NextChar();

            if (PeekChar() == '.' && char.IsDigit(PeekNextChar()))
            {
                NextChar();
                while (char.IsDigit(PeekChar()))
                    NextChar();
            }

            return GetTokenText();
        }

        protected string ReadIdentifier()
        {
            StartToken();
            if (IsAlpha(PeekChar()))
            {
                NextChar();
                while (IsAlphaNumeric(PeekChar()))
                    NextChar();
            }
            return GetTokenText();
        }

        protected string ReadString(char quoteChar = '"')
        {
            StartToken();
            if (IsMatch(quoteChar))
            {
                while (PeekChar() != quoteChar && !IsAtEnd())
                    NextChar();

                if (!IsMatch(quoteChar))
                    MyInterpreter.CompilerExceptions.LexerError("Незавершенная строковая константа", CurrentPosition());
            }
            return GetTokenText();
        }

        public string[] GetLines() => code.Split('\n');

        public LexerBase(string code)
        {
            this.code = code;
        }
        public int GetLineNumber() => line;
     
        public abstract TokenT<TokenType> NextToken();
        public abstract bool TokenTypeIsEof(TokenType tt);
        
        
    }




public enum TokenType
{
    Int, DoubleLiteral, StringLiteral,
    Id, 
    Plus, Minus, Multiply, Divide, Dot,
    Semicolon, LPar, RPar, LBrace, RBrace, Comma, Colon,
    Assign, AssignPlus, AssignMinus, AssignMult, AssignDiv, 
    Equal, Less, LessEqual, Greater, GreaterEqual, NotEqual,
    tkAnd, tkOr, tkNot,
    Eof, 
    tkTrue, tkFalse, tkIf, tkThen, tkElse, tkWhile, tkDo, tkFor,
    tkDef,  tkReturn, tkVar,
}




public class Token : TokenBase
{
    public TokenType type;
    public Token(TokenType type, Position pos, Object value) : base(pos, value)
    {
        this.type = type;
    }
}


public partial class Lexer : LexerBase<TokenType>
    {
        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            ["True"] = TokenType.tkTrue,
            ["False"] = TokenType.tkFalse,
            ["if"] = TokenType.tkIf,
            ["then"] = TokenType.tkThen,
            ["else"] = TokenType.tkElse,
            ["while"] = TokenType.tkWhile,
            ["do"] = TokenType.tkDo,
            ["for"] = TokenType.tkFor,
            ["def"] = TokenType.tkDef,
            ["return"] = TokenType.tkReturn,
            ["var"] =TokenType.tkVar,
        };

        public Lexer(string code) : base(code) { }

        public override bool TokenTypeIsEof(TokenType tt) => tt == TokenType.Eof;

        private TokenT<TokenType> ScanIdentifier(int startPos)
        {
            var identifier = ReadIdentifier();
            var tokenType = Keywords.TryGetValue(identifier, out var keywordType) 
                ? keywordType 
                : TokenType.Id;
            return new TokenT<TokenType>(tokenType, startPos, identifier);
        }

        private TokenT<TokenType> ScanString(int startPos)
        {
            var strWithQuotes = ReadString('"');
            var strValue = strWithQuotes.Substring(1, strWithQuotes.Length - 2);
            return new TokenT<TokenType>(TokenType.StringLiteral, startPos, strValue);
        }
         
        private TokenT<TokenType> ScanNumber(int startPos)
        {
            var numberStr = ReadNumber();
            
            if (numberStr.Contains("."))
                return new TokenT<TokenType>(TokenType.DoubleLiteral, startPos, double.Parse(numberStr, CultureInfo.InvariantCulture));
            else
                return new TokenT<TokenType>(TokenType.Int, startPos, int.Parse(numberStr));
        }

        private TokenT<TokenType> ScanSymbol(int startPos)
        {
            StartToken();
            var c = NextChar();
            
            switch (c)
            {
                case ',':
                    return new TokenT<TokenType>(TokenType.Comma, startPos, ",");
                case ')':
                    return new TokenT<TokenType>(TokenType.RPar, startPos, ")");
                case '(':
                    return new TokenT<TokenType>(TokenType.LPar, startPos, "(");
                case '}':
                    return new TokenT<TokenType>(TokenType.RBrace, startPos, "}");
                case '{':
                    return new TokenT<TokenType>(TokenType.LBrace, startPos, "{");
                case ';':
                    return new TokenT<TokenType>(TokenType.Semicolon, startPos, ";");
                case '.':
                    return new TokenT<TokenType>(TokenType.Dot, startPos, ".");
                    
                case '+':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.AssignPlus : TokenType.Plus,
                        startPos, GetTokenText());
                        
                case '-':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.AssignMinus : TokenType.Minus,
                        startPos, GetTokenText());
                        
                case '*':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.AssignMult : TokenType.Multiply,
                        startPos, GetTokenText());
                        
                case '/':
                    if (IsMatch('/'))
                    {
                        // Skip comment
                        while (PeekChar() != '\n' && !IsAtEnd())
                            NextChar();
                        return NextToken(); // Recursively get next token
                    }
                    else
                    {
                        return new TokenT<TokenType>(
                            IsMatch('=') ? TokenType.AssignDiv : TokenType.Divide,
                            startPos, GetTokenText());
                    }
                    
                case '!':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.NotEqual : TokenType.tkNot,
                        startPos, GetTokenText());
                        
                case '=':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.Equal : TokenType.Assign,
                        startPos, GetTokenText());
                        
                case '>':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.GreaterEqual : TokenType.Greater,
                        startPos, GetTokenText());
                        
                case '<':
                    return new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.LessEqual : TokenType.Less,
                        startPos, GetTokenText());
                        
                case '&':
                    if (IsMatch('&'))
                        return new TokenT<TokenType>(TokenType.tkAnd, startPos, "&&");
                    else
                        MyInterpreter.CompilerExceptions.LexerError("Ожидается &&", CurrentPosition());
                    break;
                    
                case '|':
                    if (IsMatch('|'))
                        return new TokenT<TokenType>(TokenType.tkOr, startPos, "||");
                    else
                        MyInterpreter.CompilerExceptions.LexerError("Ожидается ||", CurrentPosition());
                    break;
                    
                default:
                    MyInterpreter.CompilerExceptions.LexerError($"Неизвестный символ: {c}", new Position(line, startPos));
                    break;
            }
            
            return null; // Will never be reached due to exceptions
        }

        public override TokenT<TokenType> NextToken()
        {
            SkipWhitespace();
            var pos = CurrentPosition();
            if (IsAtEnd())
                return new TokenT<TokenType>(TokenType.Eof, pos.Column, "Eof");
            
            var c = PeekChar();

            if (char.IsDigit(c))
                return ScanNumber(pos.Column);
            else if (IsAlpha(c))
                return ScanIdentifier(pos.Column);
            else if (c == '"')
                return ScanString(pos.Column);
            else
                return ScanSymbol(pos.Column);
        }
    }