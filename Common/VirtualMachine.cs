using Microsoft.VisualBasic.Logging;

namespace MyInterpreter;

public enum Commands 
{
  
    
    //=
    iass,       // Integer assignment
    rass,       // Real assignment  
    bass,       // Boolean assignment
    
    // = const
    icass,      // Integer constant assignment
    rcass,      // Real constant assignment
    bcass,      // Boolean constant assignment
    
    //+=
    iassadd,    // Integer assignment with addition
    rassadd,    // Real assignment with addition
    
    //-=
    iasssub,    // Integer assignment with subtraction
    rasssub,    // Real assignment with subtraction
    
    // x+y
    iadd,       // Integer addition
    radd,       // Real addition
    
    // x-y
    isub,       // Integer subtraction
    rsub,       // Real subtraction
    
    //x*y
    imul,       // Integer multiplication
    rmul,       // Real multiplication
    
    // x/y
    idiv,       // Integer division
    rdiv,       // Real division
    
    // x<y
    ilt,        // Integer less than
    rlt,        // Real less than
    
    //x>y
    igt,        // Integer greater than
    rgt,        // Real greater than
    
    //x==y
    ieq,        // Integer equality
    req,        // Real equality
    beq,        // Boolean equality
    
    // x!=y
    ineq,   // Integer non equality
    rneq,   // Real equality
    bneq,   // Boolean equality
    //x>=y
    ic2ge,      // Integer compare to greater or equal
    rc2ge,      // Real compare to greater or equal
    
    // x<=y
    ic2le,      // Integer compare to less or equal
    rc2le,      //   Real compare to less or equal
    
    // convert
    citr,       // convert integer to real
    // if
    iif,        // Conditional jump
    
    // if not
    ifn,
    // JMP
    go,         // Unconditional jump
    
    
    call,       // Function/procedure call
    param,      // Parameter passing
    
    push,
    pop,
    
    
    label,      // Label marker
    stop        // Stop execution
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
    public int Op2Index { get; set; }
    
     // Для команд без параметров (nop, stop)
    public static ThreeAddr Create(Commands comm)
    {
        return new ThreeAddr { command = comm };
    }
    
    // Для команд с одним индексом памяти
    public static ThreeAddr Create(Commands comm, int memIndex)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex };
    }
    
    // Для константных присваиваний (icass, rcass)
    public static ThreeAddr CreateConst(Commands comm, int memIndex, int ivalue)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, IValue = ivalue };
    }
    
    public static ThreeAddr CreateConst(Commands comm, int memIndex, double rvalue)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, RValue = rvalue };
    }
    
    // Для команд с меткой (label, go)
    public static ThreeAddr Create(Commands comm, string label)
    {
        return new ThreeAddr { command = comm, Label = label };
    }
    
    // Для условных переходов (iif с индексом и меткой)
    public static ThreeAddr Create(Commands comm, int memIndex, string label)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, Label = label };
    }
    
    // Для присваиваний между переменными (iass, rass, bass)
    public static ThreeAddr CreateAssign(Commands comm, int destIndex, int srcIndex)
    {
        return new ThreeAddr { command = comm, MemIndex = destIndex, Op1Index = srcIndex };
    }
    
    // Для конвертации типов
    public static ThreeAddr CreateConvert(Commands comm, int srcIndex, int destIndex)
    {
        return new ThreeAddr { command = comm, MemIndex = srcIndex, Op1Index = destIndex };
    }
    
    // Для бинарных операций (iadd, isub, etc)
    public static ThreeAddr CreateBinary(Commands comm, int op1Index, int op2Index, int resIndex)
    {
        return new ThreeAddr { command = comm, Op1Index = op1Index, Op2Index = op2Index, MemIndex = resIndex };
    }
    
    // Для операций присваивания с операцией (iassadd, iasssub, etc)
    public static ThreeAddr CreateAssignOp(Commands comm, int destIndex, int srcIndex1, int srcIndex2)
    {
        return new ThreeAddr { command = comm, MemIndex = destIndex, Op1Index = srcIndex1, Op2Index = srcIndex2 };
    }
    private ThreeAddr() { }
}

public class VirtualMachine 
{
    public static Value[] Mem = new Value[1000];
    private static Stack<int> _callStack = new Stack<int>(); // для хранения адресов возврата
    private static Stack<Value> _paramStack = new Stack<Value>(); // для передачи параметров

    private static Dictionary<string, int> _labelAddresses = new Dictionary<string, int>();
    private static ThreeAddr[] _program;
    private static int _programCounter = 0;
    private static Dictionary<string, Action> _standardFunctions = new Dictionary<string, Action>
    {
        { "print", () => ExecutePrintFunction() }
    };
    public static void InitializeMemory()
    {
        for (int i = 0; i < Mem.Length; i++)
        {
            Mem[i] = new Value();
        }
    }

