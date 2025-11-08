using Microsoft.VisualBasic.Logging;

namespace MyInterpreter;

public enum Commands 
{
    // Прямая адресация (все операнды прямые)
    iass, rass, bass,
    icass, rcass, bcass,
    iadd, radd, isub, rsub, imul, rmul, idiv, rdiv,
    ilt, rlt, igt, rgt, ieq, req, beq, ineq, rneq, bneq,
    ic2ge, rc2ge, ic2le, rc2le,
    iassadd, rassadd, iasssub, rasssub,
    citr, iif, ifn, go, call, push, pop, param, creturn, label, stop,
    
    // Косвенная адресация для отдельных операндов
    // d - результат (MemIndex)
    iadd_d, radd_d, isub_d, rsub_d, imul_d, rmul_d, idiv_d, rdiv_d,
    ilt_d, rlt_d, igt_d, rgt_d, ieq_d, req_d, beq_d, ineq_d, rneq_d, bneq_d,
    iass_d, rass_d, bass_d,
    icass_d, rcass_d, bcass_d,
    iassadd_d, rassadd_d, iasssub_d, rasssub_d,
    iif_d, ifn_d, push_d,
    
    // l - левый операнд (Op1Index)
    iadd_l, radd_l, isub_l, rsub_l, imul_l, rmul_l, idiv_l, rdiv_l,
    ilt_l, rlt_l, igt_l, rgt_l, ieq_l, req_l, beq_l, ineq_l, rneq_l, bneq_l,
    iass_l, rass_l, bass_l,
    iassadd_l, rassadd_l, iasssub_l, rasssub_l,
    citr_l,
    
    // r - правый операнд (Op2Index)
    iadd_r, radd_r, isub_r, rsub_r, imul_r, rmul_r, idiv_r, rdiv_r,
    ilt_r, rlt_r, igt_r, rgt_r, ieq_r, req_r, beq_r, ineq_r, rneq_r, bneq_r,
    iass_r, rass_r, bass_r,
    iassadd_r, rassadd_r, iasssub_r, rasssub_r,
    
    // Комбинации двух операндов
    // ld - левый и результат
    iadd_ld, radd_ld, isub_ld, rsub_ld, imul_ld, rmul_ld, idiv_ld, rdiv_ld,
    ilt_ld, rlt_ld, igt_ld, rgt_ld, ieq_ld, req_ld, beq_ld, ineq_ld, rneq_ld, bneq_ld,
    iass_ld, rass_ld, bass_ld,
    iassadd_ld, rassadd_ld, iasssub_ld, rasssub_ld,
    citr_ld,
    
    // rd - правый и результат
    iadd_rd, radd_rd, isub_rd, rsub_rd, imul_rd, rmul_rd, idiv_rd, rdiv_rd,
    ilt_rd, rlt_rd, igt_rd, rgt_rd, ieq_rd, req_rd, beq_rd, ineq_rd, rneq_rd, bneq_rd,
    iass_rd, rass_rd, bass_rd,
    iassadd_rd, rassadd_rd, iasssub_rd, rasssub_rd,
    
    // lr - левый и правый
    iadd_lr, radd_lr, isub_lr, rsub_lr, imul_lr, rmul_lr, idiv_lr, rdiv_lr,
    ilt_lr, rlt_lr, igt_lr, rgt_lr, ieq_lr, req_lr, beq_lr, ineq_lr, rneq_lr, bneq_lr,
    iass_lr, rass_lr, bass_lr,
    iassadd_lr, rassadd_lr, iasssub_lr, rasssub_lr,
    
