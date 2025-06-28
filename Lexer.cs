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




public class Lexer
{
    
}