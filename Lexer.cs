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
public class LexerBase
{
    public string code;// код программы. Инициализируется в конструкторе
    public int line = 1;   // текущая строка
    public int column = 0; // текущий столбец
    public int cur = 0;    // текущая позиция
    public int start = 0;  // начальная позиция токена. code[start:cur] - текущий токен
    public bool atEoln = false; // сервисное поле для метода NextChar

    public Position CurrentPosition() => new Position(line,column);
    public bool IsAtEnd() => cur >= code.Length;

    /// Возвращает текущий символ и переходит к следующему
    public char NextChar()
    {
        char result;
        result = PeekChar(); // вернуть текущий символ 
        if(atEoln)
        {
            atEoln = false;
            line += 1;
            column = 0; 
        }

        if (result == (char)0)
            return result;
        
        if (result =='\n')
        {
            atEoln = true;
        }
        cur += 1; // перейти к следующему
        column += 1;

        return result;
    }

    // Если текущий символ = expected, то продвигаемся вперед
    public bool IsMatch(char expected)
    {
        bool result = PeekChar() == expected;
        if (result)
            NextChar();
        return result;
    }

    /// Вернуть текущий символ
    public char PeekChar() => IsAtEnd() ? (char)0 : code[cur];
    
    /// Вернуть следующий символ
    public char PeekNextChar()
    {
        var pos = cur + 1;
        return pos > code.Length ? (char)0 : code[pos];
    }
    
    /// Является ли символ буквой
    public static bool IsAlpha(char c) => Regex.IsMatch(c.ToString(), "[A-Za-zА-Яа-яёЁ_]");
    
    /// Является ли символ буквой или цифрой
    public static bool IsAlphaNumeric(char c) => IsAlpha(c) || char.IsDigit(c);

    public string[] Lines => code.Split((char)(10));
    
    public LexerBase(string code)
    {
        this.code = code;
    }    
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
    tkTrue, tkFalse, tkIf, tkThen, tkElse, tkWhile, tkDo 
}

public class Token : TokenBase
{
    public TokenType type;
    public Token(TokenType type, Position pos, Object value) : base(pos, value)
    {
        this.type = type;
    }
}


public class Lexer: LexerBase
{
    Dictionary<string, TokenType> KeyWords = new Dictionary<string, TokenType>()
    {
        {"True", TokenType.tkTrue},
        {"False", TokenType.tkFalse},
        {"if", TokenType.tkIf},
        {"then", TokenType.tkThen},
        {"else", TokenType.tkElse},
        {"while", TokenType.tkWhile},
        {"do", TokenType.tkDo}
    };

    private Token GetIdentifier(Position startPos)
    {
        while (IsAlphaNumeric(PeekChar()))
        { 
            NextChar();
        }

        var value = code[start..cur];
        var type = TokenType.Id;
        if (KeyWords.ContainsKey(value))
        {
            type = KeyWords[value];
        }
        return new Token(type, startPos, value);
    }

    private Token GetString(Position startPos)
    {
        while(PeekChar() != '"' && !IsAtEnd())
            NextChar();
        NextChar();
        var value = code[(start + 1)..(cur - 1)];
        return new Token(TokenType.StringLiteral, startPos, value);
    }
    private Token GetNumber(Position startPos)
    {
        while (char.IsDigit(PeekChar()))
        {
            NextChar();
        }

        if (PeekChar() == '.' && char.IsDigit(PeekNextChar()))
        {
            NextChar();
            while (char.IsDigit(PeekChar()))
            {
                NextChar();
            }

            var value = code[start..cur];
            var re = double.Parse(value);
            return new Token(TokenType.DoubleLiteral, startPos, re);
        }
        var _value = code[start..cur];
        return new Token(TokenType.Int, startPos, _value);
    }


    public Token NextToken()
    {
        var c = NextChar();
        while (c == (char)13 || c == (char)7 || c == ' ' || c == (char)10) //пропуск пробелов/табов
            c = NextChar();
        var pos = CurrentPosition();
        start = cur - 1;

        switch (c)
        {
                case (char)0:
                    return new Token(TokenType.Eof,pos,"Eof");
                case ',': 
                    return new Token(TokenType.Comma,pos,',');
                case ')': 
                    return new Token(TokenType.RPar,pos,')');
                case '(':
                    return new Token(TokenType.LPar,pos,'(');
                case '}':
                    return new Token(TokenType.RBrace,pos,'}');
                case '{': 
                    return new Token(TokenType.LBrace,pos,'{');
                case '+': 
                    return new Token(IsMatch('=') ? TokenType.AssignPlus : TokenType.Plus,pos,code[start..cur]);
                case '-': 
                    return new Token(IsMatch('=') ? TokenType.AssignMinus : TokenType.Minus,pos,code[start..cur]);
                case '*':
                    return new Token(IsMatch('=') ? TokenType.AssignMult : TokenType.Multiply,pos,code[start..cur]);
                case '/':
                    if (IsMatch('/'))
                    {
                        while (PeekChar() != (char)10 && !IsAtEnd())
                            NextChar();
                    }
                    else
                    {
                        return new Token(IsMatch('=') ? TokenType.AssignDiv : TokenType.Divide, pos,code[start..cur]);
                    }
                    break;
                case ';': 
                    return new Token(TokenType.Semicolon,pos,';');
                case '!': 
                    return new Token(IsMatch('=') ? TokenType.NotEqual : TokenType.tkNot,pos,code[start..cur]);
                case '=':
                    return new Token(IsMatch('=') ? TokenType.Equal : TokenType.Assign,pos,code[start..cur]);
                case '>': 
                    return new Token(IsMatch('=') ? TokenType.GreaterEqual : TokenType.Greater,pos,code[start..cur]);
                case '<': 
                    return new Token(IsMatch('=') ? TokenType.LessEqual : TokenType.Less,pos,code[start..cur]);            
                case '&':
                    if (IsMatch('&'))
                    {
                        return new Token(TokenType.tkAnd,pos,code[start..cur]);
                    }
                    else
                    {
                        throw new ComplierExceptions.LexerException($"Неверный символ {PeekChar()} после &",
                            CurrentPosition());
                    }
                case '|':
                    if (IsMatch('|'))
                    {
                        return new Token(TokenType.tkOr,pos,code[start..cur]);
                    }
                    else
                    {
                        ComplierExceptions.LexerError($"Неверный символ {PeekChar()} после |",
                            CurrentPosition()
                        );
                    }
                    break;
                case '"':
                    return GetString(pos);
                default:
                    if (char.IsDigit(c))
                    {
                        return GetNumber(pos);
                    }
                    else if (IsAlpha(c))
                    {
                        return GetIdentifier(pos);
                    }
                    else
                    {
                        ComplierExceptions.LexerError(
                            $"Неизвестный символ {c} в позиции {pos.Line},{pos.Column}", pos);
                    }
                    break;
        }
        return new Token(TokenType.Eof,pos,"Eof");
    }

    public Lexer(string code):base(code)
    {
    }
}