    // Все три операнда косвенные (lrd)
    iadd_lrd, radd_lrd, isub_lrd, rsub_lrd, imul_lrd, rmul_lrd, idiv_lrd, rdiv_lrd,
    ilt_lrd, rlt_lrd, igt_lrd, rgt_lrd, ieq_lrd, req_lrd, beq_lrd, ineq_lrd, rneq_lrd, bneq_lrd,
    iass_lrd, rass_lrd, bass_lrd,
    iassadd_lrd, rassadd_lrd, iasssub_lrd, rasssub_lrd,
    citr_lrd
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
    public bool IsInDirectAdressing1 = false;
    public bool IsInDirectAdressing2 = false;
    public bool IsInDirectAdressing3 = false;
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
    public static ThreeAddr CreateConst(Commands comm, int memIndex, int ivalue,bool isInDirectAdressing = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, IValue = ivalue, IsInDirectAdressing1 = isInDirectAdressing };
    }
    
    public static ThreeAddr CreateConst(Commands comm, int memIndex, double rvalue,bool isInDirectAdressing = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, RValue = rvalue, IsInDirectAdressing1 = isInDirectAdressing };
    }
    
    public static ThreeAddr CreateConst(Commands comm, int memIndex, bool bvalue,bool isInDirectAdressing = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, BValue = bvalue, IsInDirectAdressing1 = isInDirectAdressing };
    }
    
    // Для команд с меткой (label, go)
    public static ThreeAddr Create(Commands comm, string label)
    {
        return new ThreeAddr { command = comm, Label = label };
    }
    
    // Для условных переходов (iif с индексом и меткой)
    public static ThreeAddr Create(Commands comm, int memIndex, string label,bool isInDirectAdressing = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memIndex, Label = label, IsInDirectAdressing1 = isInDirectAdressing };
    }
    
    // push pop
    public static ThreeAddr Create(Commands comm, int memindex,bool isInDirectAdressing = false)
    {
        return new ThreeAddr { command = comm, MemIndex = memindex, IsInDirectAdressing1 = isInDirectAdressing };
    }
    
    // Для присваиваний между переменными (iass, rass, bass)
    public static ThreeAddr CreateAssign(Commands comm, int destIndex, int srcIndex,bool isInDirectAdressing1 = false, bool isInDirectAdressing2 = false)
    {
        return new ThreeAddr { command = comm, MemIndex = destIndex, Op1Index = srcIndex, IsInDirectAdressing1 = isInDirectAdressing1, IsInDirectAdressing2 = isInDirectAdressing2 };
    }
    
    // Для конвертации типов
    public static ThreeAddr CreateConvert(Commands comm, int srcIndex, int destIndex,bool isInDirectAdressing1 = false, bool isInDirectAdressing2 = false)
    {
        return new ThreeAddr { command = comm, MemIndex = srcIndex, Op1Index = destIndex, IsInDirectAdressing1 = isInDirectAdressing1, IsInDirectAdressing2 = isInDirectAdressing2};
    }
    
    // Для бинарных операций (iadd, isub, etc)
    public static ThreeAddr CreateBinary(Commands comm, int op1Index, int op2Index, int resIndex,bool isInDirectAdressing1 = false, bool isInDirectAdressing2 = false, bool isInDirectAdressing3 = false)
    {
        return new ThreeAddr { command = comm, Op1Index = op1Index, Op2Index = op2Index, MemIndex = resIndex, IsInDirectAdressing1 = isInDirectAdressing1, IsInDirectAdressing2 = isInDirectAdressing2, IsInDirectAdressing3 = isInDirectAdressing3};
    }
    
    // Для операций присваивания с операцией (iassadd, iasssub, etc)
    public static ThreeAddr CreateAssignOp(Commands comm, int destIndex, int srcIndex1, int srcIndex2, bool isInDirectAdressing1 = false, bool isInDirectAdressing2 = false, bool isInDirectAdressing3 = false)
    {
        return new ThreeAddr { command = comm, MemIndex = destIndex, Op1Index = srcIndex1, Op2Index = srcIndex2, IsInDirectAdressing1 = isInDirectAdressing1, IsInDirectAdressing2 = isInDirectAdressing2, IsInDirectAdressing3 = isInDirectAdressing3};
    }
    
    private ThreeAddr() { }
}

public class VirtualMachine 
{
    public static Value[] Mem = new Value[1000];
    private static Stack<int> _callStack = new Stack<int>(); // для адресов возврата
    public static int _current_frame_index = 0;
    private static Stack<int> _frame_sizes_stack = new Stack<int>();
    private static Stack<Value> _param_stack = new Stack<Value>();
    private static Dictionary<string, int> _memory_func_size = new Dictionary<string, int>();
    
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

    // Вспомогательные методы для косвенной адресации
    private static int GetAddress(int index, bool isIndirect)
    {
        if (isIndirect)
        {
            return _current_frame_index + index;
        }
        return index;
    }

    private static ref Value GetValue(int index, bool isIndirect)
    {
        int actualIndex = GetAddress(index, isIndirect);
        EnsureMemorySize(actualIndex + 1);
        return ref Mem[actualIndex];
    }

    private static void SetIntValue(int index, bool isIndirect, int value)
    {
        int actualIndex = GetAddress(index, isIndirect);
        EnsureMemorySize(actualIndex + 1);
        Mem[actualIndex].i = value;
    }

    private static void SetRealValue(int index, bool isIndirect, double value)
    {
        int actualIndex = GetAddress(index, isIndirect);
        EnsureMemorySize(actualIndex + 1);
        Mem[actualIndex].r = value;
    }

