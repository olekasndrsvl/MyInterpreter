namespace SmallMachine;

public enum Commands 
{
    nop,        // No operation
    
    ass,        // Universal assignment
    add,        // Addition
    sub,        // Subtraction  
    mul,        // Multiplication
    div,        // Division
    
    lt,         // Less than
    gt,         // Greater than
    eq,         // Equality
    neq,        // Not equal
    
    icmp,       // Integer compare with constant
    iif,        // Conditional jump
    go,         // Unconditional jump
    
    call,       // Function/procedure call
    param,      // Parameter passing
    label,      // Label marker
    
    stop        // Stop execution
}

public struct Value 
{
    public int i;
    public double r;
    public bool b;
    public VarType type;
    
    public override string ToString()
    {
        return type switch
        {
            VarType.Integer => i.ToString(),
            VarType.Real => r.ToString("F2"),
            VarType.Boolean => b.ToString(),
            _ => "undefined"
        };
    }
}

public class ThreeAddr 
{
    public Commands command;
    public int ResultIndex;
    
    public int Op1Index { get; set; }
    public int Op2Index { get; set; }
    
    public Value ConstantValue { get; set; }
    

    public string Label { get; set; }
    

    public ThreeAddr(Commands comm, int resultIndex, int op1Index, int op2Index)
    {
        command = comm;
        ResultIndex = resultIndex;
        Op1Index = op1Index;
        Op2Index = op2Index;
    }
    

    public ThreeAddr(Commands comm, int resultIndex, Value constValue)
    {
        command = comm;
        ResultIndex = resultIndex;
        ConstantValue = constValue;
    }
    
    public ThreeAddr(Commands comm, string label)
    {
        command = comm;
        Label = label;
    }
    

    public ThreeAddr(Commands comm, int conditionIndex, string label)
    {
        command = comm;
        Op1Index = conditionIndex;
        Label = label;
    }
    

    public ThreeAddr(Commands comm, int resultIndex, int op1Index, Value constValue)
    {
        command = comm;
        ResultIndex = resultIndex;
        Op1Index = op1Index;
        ConstantValue = constValue;
    }

    public ThreeAddr(Commands comm)
    {
        command = comm;
    }
}

public class SmallVirtualMachine
{
    public static Value[] Mem = new Value[1000];

    private static Dictionary<string, int> _labelAddresses = new Dictionary<string, int>();
    private static ThreeAddr[] _program;
    private static int _programCounter = 0;

    public static void InitializeMemory()
    {
        for (int i = 0; i < Mem.Length; i++)
        {
            Mem[i] = new Value { type = VarType.Integer };
        }
    }

    public static void EnsureMemorySize(int requiredSize)
    {
        if (requiredSize >= Mem.Length)
        {
            int newSize = Math.Max(requiredSize + 100, Mem.Length * 2);
            Array.Resize(ref Mem, newSize);

            for (int i = Mem.Length - (newSize - Mem.Length); i < Mem.Length; i++)
            {
                Mem[i] = new Value { type = VarType.Integer };
            }
        }
    }

    public static void MemoryDump(int count = 10)
    {
        Console.WriteLine("Memory Dump:");
        for (int i = 0; i < Math.Min(count, Mem.Length); i++)
        {
            if (Mem[i].i != 0 || Mem[i].r != 0.0 || Mem[i].b)
            {
                Console.WriteLine($"Mem[{i}] = {Mem[i]} (type: {Mem[i].type})");
            }
        }
    }

    public static void LoadProgram(List<ThreeAddr> program)
    {
        _labelAddresses.Clear();
        for (int i = 0; i < program.Count; i++)
        {
            if (program[i].command == Commands.label && !string.IsNullOrEmpty(program[i].Label))
            {
                _labelAddresses[program[i].Label] = i;
            }
        }

        _program = program.ToArray();
        _programCounter = 0;
    }

