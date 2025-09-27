namespace MyInterpreter
{
    // Маркер типа времени выполнения
    public enum RTTypeMarker
    {
        Int,
        Real,
        Bool
    }

    // Значение времени выполнения
    public class RuntimeValue
    {
        public int I { get; set; }
        public double R { get; set; }
        public bool B { get; set; }
        public RTTypeMarker TypeMarker { get; set; }

        public RuntimeValue(int ii)
        {
            I = ii;
            TypeMarker = RTTypeMarker.Int;
        }

        public RuntimeValue(double rr)
        {
            R = rr;
            TypeMarker = RTTypeMarker.Real;
        }

        public RuntimeValue(bool bb)
        {
            B = bb;
            TypeMarker = RTTypeMarker.Bool;
        }

        public bool IsInt => TypeMarker == RTTypeMarker.Int;
        public bool IsReal => TypeMarker == RTTypeMarker.Real;
        public bool IsBool => TypeMarker == RTTypeMarker.Bool;

        public override string ToString()
        {
            return TypeMarker switch
            {
                RTTypeMarker.Int => I.ToString(),
                RTTypeMarker.Real => R.ToString(),
                RTTypeMarker.Bool => B.ToString(),
                _ => "Unknown"
            };
        }
    }

    // Вспомогательные методы для создания RuntimeValue
    public static class RuntimeValueHelper
    {
        public static RuntimeValue Value(int i) => new RuntimeValue(i);
        public static RuntimeValue Value(double r) => new RuntimeValue(r);
        public static RuntimeValue Value(bool b) => new RuntimeValue(b);
    }

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
        public string Name { get; }
        public KindType Kind { get; }
        public SemanticType Type { get; } // для функций - тип возвращаемого значения
        public SemanticType[] Params { get; } // для функций - типы параметров
        public int Index { get; } // индекс переменной в таблице VarValues
        public RuntimeValue Value { get; set; } // для интерпретатора

        public SymbolInfo(string n, KindType k, SemanticType t, int ind)
        {
            Name = n;
            Kind = k;
            Type = t;
            Index = ind;
        }

        public SymbolInfo(string n, KindType k, SemanticType[] pars, SemanticType t, int ind)
        {
            Name = n;
            Kind = k;
            Params = pars;
            Type = t;
            Index = ind;
        }

        public override string ToString() => $"{Name} ({Kind}, {Type})";
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
        public static SemanticType[] NumTypes = { SemanticType.IntType, SemanticType.DoubleType };
    }

    // Основная таблица символов
    public static class SymbolTable
    {
        public static Dictionary<string, SymbolInfo> SymTable = new Dictionary<string, SymbolInfo>();
        public static List<RuntimeValue> VarValues = new List<RuntimeValue>();

        // Таблица стандартных функций
        private static void InitStandardFunctionsTable()
        {
            SymTable["Sqrt"] = new SymbolInfo("Sqrt", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType, -1);

            SymTable["Print"] = new SymbolInfo("Print", KindType.FuncName,
                new SemanticType[] { SemanticType.AnyType }, SemanticType.NoType, -1);

            // Добавим другие стандартные функции
            SymTable["Sin"] = new SymbolInfo("Sin", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType, -1);

            SymTable["Cos"] = new SymbolInfo("Cos", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType, -1);

            SymTable["Abs"] = new SymbolInfo("Abs", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType, -1);

            SymTable["Round"] = new SymbolInfo("Round", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.IntType, -1);
        }

        // Методы для работы с таблицей символов
        public static int AddVariable(string name, SemanticType type)
        {
            if (SymTable.ContainsKey(name))
                throw new InvalidOperationException($"Variable '{name}' already declared");

            int index = VarValues.Count;
            VarValues.Add(CreateDefaultValue(type));
            SymTable[name] = new SymbolInfo(name, KindType.VarName, type, index);
            return index;
        }

        public static void AddFunction(string name, SemanticType[] paramTypes, SemanticType returnType)
        {
            SymTable[name] = new SymbolInfo(name, KindType.FuncName, paramTypes, returnType, -1);
        }

        public static bool Contains(string name) => SymTable.ContainsKey(name);

        public static SymbolInfo Get(string name)
        {
            if (SymTable.TryGetValue(name, out var symbol))
                return symbol;
            throw new KeyNotFoundException($"Symbol '{name}' not found");
        }

        public static RuntimeValue GetValue(string name)
        {
            var symbol = Get(name);
            if (symbol.Kind != KindType.VarName)
                throw new InvalidOperationException($"'{name}' is not a variable");
            return VarValues[symbol.Index];
        }

        public static void SetValue(string name, RuntimeValue value)
        {
            var symbol = Get(name);
            if (symbol.Kind != KindType.VarName)
                throw new InvalidOperationException($"'{name}' is not a variable");
            VarValues[symbol.Index] = value;
        }

        public static void SetIntValue(string name, int value)
        {
            SetValue(name, RuntimeValueHelper.Value(value));
        }

        public static void SetDoubleValue(string name, double value)
        {
            SetValue(name, RuntimeValueHelper.Value(value));
        }

        public static void SetBoolValue(string name, bool value)
        {
            SetValue(name, RuntimeValueHelper.Value(value));
        }

        // Создание значения по умолчанию для типа
        private static RuntimeValue CreateDefaultValue(SemanticType type)
        {
            return type switch
            {
                SemanticType.IntType => RuntimeValueHelper.Value(0),
                SemanticType.DoubleType => RuntimeValueHelper.Value(0.0),
                SemanticType.BoolType => RuntimeValueHelper.Value(false),
                _ => throw new ArgumentException($"Unsupported type: {type}")
            };
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

        // Статический конструктор для инициализации
        static SymbolTable()
        {
            InitStandardFunctionsTable();
        }

        // Методы для отладки
        public static void PrintSymbolTable()
        {
            Console.WriteLine("Symbol Table:");
            foreach (var symbol in SymTable.Values)
            {
                Console.WriteLine($"  {symbol.Name}: {symbol.Kind}, {symbol.Type}, Index: {symbol.Index}");
            }
        }

        public static void PrintVariableValues()
        {
            Console.WriteLine("Variable Values:");
            for (int i = 0; i < VarValues.Count; i++)
            {
                Console.WriteLine($"  [{i}]: {VarValues[i]} ({VarValues[i].TypeMarker})");
            }
        }
    }
}