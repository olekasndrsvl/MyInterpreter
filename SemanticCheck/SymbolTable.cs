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

// Базовый класс пространства имен
public abstract class NameSpace
{
    public NameSpace Parent { get; set; }
    public string Name { get; set; }
    public Dictionary<string, SymbolInfo> Variables { get; } = new();
    public List<NameSpace> Children { get; } = new();

    public virtual void AddVariable(string name, SemanticType type)
    {
        if (Variables.ContainsKey(name))
            throw new InvalidOperationException($"Variable '{name}' already declared in this scope");
        
        Variables[name] = new SymbolInfo(name, KindType.VarName, type);
    }

    public SymbolInfo LookupVariable(string name)
    {
        // Поиск в текущем пространстве имен
        if (Variables.TryGetValue(name, out var symbol))
            return symbol;

        // Рекурсивный поиск в родительских пространствах имен
        return Parent?.LookupVariable(name);
    }

    public virtual NameSpace CreateChildNamespace(string name)
    {
        var child = new RegularNameSpace
        {
            Parent = this,
            Name = name
        };
        Children.Add(child);
        return child;
    }

    public virtual NameSpace CreateLightWeightChild(string name)
    {
        var child = new LightWeightNameSpace
        {
            Parent = this,
            Name = name
        };
        Children.Add(child);
        return child;
    }
}

// Обычное пространство имен
public class RegularNameSpace : NameSpace
{
    public RegularNameSpace()
    {
    }
}

// Легковесное пространство имен (для временных областей видимости)
public class LightWeightNameSpace : NameSpace
{
    // Упрощенная версия для временных вычислений
    public LightWeightNameSpace()
    {
    }
}

// Глобальное пространство имен
public class GlobalNameSpace : NameSpace
{
    public GlobalNameSpace()
    {
        Name = "Global";
        Parent = null;
    }

    public override NameSpace CreateChildNamespace(string name)
    {
        var child = new RegularNameSpace
        {
            Parent = this,
            Name = name
        };
        Children.Add(child);
        return child;
    }
}

// Класс для хранения специализации функции
public class FunctionSpecialization
{
    public FunctionSpecialization(SemanticType[] parameterTypes, SemanticType returnType = SemanticType.AnyType)
    {
        var nm = new RegularNameSpace
        {
            Parent = SymbolTree.Global,
        };
        SymbolTree.Global.Children.Add(nm);
        NameSpace = nm;
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }

    public FunctionInfo Function { get; }
    public SemanticType[] ParameterTypes { get; set; }
    public SemanticType ReturnType { get; set; }
    public NameSpace NameSpace { get; set; } // Ссылка на пространство имен этой специализации
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
public class FunctionInfo(FuncDefNode definition = null)
{
    public FuncDefNode Definition { get; set; } = definition;
    public List<FunctionSpecialization> Specializations { get; set; } = new();