    public static void Run()
    {
        if (_program == null)
            throw new InvalidOperationException("Program not loaded");

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

    private static void ExecuteCommand(ThreeAddr cmd)
    {
        int maxIndex = Math.Max(cmd.ResultIndex, Math.Max(cmd.Op1Index, cmd.Op2Index));
        if (maxIndex >= 0)
        {
            EnsureMemorySize(maxIndex + 1);
        }

        switch (cmd.command)
        {
            case Commands.ass:
                if (cmd.ConstantValue.type != VarType.Integer ||
                    (cmd.ConstantValue.type == VarType.Integer && cmd.ConstantValue.i != 0) ||
                    cmd.ConstantValue.type == VarType.Real && cmd.ConstantValue.r != 0.0)
                {
                    Mem[cmd.ResultIndex] = cmd.ConstantValue;
                }
                else
                {
                    Mem[cmd.ResultIndex] = Mem[cmd.Op1Index];
                }

                break;

            case Commands.add:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].i = Mem[cmd.Op1Index].i + Mem[cmd.Op2Index].i;
                    Mem[cmd.ResultIndex].type = VarType.Integer;
                }
                else
                {
                    double op1 = Mem[cmd.Op1Index].type == VarType.Integer ? Mem[cmd.Op1Index].i : Mem[cmd.Op1Index].r;
                    double op2 = Mem[cmd.Op2Index].type == VarType.Integer ? Mem[cmd.Op2Index].i : Mem[cmd.Op2Index].r;
                    Mem[cmd.ResultIndex].r = op1 + op2;
                    Mem[cmd.ResultIndex].type = VarType.Real;
                }

                break;

            case Commands.sub:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].i = Mem[cmd.Op1Index].i - Mem[cmd.Op2Index].i;
                    Mem[cmd.ResultIndex].type = VarType.Integer;
                }
                else
                {
                    double op1 = Mem[cmd.Op1Index].type == VarType.Integer ? Mem[cmd.Op1Index].i : Mem[cmd.Op1Index].r;
                    double op2 = Mem[cmd.Op2Index].type == VarType.Integer ? Mem[cmd.Op2Index].i : Mem[cmd.Op2Index].r;
                    Mem[cmd.ResultIndex].r = op1 - op2;
                    Mem[cmd.ResultIndex].type = VarType.Real;
                }

                break;

            case Commands.mul:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].i = Mem[cmd.Op1Index].i * Mem[cmd.Op2Index].i;
                    Mem[cmd.ResultIndex].type = VarType.Integer;
                }
                else
                {
                    double op1 = Mem[cmd.Op1Index].type == VarType.Integer ? Mem[cmd.Op1Index].i : Mem[cmd.Op1Index].r;
                    double op2 = Mem[cmd.Op2Index].type == VarType.Integer ? Mem[cmd.Op2Index].i : Mem[cmd.Op2Index].r;
                    Mem[cmd.ResultIndex].r = op1 * op2;
                    Mem[cmd.ResultIndex].type = VarType.Real;
                }

                break;

            case Commands.div:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    if (Mem[cmd.Op2Index].i == 0)
                        throw new DivideByZeroException("Integer division by zero");
                    Mem[cmd.ResultIndex].i = Mem[cmd.Op1Index].i / Mem[cmd.Op2Index].i;
                    Mem[cmd.ResultIndex].type = VarType.Integer;
                }
                else
                {
                    double op1 = Mem[cmd.Op1Index].type == VarType.Integer ? Mem[cmd.Op1Index].i : Mem[cmd.Op1Index].r;
                    double op2 = Mem[cmd.Op2Index].type == VarType.Integer ? Mem[cmd.Op2Index].i : Mem[cmd.Op2Index].r;
                    if (op2 == 0.0)
                        throw new DivideByZeroException("Real division by zero");
                    Mem[cmd.ResultIndex].r = op1 / op2;
                    Mem[cmd.ResultIndex].type = VarType.Real;
                }

                break;

            case Commands.lt:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].i < Mem[cmd.Op2Index].i;
                }
                else
                {
                    double op1 = Mem[cmd.Op1Index].type == VarType.Integer ? Mem[cmd.Op1Index].i : Mem[cmd.Op1Index].r;
                    double op2 = Mem[cmd.Op2Index].type == VarType.Integer ? Mem[cmd.Op2Index].i : Mem[cmd.Op2Index].r;
                    Mem[cmd.ResultIndex].b = op1 < op2;
                }

                Mem[cmd.ResultIndex].type = VarType.Boolean;
                break;

            case Commands.gt:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].i > Mem[cmd.Op2Index].i;
                }
                else
                {
                    double op1 = Mem[cmd.Op1Index].type == VarType.Integer ? Mem[cmd.Op1Index].i : Mem[cmd.Op1Index].r;
                    double op2 = Mem[cmd.Op2Index].type == VarType.Integer ? Mem[cmd.Op2Index].i : Mem[cmd.Op2Index].r;
                    Mem[cmd.ResultIndex].b = op1 > op2;
                }

                Mem[cmd.ResultIndex].type = VarType.Boolean;
                break;

            case Commands.eq:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].i == Mem[cmd.Op2Index].i;
                }
                else if (Mem[cmd.Op1Index].type == VarType.Real && Mem[cmd.Op2Index].type == VarType.Real)
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].r == Mem[cmd.Op2Index].r;
                }
                else
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].b == Mem[cmd.Op2Index].b;
                }

                Mem[cmd.ResultIndex].type = VarType.Boolean;
                break;

            case Commands.neq:
                if (Mem[cmd.Op1Index].type == VarType.Integer && Mem[cmd.Op2Index].type == VarType.Integer)
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].i != Mem[cmd.Op2Index].i;
                }
                else if (Mem[cmd.Op1Index].type == VarType.Real && Mem[cmd.Op2Index].type == VarType.Real)
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].r != Mem[cmd.Op2Index].r;
                }
                else
                {
                    Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].b != Mem[cmd.Op2Index].b;
                }

                Mem[cmd.ResultIndex].type = VarType.Boolean;
                break;

            case Commands.icmp:
                Mem[cmd.ResultIndex].b = Mem[cmd.Op1Index].i >= cmd.ConstantValue.i;
                Mem[cmd.ResultIndex].type = VarType.Boolean;
                break;

            case Commands.iif:
                if (Mem[cmd.Op1Index].b)
                {
                    if (_labelAddresses.TryGetValue(cmd.Label, out int address))
                    {
                        _programCounter = address - 1;
                    }
                    else
                    {
                        throw new Exception($"Label '{cmd.Label}' not found");
                    }
                }

                break;

            case Commands.go:
                if (_labelAddresses.TryGetValue(cmd.Label, out int jumpAddress))
                {
                    _programCounter = jumpAddress - 1;
                }
                else
                {
                    throw new Exception($"Label '{cmd.Label}' not found");
                }

                break;

            case Commands.label:
                break;

            case Commands.stop:
                break;

            case Commands.nop:
                break;

            default:
                throw new NotImplementedException($"Command {cmd.command} not implemented");
        }
    }

    public static void RunProgram(List<ThreeAddr> program)
    {
        InitializeMemory();
        LoadProgram(program);
        Run();
    }


    public static Value IntValue(int value) => new Value { i = value, type = VarType.Integer };
    public static Value RealValue(double value) => new Value { r = value, type = VarType.Real };
    public static Value BoolValue(bool value) => new Value { b = value, type = VarType.Boolean };

}

public enum VarType
{
    Integer,
    Real,
    Boolean
}