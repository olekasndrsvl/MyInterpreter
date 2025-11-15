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

        public override string ToString() => $"{Name} ({Kind}, {Type})";
    }

    // Класс для хранения специализации функции
    public class FunctionSpecialization
    {
        public SemanticType[] ParameterTypes { get; set; }
        public SemanticType ReturnType { get; set; }
        public Dictionary<string, SemanticType> LocalVariableTypes { get; } = new Dictionary<string, SemanticType>();
        public int SpecializationId { get; set; }
        public bool BodyChecked { get; set; }
        public FunctionSpecialization()
        {
        }
        public FunctionSpecialization(SemanticType[] parameterTypes, SemanticType returnType = SemanticType.AnyType)
        {
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is FunctionSpecialization other)
            {
                if (ParameterTypes.Length != other.ParameterTypes.Length)
                    return false;
                    
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    if (ParameterTypes[i] != other.ParameterTypes[i])
                        return false;
                }
                return ReturnType == other.ReturnType;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var type in ParameterTypes)
                {
                    hash = hash * 31 + type.GetHashCode();
                }
                hash = hash * 31 + ReturnType.GetHashCode();
                return hash;
            }
        }
    }

    // Информация о функции
    public class FunctionInfo
    {
        public FuncDefNode Definition { get; set; }
        public List<FunctionSpecialization> Specializations { get; set; } = new List<FunctionSpecialization>();
        
        public FunctionSpecialization FindOrCreateSpecialization(SemanticType[] parameterTypes)
        {
            foreach (var spec in Specializations)
            {
                if (AreParameterTypesCompatible(spec.ParameterTypes, parameterTypes))
                {
                    return spec;
                }
            }
            
            var newSpec = new FunctionSpecialization(parameterTypes);
            Specializations.Add(newSpec);
            newSpec.SpecializationId = Specializations.Count - 1;
            return newSpec;
        }
        
        private bool AreParameterTypesCompatible(SemanticType[] paramTypes, SemanticType[] argTypes)
        {
            if (paramTypes.Length != argTypes.Length)
                return false;

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (!SymbolTable.AreTypesCompatible(paramTypes[i], argTypes[i]))
                    return false;
            }
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
        
        public static SemanticType[] NumTypes = { SemanticType.IntType, SemanticType.DoubleType };
    }

    // Основная таблица символов
    public static class SymbolTable
    {
        public static Dictionary<string, SymbolInfo> SymTable = new Dictionary<string, SymbolInfo>();
        public static List<RuntimeValue> VarValues = new List<RuntimeValue>();
        
        // Новая структура для хранения информации о функциях
        public static Dictionary<string, FunctionInfo> FunctionTable = new Dictionary<string, FunctionInfo>();

        // Таблица стандартных функций
        private static void InitStandardFunctionsTable()
        {
            SymTable["Sqrt"] = new SymbolInfo("Sqrt", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType);

            SymTable["Print"] = new SymbolInfo("Print", KindType.FuncName,
                new SemanticType[] { SemanticType.AnyType }, SemanticType.NoType);

            // Добавим другие стандартные функции
            SymTable["Sin"] = new SymbolInfo("Sin", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType);

            SymTable["Cos"] = new SymbolInfo("Cos", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType);

            SymTable["Abs"] = new SymbolInfo("Abs", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.DoubleType);

            SymTable["Round"] = new SymbolInfo("Round", KindType.FuncName,
                new SemanticType[] { SemanticType.DoubleType }, SemanticType.IntType);
            
            // Инициализируем FunctionTable для стандартных функций
            FunctionTable["Sqrt"] = new FunctionInfo();
            FunctionTable["Print"] = new FunctionInfo();
            FunctionTable["Sin"] = new FunctionInfo();
            FunctionTable["Cos"] = new FunctionInfo();
            FunctionTable["Abs"] = new FunctionInfo();
            FunctionTable["Round"] = new FunctionInfo();
        }

        // Методы для работы с таблицей символов
        public static void AddVariable(string name, SemanticType type)
        {
            PrintSymbolTable();
            if (SymTable.ContainsKey(name))
                throw new InvalidOperationException($"Variable '{name}' already declared");

         
            VarValues.Add(CreateDefaultValue(type));
            SymTable[name] = new SymbolInfo(name, KindType.VarName, type);
         
        }

        public static void AddFunction(string name, SemanticType[] paramTypes, SemanticType returnType)
        {
            SymTable[name] = new SymbolInfo(name, KindType.FuncName, paramTypes, returnType);
            
            // Также добавляем в FunctionTable если еще нет
            if (!FunctionTable.ContainsKey(name))
            {
                FunctionTable[name] = new FunctionInfo();
            }
        }
        
        // Новый метод для регистрации определения функции
        public static void RegisterFunctionDefinition(string name, FuncDefNode definition)
        {
            if (!FunctionTable.ContainsKey(name))
            {
                FunctionTable[name] = new FunctionInfo();
            }
            FunctionTable[name].Definition = definition;
        }
        
        // Новый метод для получения специализации функции
        public static FunctionSpecialization GetFunctionSpecialization(string name, SemanticType[] argTypes)
        {
            if (FunctionTable.ContainsKey(name))
            {
                return FunctionTable[name].FindOrCreateSpecialization(argTypes);
            }
            return null;
        }
        
        // Новый метод для получения всех специализаций функции
        public static List<FunctionSpecialization> GetFunctionSpecializations(string name)
        {
            if (FunctionTable.ContainsKey(name))
            {
                return FunctionTable[name].Specializations;
            }
            return new List<FunctionSpecialization>();
        }

        public static bool Contains(string name) => SymTable.ContainsKey(name);

        public static SymbolInfo Get(string name)
        {
            if (SymTable.TryGetValue(name, out var symbol))
                return symbol;
            throw new KeyNotFoundException($"Symbol '{name}' not found");
        }

        public static void ResetSymbolTable()
        {
            SymTable.Clear();
            VarValues.Clear();
            FunctionTable.Clear();
        }
        
     

        public static void SetValue(string name, RuntimeValue value)
        {
            var symbol = Get(name);
            if (symbol.Kind != KindType.VarName)
                throw new InvalidOperationException($"'{name}' is not a variable");
            //VarValues[symbol.Index] = value;
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
                SemanticType.AnyType => RuntimeValueHelper.Value(0),
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

        // For debug
        public static void PrintSymbolTable()
        {
            Console.WriteLine("Symbol Table:");
            foreach (var symbol in SymTable.Values)
            {
                Console.WriteLine($"  {symbol.Name}: {symbol.Kind}, {symbol.Type}, ");
            }
        }

        public static void PrintFunctionTable()
        {
            Console.WriteLine("Function Table:");
            foreach (var function in FunctionTable)
            {
                Console.WriteLine($"  {function.Key}:");
                if (function.Value.Definition != null)
                {
                    Console.WriteLine($"    Definition: Present");
                }
                Console.WriteLine($"    Specializations: {function.Value.Specializations.Count}");
                foreach (var spec in function.Value.Specializations)
                {
                    Console.WriteLine($"      Specialization {spec.SpecializationId}:");
                    Console.WriteLine($"        Parameters: [{string.Join(", ", spec.ParameterTypes)}]");
                    Console.WriteLine($"        Return Type: {spec.ReturnType}");
                    Console.WriteLine($"        Local Variables: {spec.LocalVariableTypes.Count}");
                    foreach (var localVar in spec.LocalVariableTypes)
                    {
                        Console.WriteLine($"          {localVar.Key}: {localVar.Value}");
                    }
                }
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