    // Метод для расширения памяти при необходимости
    public static void EnsureMemorySize(int requiredSize)
    {
        if (requiredSize >= Mem.Length)
        {
            int newSize = Math.Max(requiredSize + 100, Mem.Length * 2);
            Array.Resize(ref Mem, newSize);
            
            // Инициализируем новую память
            for (int i = Mem.Length - (newSize - Mem.Length); i < Mem.Length; i++)
            {
                Mem[i] = new Value();
            }
        }
    }

    public static void MemoryDump(int count = 10) 
    {
        CompilerForm.Instance.ChangeOutputBoxText("Memory Dump:\n");
        for (int i = 0; i < Math.Min(count, Mem.Length); i++)
        {
            if (Mem[i].i != 0 || Mem[i].r != 0.0 || Mem[i].b)
            {
                CompilerForm.Instance.ChangeOutputBoxText($"Mem[{i}] = i:{Mem[i].i}, r:{Mem[i].r}, b:{Mem[i].b} \n");
            }
        }
    }

    public static void LoadProgram(List<ThreeAddr> program)
    {
        // Сначала проходим по программе чтобы найти все метки
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
        _callStack.Clear();
        _paramStack.Clear();
        _labelAddresses.Clear();
        _program = null;
    }
    private static void ExecuteCommand(ThreeAddr cmd)
    {
        // Проверяем и расширяем память при необходимости
        int maxIndex = Math.Max(cmd.MemIndex, Math.Max(cmd.Op1Index, cmd.Op2Index));
        if (maxIndex >= 0)
        {
            EnsureMemorySize(maxIndex + 1);
        }
        double TOLERANCE=0.00000001;
       // CompilerForm.Instance.ChangeOutputBoxText(cmd.command.ToString());
        //MemoryDump(1000);
        switch (cmd.command)
        {
            case Commands.icass when cmd.IValue != 0: // Integer constant assignment
                Mem[cmd.MemIndex].i = cmd.IValue;
                break;
                
            case Commands.icass: // Integer copy assignment
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i;
                break;
                
            case Commands.rcass when cmd.RValue != 0.0: // Real constant assignment
                Mem[cmd.MemIndex].r = cmd.RValue;
                break;
                
            case Commands.rcass: // Real copy assignment
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r;
                break;
                
            case Commands.bcass: // Boolean copy assignment
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].b;
                break;
                
            case Commands.iassadd: // Integer assignment with addition
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i + Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rassadd: // Real assignment with addition
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r + Mem[cmd.Op2Index].r;
                break;
                
            case Commands.iadd: // Integer addition
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i + Mem[cmd.Op2Index].i;
                break;
                
            case Commands.radd: // Real addition
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r + Mem[cmd.Op2Index].r;
                break;
                
            case Commands.isub: // Integer subtraction
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i - Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rsub: // Real subtraction
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r - Mem[cmd.Op2Index].r;
                break;
                
            case Commands.imul: // Integer multiplication
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i * Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rmul: // Real multiplication
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r * Mem[cmd.Op2Index].r;
                break;
                
            case Commands.idiv: // Integer division
                if (Mem[cmd.Op2Index].i == 0)
                    throw new DivideByZeroException("Integer division by zero");
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i / Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rdiv: // Real division
                if (Mem[cmd.Op2Index].r == 0.0)
                    throw new DivideByZeroException("Real division by zero");
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r / Mem[cmd.Op2Index].r;
                break;
                
            case Commands.ilt: // Integer less than
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i < Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rlt: // Real less than
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].r < Mem[cmd.Op2Index].r;
                break;
                
