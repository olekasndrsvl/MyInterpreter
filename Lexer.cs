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
    tkInt, tkBool, tkDbl,
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
        ["var"] = TokenType.tkVar,
        ["integer"] = TokenType.tkInt,
        ["bool"] = TokenType.tkBool,
        ["double"] = TokenType.tkDbl,
    };

    private TokenT<TokenType> previousToken = null;

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
        
        if (string.IsNullOrEmpty(numberStr))
        {
            CompilerExceptions.LexerError("Ожидается число", CurrentPosition());
            return null;
        }
        
        try
        {
            if (numberStr.Contains("."))
            {
                double value = double.Parse(numberStr, CultureInfo.InvariantCulture);
                return new TokenT<TokenType>(TokenType.DoubleLiteral, startPos, value);
            }
            else
            {
                int value = int.Parse(numberStr, CultureInfo.InvariantCulture);
                return new TokenT<TokenType>(TokenType.Int, startPos, value);
            }
        }
        catch (FormatException)
        {
            CompilerExceptions.LexerError($"Некорректный формат числа: {numberStr}", CurrentPosition());
            return null;
        }
        catch (OverflowException)
        {
            CompilerExceptions.LexerError($"Число {numberStr} выходит за пределы допустимого диапазона", CurrentPosition());
            return null;
        }
    }

    // Проверяет, ожидается ли начало выражения (т.е. минус может быть унарным)
    private bool IsStartOfExpressionContext()
    {
        if (previousToken == null)
            return true; // Начало программы
        
        var prevType = previousToken.Typ;
        
        // Минус может быть унарным, если предыдущий токен - это:
        return prevType == TokenType.LPar ||           // Открывающая скобка: (-5)
               prevType == TokenType.Comma ||          // Запятая: func(1, -5)
               prevType == TokenType.Assign ||         // Присваивание: x = -5
               prevType == TokenType.AssignPlus ||     // Составное присваивание: x += -5
               prevType == TokenType.AssignMinus ||   
               prevType == TokenType.AssignMult ||
               prevType == TokenType.AssignDiv ||
               prevType == TokenType.Plus ||           // Бинарные операторы: a + -5
               prevType == TokenType.Minus ||          // a - -5 (второй минус - унарный)
               prevType == TokenType.Multiply ||       // a * -5
               prevType == TokenType.Divide ||         // a / -5
               prevType == TokenType.tkAnd ||          // Логические операторы: true && -5
               prevType == TokenType.tkOr ||           // false || -5
               prevType == TokenType.tkNot ||          // not -5
               prevType == TokenType.Less ||           // Операторы сравнения: a < -5
               prevType == TokenType.LessEqual ||      // a <= -5
               prevType == TokenType.Greater ||        // a > -5
               prevType == TokenType.GreaterEqual ||   // a >= -5
               prevType == TokenType.Equal ||          // a == -5
               prevType == TokenType.NotEqual ||       // a != -5
               prevType == TokenType.tkReturn ||       // Ключевые слова: return -5
               prevType == TokenType.tkIf ||           // if -5
               prevType == TokenType.tkThen ||         // then -5
               prevType == TokenType.tkElse ||         // else -5
               prevType == TokenType.tkWhile ||        // while -5
               prevType == TokenType.tkDo ||           // do -5
               prevType == TokenType.tkFor ||          // for -5
               prevType == TokenType.tkDef ||          // def -5
               prevType == TokenType.tkVar;            // var -5
    }

    private TokenT<TokenType> ScanSymbol(int startPos)
    {
        StartToken();
        var c = NextChar();
        TokenT<TokenType> result = null;
        
        switch (c)
        {
            case ',':
                result = new TokenT<TokenType>(TokenType.Comma, startPos, ",");
                break;
            case ')':
                result = new TokenT<TokenType>(TokenType.RPar, startPos, ")");
                break;
            case '(':
                result = new TokenT<TokenType>(TokenType.LPar, startPos, "(");
                break;
            case '}':
                result = new TokenT<TokenType>(TokenType.RBrace, startPos, "}");
                break;
            case '{':
                result = new TokenT<TokenType>(TokenType.LBrace, startPos, "{");
                break;
            case ':':
                result = new TokenT<TokenType>(TokenType.Colon, startPos, ":");
                break;
            case ';':
                result = new TokenT<TokenType>(TokenType.Semicolon, startPos, ";");
                break;
            case '.':
                result = new TokenT<TokenType>(TokenType.Dot, startPos, ".");
                break;
                
            case '+':
                result = new TokenT<TokenType>(
                    IsMatch('=') ? TokenType.AssignPlus : TokenType.Plus,
                    startPos, GetTokenText());
                break;
                
            case '-':
                // Проверяем, является ли это составным оператором присваивания
                if (IsMatch('='))
                {
                    result = new TokenT<TokenType>(TokenType.AssignMinus, startPos, "-=");
                }
                // Проверяем, может ли это быть унарным минусом
                else if (IsStartOfExpressionContext())
                {
                    // Если после минуса идет цифра - это отрицательное число
                    if (char.IsDigit(PeekChar()))
                    {
                        result = ScanNumber(startPos);
                        // Меняем знак числа на отрицательный
                        if (result.Value is int intVal)
                            result.Value = -intVal;
                        else if (result.Value is double doubleVal)
                            result.Value = -doubleVal;
                    }
                    else
                    {
                        // Иначе это унарный оператор минус (будет обработан парсером)
                        result = new TokenT<TokenType>(TokenType.Minus, startPos, "-");
                    }
                }
                else
                {
                    // Обычный бинарный оператор минус
                    result = new TokenT<TokenType>(TokenType.Minus, startPos, "-");
                }
                break;
                
            case '*':
                result = new TokenT<TokenType>(
                    IsMatch('=') ? TokenType.AssignMult : TokenType.Multiply,
                    startPos, GetTokenText());
                break;
                
            case '/':
                if (IsMatch('/'))
                {
                    while (PeekChar() != '\n' && !IsAtEnd())
                        NextChar();
                    return NextToken();
                }
                else
                {
                    result = new TokenT<TokenType>(
                        IsMatch('=') ? TokenType.AssignDiv : TokenType.Divide,
                        startPos, GetTokenText());
                }
                break;
                
            case '!':
                result = new TokenT<TokenType>(
                    IsMatch('=') ? TokenType.NotEqual : TokenType.tkNot,
                    startPos, GetTokenText());
                break;
                
            case '=':
                result = new TokenT<TokenType>(
                    IsMatch('=') ? TokenType.Equal : TokenType.Assign,
                    startPos, GetTokenText());
                break;
                
            case '>':
                result = new TokenT<TokenType>(
                    IsMatch('=') ? TokenType.GreaterEqual : TokenType.Greater,
                    startPos, GetTokenText());
                break;
                
            case '<':
                result = new TokenT<TokenType>(
                    IsMatch('=') ? TokenType.LessEqual : TokenType.Less,
                    startPos, GetTokenText());
                break;
                
            case '&':
                if (IsMatch('&'))
                    result = new TokenT<TokenType>(TokenType.tkAnd, startPos, "&&");
                else
                    CompilerExceptions.LexerError("Ожидается &&", CurrentPosition());
                break;
                
            case '|':
                if (IsMatch('|'))
                    result = new TokenT<TokenType>(TokenType.tkOr, startPos, "||");
                else
                    CompilerExceptions.LexerError("Ожидается ||", CurrentPosition());
                break;
                
            default:
                CompilerExceptions.LexerError($"Неизвестный символ: {c}", new Position(line, startPos));
                break;
        }
        
        return result;
    }

    public override TokenT<TokenType> NextToken()
    {
        SkipWhitespace();
        var pos = CurrentPosition();
        
        if (IsAtEnd())
        {
            var eofToken = new TokenT<TokenType>(TokenType.Eof, pos.Column, "Eof");
            previousToken = eofToken;
            return eofToken;
        }
        
        var c = PeekChar();
        TokenT<TokenType> token = null;

        if (char.IsDigit(c))
        {
            token = ScanNumber(pos.Column);
        }
        else if (IsAlpha(c))
        {
            token = ScanIdentifier(pos.Column);
        }
        else if (c == '"')
        {
            token = ScanString(pos.Column);
        }
        else
        {
            token = ScanSymbol(pos.Column);
        }
        
        previousToken = token;
        return token;
    }
}