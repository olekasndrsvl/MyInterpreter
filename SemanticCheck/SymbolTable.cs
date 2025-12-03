namespace MyInterpreter.SemanticCheck;

// Семантические типы
public enum SemanticType
{
    IntType,
    DoubleType,
    BoolType,
    StringType,
    BadType,
    NoType,
    AnyType // для Print
}

// Типы символов
public enum KindType
{
    VarName,
    FuncName
}

// Информация о символе
public class SymbolInfo
{
    public SymbolInfo(string n, KindType k, SemanticType t)
    {
        Name = n;
        Kind = k;
        Type = t;
    }

    public SymbolInfo(string n, KindType k, SemanticType[] pars, SemanticType t)
    {
        Name = n;
        Kind = k;
        Params = pars;
        Type = t;
    }

    public string Name { get; }
    public KindType Kind { get; }
    public SemanticType Type { get; } // для функций - тип возвращаемого значения
    public SemanticType[] Params { get; } // для функций - типы параметров

    public override string ToString()
    {
        return $"{Name} ({Kind}, {Type})";
    }
}

// Класс для хранения специализации функции
public class FunctionSpecialization
{
    public FunctionSpecialization()
    {
    }

    public FunctionSpecialization(SemanticType[] parameterTypes, SemanticType returnType = SemanticType.AnyType)
    {
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }

    public SemanticType[] ParameterTypes { get; set; }
    public SemanticType ReturnType { get; set; }
    public Dictionary<string, SymbolInfo> LocalVariableTypes { get; } = new();
    public int SpecializationId { get; set; }
    public bool BodyChecked { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is FunctionSpecialization other)
        {
            if (ParameterTypes.Length != other.ParameterTypes.Length)
                return false;

            for (var i = 0; i < ParameterTypes.Length; i++)
                if (ParameterTypes[i] != other.ParameterTypes[i])
                    return false;
            return ReturnType == other.ReturnType;
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var type in ParameterTypes) hash = hash * 31 + type.GetHashCode();
            hash = hash * 31 + ReturnType.GetHashCode();
            return hash;
        }
    }
}

// Информация о функции
public class FunctionInfo
{
    public FuncDefNode Definition { get; set; }
    public List<FunctionSpecialization> Specializations { get; set; } = new();

    public FunctionSpecialization FindOrCreateSpecialization(SemanticType[] parameterTypes)
    {
        foreach (var spec in Specializations)
            if (AreParameterTypesCompatible(spec.ParameterTypes, parameterTypes))
                return spec;

        var newSpec = new FunctionSpecialization(parameterTypes);
        
      
        
        Specializations.Add(newSpec);
        newSpec.SpecializationId = Specializations.Count - 1;
        return newSpec;
    }

    private bool AreParameterTypesCompatible(SemanticType[] paramTypes, SemanticType[] argTypes)
    {
        if (paramTypes.Length != argTypes.Length)
            return false;

        for (var i = 0; i < paramTypes.Length; i++)
            if (paramTypes[i] != argTypes[i])
                return false;
        return true;
    }
}

// Статические массивы типов
public static class Constants
{
    public static readonly HashSet<TokenType> ArithmeticOperations = new()
    {
        TokenType.Plus,
        TokenType.Minus,
        TokenType.Multiply,
        TokenType.Divide
    };

    public static readonly HashSet<TokenType> CompareOperations = new()
    {
        TokenType.Equal,
        TokenType.Less,
        TokenType.LessEqual,
        TokenType.Greater,
        TokenType.GreaterEqual,
        TokenType.NotEqual
    };

    public static readonly HashSet<TokenType> LogicalOperations = new()
    {
        TokenType.tkAnd,
        TokenType.tkOr,
        TokenType.tkNot
    };

    public static SemanticType[] NumTypes = { SemanticType.IntType, SemanticType.DoubleType, SemanticType.AnyType };
}

// Основная таблица символов
public static class SymbolTable
{
    // Новая структура для хранения информации о функциях
    public static Dictionary<string, FunctionInfo> FunctionTable = new();
    static SymbolTable()
    {
        InitStandardFunctionsTable();
        var mainfunc = new FunctionInfo();
        var spec = mainfunc.FindOrCreateSpecialization([SemanticType.NoType]);
        FunctionTable.Add("Main", mainfunc);
    }