            case Commands.igt: // Integer greater than
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i > Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rgt: // Real greater than
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].r > Mem[cmd.Op2Index].r;
                break;
                
            case Commands.ieq: // Integer equality
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i == Mem[cmd.Op2Index].i;
                break;
                
            case Commands.req: // Real equality
                Mem[cmd.MemIndex].b = Math.Abs(Mem[cmd.Op1Index].r - Mem[cmd.Op2Index].r) < TOLERANCE;
                break;
                
            case Commands.beq: // Boolean equality
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].b == Mem[cmd.Op2Index].b;
                break;
            
            case Commands.ineq: // Integer equality
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i != Mem[cmd.Op2Index].i;
                break;
                
            case Commands.rneq: // Real equality
                Mem[cmd.MemIndex].b = Math.Abs(Mem[cmd.Op1Index].r - Mem[cmd.Op2Index].r) >= TOLERANCE;
                break;
                
            case Commands.bneq: // Boolean equality
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].b != Mem[cmd.Op2Index].b;
                break;
            case Commands.ic2ge: // Integer compare to greater or equal
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i >= cmd.IValue;
                break;
            
            case Commands.rc2ge: // Real compare to greater or equal
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].r >= cmd.RValue;
                break;
            
            case Commands.rc2le:
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i <= cmd.IValue;
                break;
            
            case Commands.ic2le: // Integer compare to less or equal
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].i <= cmd.IValue;
                break;
            
            
            
            case Commands.iif: // Conditional jump
                if (Mem[cmd.MemIndex].b)
                {
                    if (_labelAddresses.TryGetValue(cmd.Label, out int address))
                    {
                        _programCounter = address - 1; // -1 потому что после выполнения команды будет инкремент
                    }
                    else
                    {
                        throw new Exception($"Label '{cmd.Label}' not found");
                    }
                }
                break;
            case Commands.ifn: // !Conditional jump
                if (!Mem[cmd.MemIndex].b)
                {
                    if (_labelAddresses.TryGetValue(cmd.Label, out int address))
                    {
                        _programCounter = address - 1; // -1 потому что после выполнения команды будет инкремент
                    }
                    else
                    {
                        throw new Exception($"Label '{cmd.Label}' not found");
                    }
                }
                break;
            case Commands.iass: // Integer assignment
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i;
                break;
    
            case Commands.rass: // Real assignment
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r;
                break;
    
            case Commands.bass: // Boolean assignment
                Mem[cmd.MemIndex].b = Mem[cmd.Op1Index].b;
                break;
    
            case Commands.iasssub: // Integer assignment with subtraction
                Mem[cmd.MemIndex].i = Mem[cmd.Op1Index].i - Mem[cmd.Op2Index].i;
                break;
    
            case Commands.rasssub: // Real assignment with subtraction
                Mem[cmd.MemIndex].r = Mem[cmd.Op1Index].r - Mem[cmd.Op2Index].r;
                break;
    
            case Commands.citr: // Convert Integer to Real
                Mem[cmd.Op1Index].r = Mem[cmd.MemIndex].i;
                break;
    
            case Commands.call: // Function/procedure call
                if (_standardFunctions.ContainsKey(cmd.Label))
                {
                    // Вызов стандартной функции
                    _standardFunctions[cmd.Label]();
                    
                    // Если это вызов функции (не процедуры), сохраняем результат
                    if (cmd.MemIndex != 0)
                    {
                        // Для стандартных функций можно вернуть какое-то значение
                        // Например, для print возвращаем 0
                        Mem[cmd.MemIndex].i = 0;
                    }
                }
                else if (_labelAddresses.TryGetValue(cmd.Label, out int callAddress))
                {
                    // Обычный вызов пользовательской функции
                    _callStack.Push(_programCounter);
                    _programCounter = callAddress - 1;
                }
                else
                {
                    throw new Exception($"Function or procedure '{cmd.Label}' not found");
                }
                break;
    
          
            case Commands.push: // Push parameter onto stack
                _paramStack.Push(Mem[cmd.MemIndex]);
                break;
                
            case Commands.pop: // Pop from stack
                if (_paramStack.Count > 0)
                    _paramStack.Pop();
                break;
            
            case Commands.go: // Unconditional jump
                if (_labelAddresses.TryGetValue(cmd.Label, out int jumpAddress))
                {
                    _programCounter = jumpAddress - 1; // -1 потому что после выполнения команды будет инкремент
                }
                else
                {
                    throw new Exception($"Label '{cmd.Label}' not found");
                }
                break;
                
            case Commands.label:
                break;
                
            case Commands.stop: // Stop execution
                break;
          
            default:
                throw new NotImplementedException($"Command {cmd.command} not implemented");
        }
    }

    private static void ExecutePrintFunction()
    {
        if (_paramStack.Count > 0)
        {
            var value = _paramStack.Peek(); // Смотрим значение, но не убираем со стека
            
            // Определяем тип и выводим соответствующее значение
            // Можно добавить логику определения типа на основе содержимого
            if (Math.Abs(value.r) > 0.000001)
            {
                CompilerForm.Instance.ChangeOutputBoxText(value.r.ToString("F6"));
            }
            else
            {
                CompilerForm.Instance.ChangeOutputBoxText(value.i.ToString());
            }
            
        }
    }
    public static void RunProgram(List<ThreeAddr> program)
    {
        InitializeMemory();
        LoadProgram(program);
        Run();
    }

    // Вспомогательные методы для тестирования
    public static void CompileAndRunExample()
    {
        
    }
}

// Перечисление для типов переменных
public enum VarType
{
    Integer,
    Real,
    Boolean
}