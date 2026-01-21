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
    AnyType
}

// Типы символов
public enum KindType
{
    VarName,
    FuncName
}

// Информация о символе
public class SymbolInfo(string n, KindType k, SemanticType[] pars, SemanticType t)
{
    public SymbolInfo(string n, KindType k, SemanticType t) : this(n, k, null, t)
    {
    }

    public string Name { get; } = n;
    public KindType Kind { get; } = k;
    public SemanticType Type { get; } = t; // для функций - тип возвращаемого значения
    public SemanticType[] Params { get; } = pars; // для функций - типы параметров

    // Для генерации машинного кода
    public int VariableAddress = -1;
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

    public virtual RegularNameSpace CreateChildNamespace(string name)
    {
        var child = new RegularNameSpace
        {
            Parent = this,
            Name = name
        };
        Children.Add(child);
        return child;
    }

    public virtual LightWeightNameSpace CreateLightWeightChild(string name)
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
}

// Легковесное пространство имен (для временных областей видимости)
public class LightWeightNameSpace : NameSpace
{
    // Упрощенная версия для временных вычислений
}

// Глобальное пространство имен
public class GlobalNameSpace : NameSpace
{
    public GlobalNameSpace()
    {
        Name = "Global";
        Parent = null;
    }

    public override RegularNameSpace CreateChildNamespace(string name)
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
    public FunctionSpecialization(SemanticType[] parameterTypes, FunctionInfo function = null,
        SemanticType returnType = SemanticType.AnyType)
    {
        ParameterTypes = parameterTypes;
        Function = function;
        ReturnType = returnType;
        SpecializationId = function?.Specializations.Count ?? 0;
        BodyChecked = false;
    }

    public FunctionInfo Function { get; }
    public SemanticType[] ParameterTypes { get; set; }
    public SemanticType ReturnType { get; set; }
    public NameSpace NameSpace { get; set; }
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
        var functionName = "anonymous";

        // Безопасное получение имени функции
        if (Definition?.Name != null && !string.IsNullOrEmpty(Definition.Name.Name))
            functionName = Definition.Name.Name;
        else
            // Ищем в таблице функций по ссылке на this
            foreach (var entry in SymbolTree.FunctionTable)
                if (entry.Value == this)
                {
                    functionName = entry.Key;
                    break;
                }

        //Console.WriteLine($"Created spec for {functionName} with pars: {string.Join(' ',parameterTypes)}");

        // Создаем новую специализацию
        var newSpec = new FunctionSpecialization(parameterTypes, this)
        {
            SpecializationId = Specializations.Count
        };

        Specializations.Add(newSpec);

        // Для стандартных функций (кроме Main) не создаем пространство имен
        bool isStandardFunction = SymbolTree.IsStandardFunction(functionName) && functionName != "Main";
    
        if (!isStandardFunction)
        {
            // Создаем пространство имен только для пользовательских функций
            newSpec.NameSpace = SymbolTree.CreateFunctionNamespace(
                functionName,
                newSpec.SpecializationId,
                parameterTypes,
                Definition?.Params?.Select(x => x.Name).ToArray() ?? Array.Empty<string>()
            );
        }

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
    static SymbolTree()
    {
        Reset();
    }
    private static readonly HashSet<string> StandardFunctions = new()
    {
        "Sqrt", "Sin", "Cos", "Abs", "Round", "Pow", "Max", "Min", 
        "ToString", "Print", "Main"
    };

