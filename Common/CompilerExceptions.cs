using System.Text;


namespace MyInterpreter;
public class Position
{
    public int Line { get; set; }
    public int Column { get; set; }

    public Position(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"({Line}, {Column})";
    }
}
public class CompilerExceptions
{
    public class BaseCompilerException : Exception
    {
        public Position Pos;
        public BaseCompilerException(string message, Position p =null) : base(message) { Pos = p; }
    }

    public class LexerException : BaseCompilerException
    {
        public LexerException(string message, Position p = null) : base(message, p) { }
    }

    public class SyntaxException : BaseCompilerException
    {
        public SyntaxException(string message, Position p = null) : base(message, p) { }
    }

    public class SemanticException : BaseCompilerException
    {
        public SemanticException(string message, Position p = null) : base(message, p) { }
    }

    
    // Выброс ошибок объявленных выше
    public static void LexerError(string msg, Position pos)
    {
       
        throw new LexerException(msg, pos);
    }

    public static void SyntaxError(string msg, Position pos)
    {
        throw new SyntaxException(msg, pos);
    }

    public static void SemanticError(string msg, Position pos)
    {
        throw new SemanticException(msg, pos);
    }
    
    
    // Вывод ошибок со "стрелочкой"
    public static string OutPutError(string prefix, BaseCompilerException e, string[] lines)
    {
        var sb = new StringBuilder();
        var line = lines[e.Pos.Line-1];
        sb.Append(line);
        sb.Append('\n');
        for (var i = 0; i < e.Pos.Column - 1; i++)
            sb.Append(' ');
        sb.Append('^');
        sb.Append('\n');
        sb.Append(prefix + ' ' + e.Pos.ToString() + ':' + e.Message);
        sb.Append('\n');
        return sb.ToString();
    }

}