namespace MyInterpreter;

public enum Commands
{
    // Прямая адресация (все операнды прямые)
    iass,
    rass,
    bass,
    icass,
    rcass,
    bcass,
    iadd,
    radd,
    isub,
    rsub,
    imul,
    rmul,
    idiv,
    rdiv,
    ilt,
    rlt,
    igt,
    rgt,
    ieq,
    req,
    beq,
    ineq,
    rneq,
    bneq,
    ic2ge,
    rc2ge,
    ic2le,
    rc2le,
    iassadd,
    rassadd,
    iasssub,
    rasssub,
    citr,
    iif,
    ifn,
    go,
    call,
    push,
    pop,
    creturn,
    label,
    stop,
    movout,
    movin
}

public struct Value
{
    public int i;
    public double r;
    public bool b;
}

public class ThreeAddr
{
    public Commands command;

    public int MemIndex; // Индекс в массиве памяти

    private ThreeAddr()
    {
    }

    public bool isInDirectAddressing1 { get; set; }

    // Для хранения типа значения
    public VarType Type { get; set; }

    // Для непосредственных значений
    public int IValue { get; set; }
    public double RValue { get; set; }
    public bool BValue { get; set; }

    // Для хранения меток (для goto и условных переходов)
    public string Label { get; set; }

    // Для бинарных операций
    public int Op1Index { get; set; }
    public bool isInDirectAddressing2 { get; set; }
    public int Op2Index { get; set; }
    public bool isInDirectAddressing3 { get; set; }

    // Для команд без параметров (nop, stop)
    public static ThreeAddr Create(Commands comm)
    {
        return new ThreeAddr { command = comm };
    }

    // Для команд с одним индексом памяти
    public static ThreeAddr Create(Commands comm, int memIndex, bool isInDirectAddress = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, isInDirectAddressing1 = isInDirectAddress };
    }

    // Для константных присваиваний (icass, rcass)
    public static ThreeAddr CreateConst(Commands comm, int memIndex, int ivalue, bool isInDirectAddress = false)
    {
        return new ThreeAddr
            { command = comm, MemIndex = memIndex, IValue = ivalue, isInDirectAddressing1 = isInDirectAddress };
    }

    public static ThreeAddr CreateConst(Commands comm, int memIndex, double rvalue, bool isInDirectAddress = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, RValue = rvalue, isInDirectAddressing1 = false };
    }

    // Для команд с меткой (label, go)
    public static ThreeAddr Create(Commands comm, string label)
    {
        return new ThreeAddr { command = comm, Label = label };
    }

    // Для условных переходов (iif с индексом и меткой)
    public static ThreeAddr Create(Commands comm, int memIndex, string label, bool isInDirectAddress = false)
    {
        return new ThreeAddr
            { command = comm, MemIndex = memIndex, Label = label, isInDirectAddressing1 = isInDirectAddress };
    }

    // Для присваиваний между переменными (iass, rass, bass)
    public static ThreeAddr CreateAssign(Commands comm, int destIndex, int srcIndex, bool isInDirectAddress1 = false,
        bool isInDirectAddress2 = false)
    {
        return new ThreeAddr
        {
            command = comm, MemIndex = destIndex, Op1Index = srcIndex, isInDirectAddressing1 = isInDirectAddress1,
            isInDirectAddressing2 = isInDirectAddress2
        };
    }

    // Для конвертации типов
    public static ThreeAddr CreateConvert(Commands comm, int srcIndex, int destIndex, bool isInDirectAddress1 = false,
        bool isInDirectAddress2 = false)
    {
        return new ThreeAddr
        {
            command = comm, MemIndex = srcIndex, Op1Index = destIndex, isInDirectAddressing1 = isInDirectAddress1,
            isInDirectAddressing2 = isInDirectAddress2
        };
    }

    // Для бинарных операций (iadd, isub, etc)
    public static ThreeAddr CreateBinary(Commands comm, int op1Index, int op2Index, int resIndex,
        bool isInDirectAddress1 = false, bool isInDirectAddress2 = false, bool isInDirectAddress3 = false)
    {
        return new ThreeAddr
        {
            command = comm, Op1Index = op1Index, Op2Index = op2Index, MemIndex = resIndex,
            isInDirectAddressing1 = isInDirectAddress1, isInDirectAddressing2 = isInDirectAddress2,
            isInDirectAddressing3 = isInDirectAddress3
        };
    }

    // Для операций присваивания с операцией (iassadd, iasssub, etc)
    public static ThreeAddr CreateAssignOp(Commands comm, int destIndex, int srcIndex1, int srcIndex2,
        bool isInDirectAddress1 = false, bool isInDirectAddress2 = false, bool isInDirectAddress3 = false)
    {
        return new ThreeAddr
        {
            command = comm, MemIndex = destIndex, Op1Index = srcIndex1, Op2Index = srcIndex2,
            isInDirectAddressing1 = isInDirectAddress1, isInDirectAddressing2 = isInDirectAddress2,
            isInDirectAddressing3 = isInDirectAddress3
        };
    }
}