    // Таблица стандартных функций
    private static void InitStandardFunctionsTable()
    {
        // Sqrt - специализации для Double и Int
        var sqrtFunc = new FunctionInfo();
        sqrtFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.DoubleType));
        sqrtFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType },
            SemanticType.DoubleType));
        FunctionTable["Sqrt"] = sqrtFunc;

        // Print - специализации для всех типов
        var printFunc = new FunctionInfo();
        printFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.AnyType },
            SemanticType.NoType));
        printFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType },
            SemanticType.NoType));
        printFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.NoType));
        printFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.BoolType },
            SemanticType.NoType));
        printFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.StringType },
            SemanticType.NoType));
        FunctionTable["Print"] = printFunc;

        // Sin - специализации для Double и Int
        var sinFunc = new FunctionInfo();
        sinFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.DoubleType));
        sinFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType },
            SemanticType.DoubleType));
        FunctionTable["Sin"] = sinFunc;

        // Cos - специализации для Double и Int
        var cosFunc = new FunctionInfo();
        cosFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.DoubleType));
        cosFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType },
            SemanticType.DoubleType));
        FunctionTable["Cos"] = cosFunc;

        // Abs - специализации для Double и Int
        var absFunc = new FunctionInfo();
        absFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.DoubleType));
        absFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType },
            SemanticType.IntType));
        FunctionTable["Abs"] = absFunc;

        // Round - специализации для Double
        var roundFunc = new FunctionInfo();
        roundFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.IntType));
        FunctionTable["Round"] = roundFunc;

        // Дополнительные стандартные функции с их специализациями:

        // Pow - возведение в степень
        var powFunc = new FunctionInfo();
        powFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType, SemanticType.DoubleType },
            SemanticType.DoubleType));
        powFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType, SemanticType.IntType },
            SemanticType.DoubleType));
        FunctionTable["Pow"] = powFunc;

        // Max - максимум из двух чисел
        var maxFunc = new FunctionInfo();
        maxFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType, SemanticType.DoubleType },
            SemanticType.DoubleType));
        maxFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType, SemanticType.IntType },
            SemanticType.IntType));
        FunctionTable["Max"] = maxFunc;

        // Min - минимум из двух чисел
        var minFunc = new FunctionInfo();
        minFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType, SemanticType.DoubleType },
            SemanticType.DoubleType));
        minFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType, SemanticType.IntType },
            SemanticType.IntType));
        FunctionTable["Min"] = minFunc;

        // ToString - преобразование в строку
        var toStringFunc = new FunctionInfo();
        toStringFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.AnyType },
            SemanticType.StringType));
        toStringFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType },
            SemanticType.StringType));
        toStringFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType },
            SemanticType.StringType));
        toStringFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.BoolType },
            SemanticType.StringType));
        FunctionTable["ToString"] = toStringFunc;
    }

    // Методы для работы с таблицей символов
    public static void AddVariable(string name, FunctionSpecialization specialization, SemanticType type)
    {
        if (specialization.LocalVariableTypes.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' already declared");
        specialization.LocalVariableTypes[name] =
            new SymbolInfo(name, KindType.VarName, type);
    }

    // Новый метод для регистрации определения функции
    public static void RegisterFunctionDefinition(string name, FuncDefNode definition)
    {
        if (!FunctionTable.ContainsKey(name)) FunctionTable[name] = new FunctionInfo();
        FunctionTable[name].Definition = definition;
    }

    // Новый метод для получения специализации функции
    public static FunctionSpecialization GetFunctionSpecialization(string name, SemanticType[] argTypes)
    {
        if (FunctionTable.ContainsKey(name)) return FunctionTable[name].FindOrCreateSpecialization(argTypes);
        return null;
    }

    // Новый метод для получения всех специализаций функции
    public static List<FunctionSpecialization> GetFunctionSpecializations(string name)
    {
        if (FunctionTable.ContainsKey(name)) return FunctionTable[name].Specializations;
        return new List<FunctionSpecialization>();
    }


    public static void ResetSymbolTable()
    {
        
        FunctionTable.Clear();
        
        InitStandardFunctionsTable();
        var mainfunc = new FunctionInfo();
        var spec = mainfunc.FindOrCreateSpecialization([SemanticType.NoType]);
        FunctionTable.Add("Main", mainfunc);
    }

    // Проверка совместимости типов
    public static bool AreTypesCompatible(SemanticType t1, SemanticType t2)
    {
        if (t1 == t2) return true;
        if (t1 == SemanticType.AnyType || t2 == SemanticType.AnyType) return true;

        // Неявное преобразование int -> double
        if (t1 == SemanticType.IntType && t2 == SemanticType.DoubleType) return true;
        if (t1 == SemanticType.DoubleType && t2 == SemanticType.IntType) return true;

        return false;
    }

    // Получение результирующего типа для бинарной операции
    public static SemanticType GetResultType(SemanticType left, SemanticType right, string op)
    {
        // Для арифметических операций
        if (op == "+" || op == "-" || op == "*" || op == "/")
        {
            if (left == SemanticType.DoubleType || right == SemanticType.DoubleType)
                return SemanticType.DoubleType;
            if (left == SemanticType.IntType && right == SemanticType.IntType)
                return SemanticType.IntType;
        }

        // Для операций сравнения
        if (op == "<" || op == ">" || op == "<=" || op == ">=" || op == "==" || op == "!=")
            return SemanticType.BoolType;

        throw new ArgumentException($"Unknown operation: {op}");
    }


    // For debug
    public static void PrintFunctionTable()
    {
        Console.WriteLine("Function Table:");
        foreach (var function in FunctionTable)
        {
            Console.WriteLine($"  {function.Key}:");
            if (function.Value.Definition != null) Console.WriteLine("    Definition: Present");
            Console.WriteLine($"    Specializations: {function.Value.Specializations.Count}");
            foreach (var spec in function.Value.Specializations)
            {
                Console.WriteLine($"      Specialization {spec.SpecializationId}:");
                Console.WriteLine($"        Parameters: [{string.Join(", ", spec.ParameterTypes ?? new SemanticType[]{} )}]");
                Console.WriteLine($"        Return Type: {spec.ReturnType}");
                Console.WriteLine($"        Local Variables: {spec.LocalVariableTypes.Count}");
                foreach (var localVar in spec.LocalVariableTypes)
                    Console.WriteLine($"          {localVar.Key}: {localVar.Value.Type}");
            }
        }
    }
}