    public static bool IsStandardFunction(string functionName)
    {
        return StandardFunctions.Contains(functionName);
    }
    public static GlobalNameSpace Global { get; private set; }
    public static Dictionary<string, FunctionInfo> FunctionTable { get; private set; }

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
        FunctionTable["Sqrt"] = sqrtFunc;
        sqrtFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType }).ReturnType = SemanticType.DoubleType;
        sqrtFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType }).ReturnType = SemanticType.DoubleType;

        // Sin
        var sinFunc = new FunctionInfo();
        FunctionTable["Sin"] = sinFunc;
        sinFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType }).ReturnType = SemanticType.DoubleType;
        sinFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType }).ReturnType = SemanticType.DoubleType;

        // Cos
        var cosFunc = new FunctionInfo();
        FunctionTable["Cos"] = cosFunc;
        cosFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType }).ReturnType = SemanticType.DoubleType;
        cosFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType }).ReturnType = SemanticType.DoubleType;

        // Abs
        var absFunc = new FunctionInfo();
        FunctionTable["Abs"] = absFunc;
        absFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType }).ReturnType = SemanticType.DoubleType;
        absFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType }).ReturnType = SemanticType.IntType;

        // Round
        var roundFunc = new FunctionInfo();
        FunctionTable["Round"] = roundFunc;
        roundFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType }).ReturnType = SemanticType.IntType;

        // Pow
        var powFunc = new FunctionInfo();
        FunctionTable["Pow"] = powFunc;
        powFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType, SemanticType.DoubleType }).ReturnType =
            SemanticType.DoubleType;
        powFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType, SemanticType.IntType }).ReturnType =
            SemanticType.DoubleType;

        // Max
        var maxFunc = new FunctionInfo();
        FunctionTable["Max"] = maxFunc;
        maxFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType, SemanticType.DoubleType }).ReturnType =
            SemanticType.DoubleType;
        maxFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType, SemanticType.IntType }).ReturnType =
            SemanticType.IntType;

        // Min
        var minFunc = new FunctionInfo();
        FunctionTable["Min"] = minFunc;
        minFunc.FindOrCreateSpecialization(new[] { SemanticType.DoubleType, SemanticType.DoubleType }).ReturnType =
            SemanticType.DoubleType;
        minFunc.FindOrCreateSpecialization(new[] { SemanticType.IntType, SemanticType.IntType }).ReturnType =
            SemanticType.IntType;

        // ToString
        var toStringFunc = new FunctionInfo();
        FunctionTable["ToString"] = toStringFunc;
        toStringFunc.FindOrCreateSpecialization(new[] { SemanticType.AnyType }).ReturnType = SemanticType.StringType;
        // Print
        var printFunc = new FunctionInfo();
        FunctionTable["Print"] = printFunc;
        printFunc.FindOrCreateSpecialization(new[] { SemanticType.AnyType }).ReturnType = SemanticType.NoType;


        // Main
        var mainFunc = new FunctionInfo();
        FunctionTable["Main"] = mainFunc;
        mainFunc.FindOrCreateSpecialization(new[] { SemanticType.NoType }).ReturnType = SemanticType.NoType;
    }

    // Создание пространства имен для специализации функции
    public static NameSpace CreateFunctionNamespace(string functionName, int specializationId,
        SemanticType[] paramTypes, string[] paramNames)
    {
        // Создаем пространство имен для функции как дочернее к Global
        var funcNamespace = Global.CreateChildNamespace($"{functionName}_{specializationId}");

        // Добавляем параметры как переменные в это пространство имен
        if (paramNames != null && paramNames.Length > 0)
            // Проверяем, что у нас достаточно имен для всех параметров
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var paramName = i < paramNames.Length ? paramNames[i] : $"param{i}";
                funcNamespace.AddVariable(paramName, paramTypes[i]);
            }
        else if (paramTypes.Length > 0)
            // Если имен параметров нет, создаем стандартные имена
            for (var i = 0; i < paramTypes.Length; i++)
                funcNamespace.AddVariable($"param{i}", paramTypes[i]);

        // Связываем пространство имен со специализацией
        if (FunctionTable.TryGetValue(functionName, out var funcInfo) &&
            specializationId < funcInfo.Specializations.Count)
            funcInfo.Specializations[specializationId].NameSpace = funcNamespace;

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
            return funcInfo.Specializations[specializationId];
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


    // Для отладки - печать дерева пространств имен
    public static void PrintNamespaceTree(NameSpace ns, int indent = 0, bool isLast = true, string prefix = "")
    {
        var currentPrefix = prefix;

        // Добавляем символы для текущего уровня
        if (indent > 0) currentPrefix += isLast ? "+-- " : "|--- ";

        // Выводим текущее пространство имен
        Console.WriteLine($"{currentPrefix}[NS] {ns.Name} ({ns.GetType().Name})");

        // Подготавливаем префикс для следующего уровня
        var nextPrefix = prefix + (isLast ? "    " : "|   ");

        // Выводим переменные внутри пространства
        if (ns.Variables.Count > 0)
            foreach (var variable in ns.Variables)
            {
                var varPrefix = nextPrefix + "|   ";
                Console.WriteLine($"{varPrefix}|- {variable.Key}: {variable.Value.Type}");
            }

        // Рекурсивно выводим дочерние пространства
        for (var i = 0; i < ns.Children.Count; i++)
        {
            var childIsLast = i == ns.Children.Count - 1;
            PrintNamespaceTree(ns.Children[i], indent + 1, childIsLast, nextPrefix);
        }
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