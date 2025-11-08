
using MyInterpreter;

namespace SmallMachine;
public class TestVisitor
{
    
}public class CodeGenerator : IVisitor<List<ThreeAddr>>
{
    private int _tempCounter = 0;
    private int _labelCounter = 0;
    private Dictionary<string, int> _variableAddresses = new Dictionary<string, int>();
    private int _nextAddress = 0;
    
    // Для отслеживания текущего типа выражения (если нужно)
    private VarType _currentType = VarType.Integer;
    
    public CodeGenerator()
    {
        _nextAddress = 0;
    }
    
    private int GetVariableAddress(string name)
    {
        if (!_variableAddresses.ContainsKey(name))
        {
            _variableAddresses[name] = _nextAddress++;
        }
        return _variableAddresses[name];
    }
    
    private int NewTemp()
    {
        int tempAddr = _nextAddress + _tempCounter;
        _tempCounter++;
        return tempAddr;
    }
    
    private string NewLabel()
    {
        return $"L{_labelCounter++}";
    }
    
    private void ResetTempCounter()
    {
        _tempCounter = 0;
    }
    
    // Вспомогательный метод для получения адреса результата из кода
    private int GetLastResultAddress(List<ThreeAddr> code)
    {
        if (code.Count == 0)
            throw new InvalidOperationException("No commands in code list");
            
        for (int i = code.Count - 1; i >= 0; i--)
        {
            if (code[i].ResultIndex >= 0)
                return code[i].ResultIndex;
        }
        
        throw new InvalidOperationException("No result address found in code");
    }
    
    // Вспомогательные методы для создания значений
    private Value IntValue(int value) => new Value { i = value, type = VarType.Integer };
    private Value RealValue(double value) => new Value { r = value, type = VarType.Real };
    private Value BoolValue(bool value) => new Value { b = value, type = VarType.Boolean };

    public List<ThreeAddr> VisitNode(Node node)
    {
        // Базовый случай - если узел не обработан специальным методом
        if (node is ExprNode exprNode)
            return VisitExprNode(exprNode);
        else if (node is StatementNode stmtNode)
            return VisitStatementNode(stmtNode);
        else
            throw new NotImplementedException($"VisitNode for {node.GetType()} not implemented");
    }

    public List<ThreeAddr> VisitExprNode(ExprNode expr)
    {
        return expr.Visit(this);
    }

    public List<ThreeAddr> VisitStatementNode(StatementNode stmt)
    {
        return stmt.Visit(this);
    }

    public List<ThreeAddr> VisitBinOp(BinOpNode bin)
    {
        var code = new List<ThreeAddr>();
        
        // Генерируем код для левого операнда
        var leftCode = bin.Left.Visit(this);
        code.AddRange(leftCode);
        int leftAddress = GetLastResultAddress(leftCode);
        
        // Генерируем код для правого операнда
        var rightCode = bin.Right.Visit(this);
        code.AddRange(rightCode);
        int rightAddress = GetLastResultAddress(rightCode);
        
        // Создаем временную переменную для результата
        int resultAddress = NewTemp();
        
        // Определяем команду на основе оператора
        Commands operation = bin.Op switch
        {
            TokenType.Plus => Commands.add,
            TokenType.Minus => Commands.sub,
            TokenType.Multiply => Commands.mul,
            TokenType.Divide => Commands.div,
            TokenType.Less => Commands.lt,
            TokenType.Greater => Commands.gt,
            TokenType.Equal => Commands.eq,
            TokenType.NotEqual => Commands.neq,
            _ => throw new ArgumentException($"Unsupported operator: {bin.OpToStr()}")
        };
        
        code.Add(new ThreeAddr(operation, resultAddress, leftAddress, rightAddress));
        return code;
    }

    public List<ThreeAddr> VisitStatementList(StatementListNode stl)
    {
        var code = new List<ThreeAddr>();
        foreach (var statement in stl.lst)
        {
            code.AddRange(statement.Visit(this));
        }
        return code;
    }

    public List<ThreeAddr> VisitExprList(ExprListNode exlist)
    {
        var code = new List<ThreeAddr>();
        foreach (var expr in exlist.lst)
        {
            code.AddRange(expr.Visit(this));
        }
        return code;
    }

    public List<ThreeAddr> VisitInt(IntNode n)
    {
        var code = new List<ThreeAddr>();
        int tempAddress = NewTemp();
        code.Add(new ThreeAddr(Commands.ass, tempAddress, IntValue(n.Val)));
        return code;
    }

    public List<ThreeAddr> VisitDouble(DoubleNode d)
    {
        var code = new List<ThreeAddr>();
        int tempAddress = NewTemp();
        code.Add(new ThreeAddr(Commands.ass, tempAddress, RealValue(d.Val)));
        return code;
    }

    public List<ThreeAddr> VisitId(IdNode id)
    {
        var code = new List<ThreeAddr>();
        int varAddress = GetVariableAddress(id.Name);
        int tempAddress = NewTemp();
        
        // Копируем значение переменной во временную переменную
        code.Add(new ThreeAddr(Commands.ass, tempAddress, varAddress, 0));
        
        return code;
    }

    public List<ThreeAddr> VisitAssign(AssignNode ass)
    {
        var code = new List<ThreeAddr>();
        var exprCode = ass.Expr.Visit(this);
        code.AddRange(exprCode);
        
        int resultAddress = GetLastResultAddress(exprCode);
        int varAddress = GetVariableAddress(ass.Ident.Name);
        code.Add(new ThreeAddr(Commands.ass, varAddress, resultAddress, 0));
        
        return code;
    }

    public List<ThreeAddr> VisitAssignOp(AssignOpNode ass)
    {
        var code = new List<ThreeAddr>();
        
        // Получаем текущее значение переменной
        int varAddress = GetVariableAddress(ass.Ident.Name);
        int tempVar = NewTemp();
        code.Add(new ThreeAddr(Commands.ass, tempVar, varAddress, 0));
        
        // Генерируем код для правого выражения
        var exprCode = ass.Expr.Visit(this);
        code.AddRange(exprCode);
        int exprAddress = GetLastResultAddress(exprCode);
        
        // Выполняем операцию
        int resultAddress = NewTemp();
        Commands operation = ass.Op switch
        {
            '+' => Commands.add,
            '-' => Commands.sub,
            '*' => Commands.mul,
            '/' => Commands.div,
            _ => throw new ArgumentException($"Unsupported assignment operator: {ass.Op}")
        };
        
        code.Add(new ThreeAddr(operation, resultAddress, tempVar, exprAddress));
        
        // Присваиваем результат обратно в переменную
        code.Add(new ThreeAddr(Commands.ass, varAddress, resultAddress, 0));
        
        return code;
    }

    public List<ThreeAddr> VisitIf(IfNode ifn)
    {
        var code = new List<ThreeAddr>();
        
        // Генерируем код для условия
        var conditionCode = ifn.Condition.Visit(this);
        code.AddRange(conditionCode);
        int conditionAddress = GetLastResultAddress(conditionCode);
        
        string elseLabel = NewLabel();
        string endLabel = NewLabel();
        
        // Условный переход: если условие ложно, переходим на else
        code.Add(new ThreeAddr(Commands.iif, conditionAddress, elseLabel));
        
        // then branch
        var thenCode = ifn.ThenStat.Visit(this);
        code.AddRange(thenCode);
        code.Add(new ThreeAddr(Commands.go, endLabel));
        
        // else branch
        code.Add(new ThreeAddr(Commands.label, elseLabel));
        if (ifn.ElseStat != null)
        {
            var elseCode = ifn.ElseStat.Visit(this);
            code.AddRange(elseCode);
        }
        
        code.Add(new ThreeAddr(Commands.label, endLabel));
        return code;
    }

    public List<ThreeAddr> VisitWhile(WhileNode whn)
    {
        var code = new List<ThreeAddr>();
        
        string startLabel = NewLabel();
        string conditionLabel = NewLabel();
        string endLabel = NewLabel();
        
        // Переходим к проверке условия
        code.Add(new ThreeAddr(Commands.go, conditionLabel));
        
        // Начало цикла
        code.Add(new ThreeAddr(Commands.label, startLabel));
        
        // Тело цикла
        var bodyCode = whn.Stat.Visit(this);
        code.AddRange(bodyCode);
        
        // Проверка условия
        code.Add(new ThreeAddr(Commands.label, conditionLabel));
        var conditionCode = whn.Condition.Visit(this);
        code.AddRange(conditionCode);
        int conditionAddress = GetLastResultAddress(conditionCode);
        
        // Если условие истинно, переходим в начало цикла
        code.Add(new ThreeAddr(Commands.iif, conditionAddress, startLabel));
        
        // Конец цикла
        code.Add(new ThreeAddr(Commands.label, endLabel));
        
        return code;
    }

    public List<ThreeAddr> VisitFor(ForNode forNode)
    {
        var code = new List<ThreeAddr>();
        
        // Инициализация
        if (forNode.Counter != null)
        {
            code.AddRange(forNode.Counter.Visit(this));
        }
        
        string startLabel = NewLabel();
        string conditionLabel = NewLabel();
        string endLabel = NewLabel();
        
        // Переходим к проверке условия
        code.Add(new ThreeAddr(Commands.go, conditionLabel));
        
        // Начало цикла
        code.Add(new ThreeAddr(Commands.label, startLabel));
        
        // Тело цикла
        code.AddRange(forNode.Stat.Visit(this));
        
        // Инкремент
        if (forNode.Increment != null)
        {
            code.AddRange(forNode.Increment.Visit(this));
        }
        
        // Проверка условия
        code.Add(new ThreeAddr(Commands.label, conditionLabel));
        var conditionCode = forNode.Condition.Visit(this);
        code.AddRange(conditionCode);
        int conditionAddress = GetLastResultAddress(conditionCode);
        
        // Если условие истинно, переходим в начало цикла
        code.Add(new ThreeAddr(Commands.iif, conditionAddress, startLabel));
        
        // Конец цикла
        code.Add(new ThreeAddr(Commands.label, endLabel));
        
        return code;
    }

    public List<ThreeAddr> VisitProcCall(ProcCallNode p)
    {
        var code = new List<ThreeAddr>();
        
        // Генерируем код для аргументов
        if (p.Pars != null)
        {
            code.AddRange(p.Pars.Visit(this));
        }
        
        // Для простоты, вызов процедуры генерирует специальную команду call
        // В реальной реализации здесь была бы более сложная логика передачи параметров
        code.Add(new ThreeAddr(Commands.call, p.Name.Name));
        
        return code;
    }

    public List<ThreeAddr> VisitFuncCall(FuncCallNode f)
    {
        var code = new List<ThreeAddr>();
        
        // Генерируем код для аргументов
        if (f.Pars != null)
        {
            code.AddRange(f.Pars.Visit(this));
        }
        
        // Для функции создаем временную переменную для результата
        int resultAddress = NewTemp();
        
        // В реальной реализации здесь был бы вызов функции с сохранением результата
        // Для примера просто присваиваем 0
        code.Add(new ThreeAddr(Commands.ass, resultAddress, IntValue(0)));
        
        return code;
    }

    public List<ThreeAddr> VisitFunDef(FuncDefNode f)
    {
        throw new NotImplementedException();
    }

    public List<ThreeAddr> VisitReturn(ReturnNode r)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, int> GetVariableMap()
    {
        return new Dictionary<string, int>(_variableAddresses);
    }
}