public class VirtualMachine
{
    public static Value[] Mem = new Value[1000];

    public static Dictionary<string, int> FrameSizes = new();

    // Регистры виртуальной машины
    private static int _currentFrameIndex; // SP - указатель вершины стека

    private static Value _returnValueRegister;

    private static readonly Stack<Tuple<int, int>> _currentFrameStack = new();

    // Стек вызовов - храним только адреса возврата
    private static readonly Stack<int> _returnAddressStack = new();

    private static readonly Dictionary<string, int> _labelAddresses = new();
    private static ThreeAddr[] _program;
    private static int _programCounter;

    private static readonly Dictionary<string, Action> _standardFunctions = new()
    {
        { "print", () => ExecutePrintFunction() }
    };

    // Вспомогательные методы для работы с косвенной адресацией
    private static int ResolveAddress(int index, bool isIndirect)
    {
        if (isIndirect) return index + _currentFrameIndex;
        return index;
    }

    private static ref Value GetValueRef(int index, bool isIndirect)
    {
        var resolvedIndex = ResolveAddress(index, isIndirect);
        EnsureMemorySize(resolvedIndex + 1);
        return ref Mem[resolvedIndex];
    }

    private static int GetIntValue(int index, bool isIndirect)
    {
        return GetValueRef(index, isIndirect).i;
    }

    private static double GetRealValue(int index, bool isIndirect)
    {
        return GetValueRef(index, isIndirect).r;
    }

    private static bool GetBoolValue(int index, bool isIndirect)
    {
        return GetValueRef(index, isIndirect).b;
    }

    private static void SetIntValue(int index, bool isIndirect, int value)
    {
        GetValueRef(index, isIndirect).i = value;
    }

    private static void SetRealValue(int index, bool isIndirect, double value)
    {
        GetValueRef(index, isIndirect).r = value;
    }

    private static void SetBoolValue(int index, bool isIndirect, bool value)
    {
        GetValueRef(index, isIndirect).b = value;
    }

    public static void GiveFrameSize(Dictionary<string, int> dict)
    {
        FrameSizes = dict;
        
        _currentFrameStack.Push(new Tuple<int, int>(FrameSizes["GlobalVariables"], FrameSizes["MainFrame"]));
        FrameSizes["print"] = 1;
        _currentFrameIndex=FrameSizes["GlobalVariables"];
    }

    public static void InitializeMemory()
    {
        for (var i = 0; i < Mem.Length; i++) Mem[i] = new Value();
    }

    public static void EnsureMemorySize(int requiredSize)
    {
        if (requiredSize >= Mem.Length)
        {
            var newSize = Math.Max(requiredSize + 100, Mem.Length * 2);
            Array.Resize(ref Mem, newSize);

            for (var i = Mem.Length - (newSize - Mem.Length); i < Mem.Length; i++) Mem[i] = new Value();
        }
    }