    private static void SetBoolValue(int index, bool isIndirect, bool value)
    {
        int actualIndex = GetAddress(index, isIndirect);
        EnsureMemorySize(actualIndex + 1);
        Mem[actualIndex].b = value;
    }

    public static void MemoryDump(int count = 10) 
    {
        CompilerForm.Instance.ChangeOutputBoxText("Memory Dump:\n");
        CompilerForm.Instance.ChangeOutputBoxText($"Current Frame Index: {_current_frame_index}\n");
        
        for (int i = 0; i < Math.Min(count, Mem.Length); i++)
        {
            if (Mem[i].i != 0 || Mem[i].r != 0.0 || Mem[i].b)
            {
                string frameInfo = (i >= _current_frame_index && i < _current_frame_index + 100) ? 
                    " [FRAME]" : "";
                CompilerForm.Instance.ChangeOutputBoxText($"Mem[{i}] = i:{Mem[i].i}, r:{Mem[i].r}, b:{Mem[i].b}{frameInfo}\n");
            }
        }
    }

    public static void LoadProgram(List<ThreeAddr> program)
    {
        // Сначала проходим по программе, чтобы найти все метки
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
        _labelAddresses.Clear();
        _program = null;
        _current_frame_index = 0;
        _frame_sizes_stack.Clear();
        _memory_func_size.Clear();
    }