    public FunctionSpecialization FindOrCreateSpecialization(SemanticType[] parameterTypes)
    {
        // Проверяем существующие специализации
        foreach (var spec in Specializations)
            if (AreParameterTypesCompatible(spec.ParameterTypes, parameterTypes))
                return spec;

        // Создаем новую специализацию
        var newSpec = new FunctionSpecialization(parameterTypes)
        {
            SpecializationId = Specializations.Count
        };
        newSpec.NameSpace.Name=definition?.Name+newSpec.SpecializationId.ToString();
        Specializations.Add(newSpec);
        
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

// Главная таблица символов
public static class SymbolTree
{
    public static GlobalNameSpace Global { get; private set; }
    public static Dictionary<string, FunctionInfo> FunctionTable { get; private set; }

    static SymbolTree()
    {
        Reset();
    }

    public static void Reset()
    {
        Global = new GlobalNameSpace();
        FunctionTable = new Dictionary<string, FunctionInfo>();
        InitStandardFunctions();
    }

    private static void InitStandardFunctions()
    {
        // Sqrt
        var sqrtFunc = new FunctionInfo();
        sqrtFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType }, SemanticType.DoubleType));
        sqrtFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType }, SemanticType.DoubleType));
        FunctionTable["Sqrt"] = sqrtFunc;

        // Print
        var printFunc = new FunctionInfo();
        printFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.AnyType }, SemanticType.NoType));
        FunctionTable["Print"] = printFunc;

        // Sin
        var sinFunc = new FunctionInfo();
        sinFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType }, SemanticType.DoubleType));
        sinFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType }, SemanticType.DoubleType));
        FunctionTable["Sin"] = sinFunc;

        // Cos
        var cosFunc = new FunctionInfo();
        cosFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType }, SemanticType.DoubleType));
        cosFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType }, SemanticType.DoubleType));
        FunctionTable["Cos"] = cosFunc;

        // Abs
        var absFunc = new FunctionInfo();
        absFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType }, SemanticType.DoubleType));
        absFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType }, SemanticType.IntType));
        FunctionTable["Abs"] = absFunc;

        // Round
        var roundFunc = new FunctionInfo();
        roundFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType }, SemanticType.IntType));
        FunctionTable["Round"] = roundFunc;

        // Pow
        var powFunc = new FunctionInfo();
        powFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType, SemanticType.DoubleType }, SemanticType.DoubleType));
        powFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType, SemanticType.IntType }, SemanticType.DoubleType));
        FunctionTable["Pow"] = powFunc;

        // Max
        var maxFunc = new FunctionInfo();
        maxFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType, SemanticType.DoubleType }, SemanticType.DoubleType));
        maxFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType, SemanticType.IntType }, SemanticType.IntType));
        FunctionTable["Max"] = maxFunc;

        // Min
        var minFunc = new FunctionInfo();
        minFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.DoubleType, SemanticType.DoubleType }, SemanticType.DoubleType));
        minFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.IntType, SemanticType.IntType }, SemanticType.IntType));
        FunctionTable["Min"] = minFunc;

        // ToString
        var toStringFunc = new FunctionInfo();
        toStringFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.AnyType }, SemanticType.StringType));
        FunctionTable["ToString"] = toStringFunc;

        // Main
        var mainFunc = new FunctionInfo();
        mainFunc.Specializations.Add(new FunctionSpecialization(
            new[] { SemanticType.NoType }, SemanticType.NoType));
        FunctionTable["Main"] = mainFunc;
    }

    // Создание пространства имен для специализации функции
    public static NameSpace CreateFunctionNamespace(string functionName, int specializationId, 
        SemanticType[] paramTypes, string[] paramNames)
    {
        // Создаем пространство имен для функции как дочернее к Global
        var funcNamespace = Global.CreateChildNamespace($"{functionName}_{specializationId}");
        
        // Добавляем параметры как переменные в это пространство имен
        for (int i = 0; i < paramTypes.Length; i++)
        {
            funcNamespace.AddVariable(paramNames[i], paramTypes[i]);
        }
        
        // Связываем пространство имен со специализацией
        if (FunctionTable.TryGetValue(functionName, out var funcInfo) && 
            specializationId < funcInfo.Specializations.Count)
        {
            funcInfo.Specializations[specializationId].NameSpace = funcNamespace;
        }
        
        return funcNamespace;
    }

    // Получение или создание специализации функции
    public static FunctionSpecialization GetOrCreateFunctionSpecialization(string functionName, 
        SemanticType[] paramTypes)
    {
        if (!FunctionTable.TryGetValue(functionName, out var funcInfo))
        {
            funcInfo = new FunctionInfo();
            FunctionTable[functionName] = funcInfo;
        }

        return funcInfo.FindOrCreateSpecialization(paramTypes);
    }

    // Получение специализации функции по id
    public static FunctionSpecialization GetFunctionSpecialization(string functionName, int specializationId)
    {
        if (FunctionTable.TryGetValue(functionName, out var funcInfo) && 
            specializationId < funcInfo.Specializations.Count)
        {
            return funcInfo.Specializations[specializationId];
        }
        return null;
    }

    // Поиск функции
    public static FunctionInfo LookupFunction(string functionName)
    {
        return FunctionTable.TryGetValue(functionName, out var funcInfo) ? funcInfo : null;
    }

    // Поиск переменной в дереве пространств имен
    public static SymbolInfo LookupVariable(string variableName, NameSpace currentNamespace)
    {
        return currentNamespace?.LookupVariable(variableName);
    }

    // Добавление переменной в текущее пространство имен
    public static void AddVariableToCurrentNamespace(string name, SemanticType type, NameSpace currentNamespace)
    {
        if (currentNamespace == null)
            throw new InvalidOperationException("Cannot add variable - no current namespace");
        
        currentNamespace.AddVariable(name, type);
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

    // Для отладки - печать дерева пространств имен
    public static void PrintNamespaceTree(NameSpace ns, int indent = 0)
    {
        var indentStr = new string(' ', indent * 2);
        
        Console.WriteLine($"{indentStr}Namespace: {ns.Name} ({ns.GetType().Name})");
        
        if (ns.Variables.Count > 0)
        {
            Console.WriteLine($"{indentStr}  Variables:");
            foreach (var var in ns.Variables)
                Console.WriteLine($"{indentStr}    {var.Key}: {var.Value.Type}");
        }
        
        foreach (var child in ns.Children)
            PrintNamespaceTree(child, indent + 1);
    }

    // Для отладки - печать таблицы функций
    public static void PrintFunctionTable()
    {
        Console.WriteLine("Function Table:");
        foreach (var function in FunctionTable)
        {
            Console.WriteLine($"  {function.Key}:");
            foreach (var spec in function.Value.Specializations)
            {
                Console.WriteLine($"    Specialization {spec.SpecializationId}:");
                Console.WriteLine($"      Parameters: [{string.Join(", ", spec.ParameterTypes)}]");
                Console.WriteLine($"      Return Type: {spec.ReturnType}");
                Console.WriteLine($"      Has Namespace: {(spec.NameSpace != null ? "Yes" : "No")}");
            }
        }
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