    public static void MemoryDump(int count = 10)
    {
        CompilerForm.Instance.ChangeOutputBoxText("Memory Dump:\n");
        for (var i = 0; i < Math.Min(count, Mem.Length); i++)
            if (Mem[i].i != 0 || Mem[i].r != 0.0 || Mem[i].b)
                CompilerForm.Instance.ChangeOutputBoxText($"Mem[{i}] = i:{Mem[i].i}, r:{Mem[i].r}, b:{Mem[i].b} \n");
    }

    public static void LoadProgram(List<ThreeAddr> program)
    {
        _labelAddresses.Clear();
        for (var i = 0; i < program.Count; i++)
            if (program[i].command == Commands.label && !string.IsNullOrEmpty(program[i].Label))
                _labelAddresses[program[i].Label] = i;

        _program = program.ToArray();
        _programCounter = 0;
    }

    public static void Run()
    {
        var temp = 0;
        if (_program == null)
            throw new InvalidOperationException("Программа не загружена!");

        _programCounter = 0;

        while (_programCounter < _program.Length)
        {
            var command = _program[_programCounter];
            ExecuteCommand(command);

            if (command.command == Commands.stop)
                break;

            _programCounter++;
        }
    }

    public static void ResetVirtualMachine()
    {
        _programCounter = 0;
        InitializeMemory();
        _labelAddresses.Clear();
        _program = null;
    }