 private static void ExecuteCommand(ThreeAddr cmd)
{
    double tolerance = 0.00000001;
    
    switch (cmd.command)
    {
        case Commands.icass: // Integer constant assignment
            SetIntValue(cmd.MemIndex, cmd.IsInDirectAdressing1, cmd.IValue);
            break;
            
        case Commands.rcass: // Real constant assignment
            SetRealValue(cmd.MemIndex, cmd.IsInDirectAdressing1, cmd.RValue);
            break;
            
        case Commands.bcass: // Boolean constant assignment
            SetBoolValue(cmd.MemIndex, cmd.IsInDirectAdressing1, cmd.BValue);
            break;
            
        case Commands.iass: // Integer assignment
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rass: // Real assignment
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.bass: // Boolean assignment
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).b;
            break;
            
        case Commands.iadd: // Integer addition
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i + 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.radd: // Real addition
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r + 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.isub: // Integer subtraction
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i - 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rsub: // Real subtraction
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r - 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.imul: // Integer multiplication
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i * 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rmul: // Real multiplication
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r * 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.idiv: // Integer division
            if (GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i == 0)
                throw new DivideByZeroException("Integer division by zero");
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i / 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rdiv: // Real division
            if (Math.Abs(GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r) < tolerance)
                throw new DivideByZeroException("Real division by zero");
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r / 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.ilt: // Integer less than
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i < 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rlt: // Real less than
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r < 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.igt: // Integer greater than
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i > 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rgt: // Real greater than
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r > 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r;
            break;
            
        case Commands.ieq: // Integer equality
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i == 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.req: // Real equality
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                Math.Abs(GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r - 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r) < tolerance;
            break;
            
        case Commands.beq: // Boolean equality
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).b == 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).b;
            break;
            
        case Commands.ineq: // Integer non equality
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).i != 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).i;
            break;
            
        case Commands.rneq: // Real non equality
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                Math.Abs(GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).r - 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).r) >= tolerance;
            break;
            
        case Commands.bneq: // Boolean non equality
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing3).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing1).b != 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing2).b;
            break;
            
        case Commands.ic2ge: // Integer compare to greater or equal
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).i >= cmd.IValue;
            break;
        
        case Commands.rc2ge: // Real compare to greater or equal
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).r >= cmd.RValue;
            break;
        
        case Commands.ic2le: // Integer compare to less or equal
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).i <= cmd.IValue;
            break;
        
        case Commands.rc2le: // Real compare to less or equal
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).r <= cmd.RValue;
            break;
            
        case Commands.iassadd: // Integer assignment with addition
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).i + 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing3).i;
            break;
            
        case Commands.rassadd: // Real assignment with addition
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).r + 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing3).r;
            break;
            
        case Commands.iasssub: // Integer assignment with subtraction
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).i = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).i - 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing3).i;
            break;
            
        case Commands.rasssub: // Real assignment with subtraction
            GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).r = 
                GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).r - 
                GetValue(cmd.Op2Index, cmd.IsInDirectAdressing3).r;
            break;
            
        case Commands.citr: // Convert integer to real
            GetValue(cmd.Op1Index, cmd.IsInDirectAdressing2).r = 
                GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).i;
            break;
            
        case Commands.iif: // Conditional jump
            if (GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b)
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
            
        case Commands.ifn: // Conditional jump if false
            if (!GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1).b)
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
            
        case Commands.go: // Unconditional jump
            if (_labelAddresses.TryGetValue(cmd.Label, out int jumpAddress))
            {
                _programCounter = jumpAddress - 1;
            }
            else
            {
                throw new Exception($"Label '{cmd.Label}' not found");
            }
            break;
            
        case Commands.call: // Function/procedure call
            if (_standardFunctions.ContainsKey(cmd.Label))
            {
                // Вызов стандартной функции
                _standardFunctions[cmd.Label]();
                
                // Если это вызов функции (не процедуры), сохраняем результат
                if (cmd.MemIndex != 0)
                {
                    SetIntValue(cmd.MemIndex, cmd.IsInDirectAdressing1, 0);
                }
            }
            else if (_labelAddresses.TryGetValue(cmd.Label, out int callAddress))
            {
                // Обычный вызов пользовательской функции
                _callStack.Push(_programCounter);
                _programCounter = callAddress - 1;
                
                _current_frame_index += _memory_func_size[cmd.Label];
                _frame_sizes_stack.Push(_memory_func_size[cmd.Label]);
            }
            else
            {
                throw new Exception($"Function or procedure '{cmd.Label}' not found");
            }
            break;
            
        case Commands.creturn: // Return from function
            if (_callStack.Count > 0)
            {
                _programCounter = _callStack.Pop();
                _current_frame_index -= _frame_sizes_stack.Pop();
            }
            else
            {
                throw new Exception("Call stack underflow in creturn");
            }
            break;
            
        case Commands.push:
            // Реализация push с учетом косвенной адресации
            _param_stack.Push(GetValue(cmd.MemIndex, cmd.IsInDirectAdressing1));
            break;

        case Commands.pop:
            // Реализация pop с учетом косвенной адресации
            if (_param_stack.Count > 0)
            {
                var value = _param_stack.Pop();
                if (cmd.IsInDirectAdressing1)
                {
                    Mem[_current_frame_index + cmd.MemIndex] = value;
                }
                else
                {
                    Mem[cmd.MemIndex] = value;
                }
            }
            break;

        case Commands.param:
            // Реализация передачи параметров
            break;
            
        case Commands.label:
            // Метка - ничего не делаем
            break;
            
        case Commands.stop:
            // Остановка выполнения
            break;
            
        default:
            throw new NotImplementedException($"Command {cmd.command} not implemented");
    }
}

    private static void ExecutePrintFunction()
    {
        // Базовая реализация функции print
        // В реальной реализации здесь должна быть логика вывода значений
        CompilerForm.Instance.ChangeOutputBoxText("[print function called]\n");
    }

    public static void RunProgram(List<ThreeAddr> program)
    {
        InitializeMemory();
        LoadProgram(program);
        Run();
    }

    // Метод для регистрации размера памяти функции
    public static void RegisterFunctionMemorySize(string functionName, int size)
    {
        _memory_func_size[functionName] = size;
    }

    // Вспомогательные методы для тестирования
    public static void CompileAndRunExample()
    {
        // Пример программы с косвенной адресацией
        var program = new List<ThreeAddr>
        {
            // Основная программа - глобальная область (прямая адресация)
            ThreeAddr.CreateConst(Commands.icass, 0, 10), // Mem[0] = 10 (глобальная переменная)
            
            // Вызов функции с передачей параметра через косвенную адресацию
            ThreeAddr.Create(Commands.call, "myFunction"),
            
            ThreeAddr.Create(Commands.stop)
        };

        // Функция - использует косвенную адресацию
        var functionCode = new List<ThreeAddr>
        {
            ThreeAddr.Create(Commands.label, "myFunction"),
            
            // Локальная переменная в кадре (косвенная адресация)
            ThreeAddr.CreateConst(Commands.icass, 0, 5, true), // Frame[0] = 5
            
            // Работа с глобальной переменной (прямая адресация)
            ThreeAddr.CreateBinary(Commands.iadd, 0, 0, 1, false), // Mem[1] = Mem[0] + Frame[0]
            
            ThreeAddr.Create(Commands.creturn)
        };

        // Регистрируем размер памяти для функции
        RegisterFunctionMemorySize("myFunction", 10);

        // Объединяем программу
        program.AddRange(functionCode);
        
        // Запускаем
        RunProgram(program);
    }
}

// Перечисление для типов переменных
public enum VarType
{
    Integer,
    Real,
    Boolean
}