    private static void ExecuteCommand(ThreeAddr cmd)
    {
        var TOLERANCE = double.Epsilon;
        switch (cmd.command)
        {
            case Commands.icass when cmd.IValue != 0:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1, cmd.IValue);
                break;

            case Commands.icass:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2));
                break;

            case Commands.rcass when cmd.RValue != 0.0:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1, cmd.RValue);
                break;

            case Commands.rcass:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2));
                break;

            case Commands.bcass:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetBoolValue(cmd.Op1Index, cmd.isInDirectAddressing2));
                break;

            case Commands.iassadd:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) +
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rassadd:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) +
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.iadd:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) +
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.radd:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) +
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.isub:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) -
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rsub:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) -
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.imul:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) *
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rmul:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) *
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.idiv:
                var divisorInt = GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3);
                if (divisorInt == 0)
                    throw new DivideByZeroException("Integer division by zero");
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) / divisorInt);
                break;

            case Commands.rdiv:
                var divisorReal = GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3);
                if (divisorReal == 0.0)
                    throw new DivideByZeroException("Real division by zero");
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) / divisorReal);
                break;

            case Commands.ilt:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) <
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rlt:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) <
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.igt:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) >
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rgt:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) >
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.ieq:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) ==
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.req:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    Math.Abs(GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) -
                             GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3)) < TOLERANCE);
                break;

            case Commands.beq:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetBoolValue(cmd.Op1Index, cmd.isInDirectAddressing2) ==
                    GetBoolValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.ineq:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) !=
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rneq:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    Math.Abs(GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) -
                             GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3)) >= TOLERANCE);
                break;

            case Commands.bneq:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetBoolValue(cmd.Op1Index, cmd.isInDirectAddressing2) !=
                    GetBoolValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.ic2ge:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) >=
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rc2ge:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) >=
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.ic2le:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) <=
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rc2le:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) <=
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.iif:
                if (GetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1))
                {
                    if (_labelAddresses.TryGetValue(cmd.Label, out var address))
                        _programCounter = address - 1;
                    else
                        throw new Exception($"Label '{cmd.Label}' not found");
                }

                break;

            case Commands.ifn:
                if (!GetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1))
                {
                    if (_labelAddresses.TryGetValue(cmd.Label, out var address))
                        _programCounter = address - 1;
                    else
                        throw new Exception($"Label '{cmd.Label}' not found");
                }

                break;

            case Commands.iass:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2));
                break;

            case Commands.rass:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2));
                break;

            case Commands.bass:
                SetBoolValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetBoolValue(cmd.Op1Index, cmd.isInDirectAddressing2));
                break;

            case Commands.iasssub:
                SetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetIntValue(cmd.Op1Index, cmd.isInDirectAddressing2) -
                    GetIntValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.rasssub:
                SetRealValue(cmd.MemIndex, cmd.isInDirectAddressing1,
                    GetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2) -
                    GetRealValue(cmd.Op2Index, cmd.isInDirectAddressing3));
                break;

            case Commands.citr:
                SetRealValue(cmd.Op1Index, cmd.isInDirectAddressing2,
                    GetIntValue(cmd.MemIndex, cmd.isInDirectAddressing1));
                break;

            case Commands.call:

                _currentFrameIndex += _currentFrameStack.Peek().Item2;
                _currentFrameStack.Push(new Tuple<int, int>(_currentFrameIndex, FrameSizes[cmd.Label]));


                if (_standardFunctions.ContainsKey(cmd.Label))
                {
                    _standardFunctions[cmd.Label]();
                }
                else if (_labelAddresses.TryGetValue(cmd.Label, out var callAddress))
                {
                    _returnAddressStack.Push(_programCounter);
                    _programCounter = callAddress - 1;
                }
                else
                {
                    throw new Exception($"Function or procedure '{cmd.Label}' not found");
                }

                break;

            case Commands.creturn:
                var returnAddress = _returnAddressStack.Pop();
                _programCounter = returnAddress;
                _currentFrameStack.Pop();
                _currentFrameIndex = _currentFrameStack.Peek().Item1;

                break;
            case Commands.movout:
                var tmp = new Value
                {
                    b = _returnValueRegister.b,
                    i = _returnValueRegister.i,
                    r = _returnValueRegister.r
                };

                if (cmd.isInDirectAddressing1)
                    Mem[cmd.MemIndex + _currentFrameIndex] = tmp;
                else
                    Mem[cmd.MemIndex] = tmp;
                Console.WriteLine(
                    $"reg: i:{_returnValueRegister.i} r:{_returnValueRegister.r} b: {_returnValueRegister.b}");
                break;

            case Commands.movin:
                if (cmd.isInDirectAddressing1)
                    _returnValueRegister = Mem[cmd.MemIndex + _currentFrameIndex];
                else
                    _returnValueRegister = Mem[cmd.MemIndex];

                break;

            case Commands.go:
                if (_labelAddresses.TryGetValue(cmd.Label, out var jumpAddress))
                    _programCounter = jumpAddress - 1;
                else
                    throw new Exception($"Label '{cmd.Label}' not found");
                break;


            case Commands.label:
                break;

            case Commands.stop:
                break;

            default:
                throw new NotImplementedException($"Command {cmd.command} not implemented");
        }
    }

    private static void ExecutePrintFunction()
    {
        Console.WriteLine("x: " + Mem[0].i);
        Console.WriteLine("print:" + _currentFrameIndex);
        if (_currentFrameIndex > 0)
        {
            var value = Mem[_currentFrameIndex];
            if (Math.Abs(value.r) > 0.000001)
                CompilerForm.Instance.ChangeOutputBoxText(value.r.ToString("F6") + '\n');
            else
                CompilerForm.Instance.ChangeOutputBoxText(value.i.ToString() + '\n');
            _currentFrameStack.Pop();
            _currentFrameIndex = _currentFrameStack.Peek().Item1;
        }
        else
        {
            throw new CompilerExceptions.UnExpectedException(
                "Вызвана стандартная функция print(), но фрейм для функции не был объявлен!");
        }
    }

    public static void RunProgram(List<ThreeAddr> program)
    {
        InitializeMemory();
        LoadProgram(program);
        Run();
    }
}

public enum VarType
{
    Integer,
    Real,
    Boolean
}