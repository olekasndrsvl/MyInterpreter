namespace MyInterpreter.Common;

public class ThreeAddressCodeVisitor : IVisitorP
{
    private List<ThreeAddr> _code = new List<ThreeAddr>();
    private int _tempCounter = 100;
    private int _labelCounter = 0;
    private Dictionary<string, int> _labelAddresses = new Dictionary<string, int>();
    private Dictionary<string, int> _variableAddresses = new Dictionary<string, int>();
    private int _nextVarAddress = 0;
    
    // Таблица для бинарных операций: (тип_левого, тип_правого, токен) -> команда
    private static readonly Dictionary<(SemanticType, SemanticType, TokenType), Commands> _binOpTable = 
        new Dictionary<(SemanticType, SemanticType, TokenType), Commands>
    {
        // Целочисленные операции
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Plus), Commands.iadd},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Minus), Commands.isub},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Multiply), Commands.imul},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Divide), Commands.idiv},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Less), Commands.ilt},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Greater), Commands.igt},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.Equal), Commands.ieq},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.NotEqual), Commands.ineq},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.LessEqual), Commands.ic2le},
        {(SemanticType.IntType, SemanticType.IntType, TokenType.GreaterEqual), Commands.ic2ge},
        
        // Вещественные операции
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Plus), Commands.radd},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Minus), Commands.rsub},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Multiply), Commands.rmul},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Divide), Commands.rdiv},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Less), Commands.rlt},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Greater), Commands.rgt},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Equal), Commands.req},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.NotEqual), Commands.rneq},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.LessEqual), Commands.rc2le},
        {(SemanticType.DoubleType, SemanticType.DoubleType, TokenType.GreaterEqual), Commands.rc2ge},
        
        // Смешанные типы (int-double) - преобразуются к double
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Plus), Commands.radd},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Plus), Commands.radd},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Minus), Commands.rsub},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Minus), Commands.rsub},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Multiply), Commands.rmul},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Multiply), Commands.rmul},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Divide), Commands.rdiv},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Divide), Commands.rdiv},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Less), Commands.rlt},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Less), Commands.rlt},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Greater), Commands.rgt},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Greater), Commands.rgt},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.Equal), Commands.req},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.Equal), Commands.req},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.NotEqual), Commands.rneq},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.NotEqual), Commands.rneq},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.LessEqual), Commands.rc2le},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.LessEqual), Commands.rc2le},
        {(SemanticType.IntType, SemanticType.DoubleType, TokenType.GreaterEqual), Commands.rc2ge},
        {(SemanticType.DoubleType, SemanticType.IntType, TokenType.GreaterEqual), Commands.rc2ge},
    };

    // Таблица для операций присваивания с операцией: (тип_переменной, символ_операции) -> команда
    private static readonly Dictionary<(SemanticType, char), Commands> _assignOpTable =
        new Dictionary<(SemanticType, char), Commands>
    {
        {(SemanticType.IntType, '+'), Commands.iadd},
        {(SemanticType.IntType, '-'), Commands.isub},
        {(SemanticType.IntType, '*'), Commands.imul},
        {(SemanticType.IntType, '/'), Commands.idiv},
        
        {(SemanticType.DoubleType, '+'), Commands.radd},
        {(SemanticType.DoubleType, '-'), Commands.rsub},
        {(SemanticType.DoubleType, '*'), Commands.rmul},
        {(SemanticType.DoubleType, '/'), Commands.rdiv},
    };

    public List<ThreeAddr> Code => _code;
    public Dictionary<string, int> LabelAddresses => _labelAddresses;

    private int NewTemp() => _tempCounter++;
    private string NewLabel() => $"L{_labelCounter++}";
    
    private int GetVariableAddress(string varName)
    {
        if (!_variableAddresses.ContainsKey(varName))
        {
            _variableAddresses[varName] = _nextVarAddress++;
        }
        return _variableAddresses[varName];
    }

    public void VisitNode(Node node) { }
    public void VisitExprNode(ExprNode node) { }
    public void VisitStatementNode(StatementNode node) { }

    public void VisitBinOp(BinOpNode bin)
    {
        bin.Left.VisitP(this);
        int leftTemp = _tempCounter - 1;
        
        bin.Right.VisitP(this);
        int rightTemp = _tempCounter - 1;
        
        

        var leftType = TypeChecker.CalcType(bin.Left);
        var rightType = TypeChecker.CalcType(bin.Right);

        // Автоматическое преобразование типов при необходимости
        if (leftType == SemanticType.IntType && rightType == SemanticType.DoubleType)
        {
            // Convert left int to double
            int convertedTemp = NewTemp();
            _code.Add(ThreeAddr.CreateConvert(Commands.citr, leftTemp, convertedTemp));
            leftTemp = convertedTemp;
            leftType = SemanticType.DoubleType;
        }
        else if (leftType == SemanticType.DoubleType && rightType == SemanticType.IntType)
        {
            // Convert right int to double
            int convertedTemp = NewTemp();
            _code.Add(ThreeAddr.CreateConvert(Commands.citr, rightTemp, convertedTemp));
            rightTemp = convertedTemp;
            rightType = SemanticType.DoubleType;
        }
        
        int resultTemp = NewTemp();
        // Поиск команды в таблице
        if (_binOpTable.TryGetValue((leftType, rightType, bin.Op), out Commands command))
        {
            _code.Add(ThreeAddr.CreateBinary(command, leftTemp, rightTemp, resultTemp));
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported binary operation {bin.Op} for types {leftType} and {rightType}");
        }
    }

    public void VisitStatementList(StatementListNode stl)
    {
        foreach (var st in stl.lst)
        {
            st.VisitP(this);
        }
    }

    public void VisitExprList(ExprListNode exlist)
    {
        foreach (var ex in exlist.lst)
        {
            ex.VisitP(this);
        }
    }

    public void VisitInt(IntNode n)
    {
        int tempIndex = NewTemp();
        _code.Add(ThreeAddr.CreateConst(Commands.icass, tempIndex, n.Val));
    }

    public void VisitDouble(DoubleNode d)
    {
        int tempIndex = NewTemp();
        _code.Add(ThreeAddr.CreateConst(Commands.rcass, tempIndex, d.Val));
    }

    public void VisitId(IdNode id)
    {
        int tempIndex = NewTemp();
        int varAddress = GetVariableAddress(id.Name);
        var varType = TypeChecker.CalcType(id);
        
        Commands command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        _code.Add(ThreeAddr.CreateAssign(command, tempIndex, varAddress));
    }

    public void VisitAssign(AssignNode ass)
    {
        int varAddress = GetVariableAddress(ass.Ident.Name);
        var varType = TypeChecker.CalcType(ass.Ident);
    
        // Оптимизация для констант
        if (ass.Expr is IntNode intNode)
        {
            if (varType == SemanticType.DoubleType)
            {
                // int -> double: создаем double константу напрямую
                _code.Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, (double)intNode.Val));
            }
            else
            {
                _code.Add(ThreeAddr.CreateConst(Commands.icass, varAddress, intNode.Val));
            }
        }
        else if (ass.Expr is DoubleNode doubleNode)
        {
            if (varType == SemanticType.IntType)
            {
                // double -> int: преобразуем с потерей точности
                _code.Add(ThreeAddr.CreateConst(Commands.icass, varAddress, (int)doubleNode.Val));
            }
            else
            {
                _code.Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, doubleNode.Val));
            }
        }
        else
        {
            ass.Expr.VisitP(this);
            var exprType = TypeChecker.CalcType(ass.Expr);
            int exprResultTemp = _tempCounter - 1;
            varAddress = GetVariableAddress(ass.Ident.Name);
            Commands command = exprType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
            _code.Add(ThreeAddr.CreateAssign(command, varAddress, exprResultTemp));
        }
    }

    public void VisitAssignOp(AssignOpNode ass)
    {
        int varAddress = GetVariableAddress(ass.Ident.Name);
        var varType = TypeChecker.CalcType(ass.Ident);
        
        int currentValueTemp = NewTemp();
        Commands loadCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        _code.Add(ThreeAddr.CreateAssign(loadCommand, currentValueTemp, varAddress));
        
        ass.Expr.VisitP(this);
        int exprResultTemp = _tempCounter - 1;
        var exprType = TypeChecker.CalcType(ass.Expr);
        
        // Handle type conversion if needed
        if (varType == SemanticType.DoubleType && exprType == SemanticType.IntType)
        {
            int convertedTemp = NewTemp();
            _code.Add(ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp));
            exprResultTemp = convertedTemp;
        }
        
        int operationResultTemp = NewTemp();
        
        // Использование таблицы для операций присваивания
        if (_assignOpTable.TryGetValue((varType, ass.Op), out Commands operationCommand))
        {
            _code.Add(ThreeAddr.CreateBinary(operationCommand, currentValueTemp, exprResultTemp, operationResultTemp));
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported assignment operation '{ass.Op}' for type {varType}");
        }
        
        Commands storeCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        _code.Add(ThreeAddr.CreateAssign(storeCommand, varAddress, operationResultTemp));
    }

    public void VisitIf(IfNode ifn)
    {
        string elseLabel = NewLabel();
        string endLabel = NewLabel();
        
        ifn.Condition.VisitP(this);
        int condTemp = _tempCounter - 1;
        
        _code.Add(ThreeAddr.Create(Commands.iif, condTemp, elseLabel));
        ifn.ThenStat.VisitP(this);
        _code.Add(ThreeAddr.Create(Commands.go, endLabel));
        
        _code.Add(ThreeAddr.Create(Commands.label, elseLabel));
        ifn.ElseStat?.VisitP(this);
        
        _code.Add(ThreeAddr.Create(Commands.label, endLabel));
    }

    public void VisitWhile(WhileNode whn)
    {
        string startLabel = NewLabel();
        string endLabel = NewLabel();
        
        _code.Add(ThreeAddr.Create(Commands.label, startLabel));
        
        whn.Condition.VisitP(this);
        int condTemp = _tempCounter - 1;
        
        _code.Add(ThreeAddr.Create(Commands.ifn, condTemp, endLabel));
        whn.Stat.VisitP(this);
        _code.Add(ThreeAddr.Create(Commands.go, startLabel));
        
        _code.Add(ThreeAddr.Create(Commands.label, endLabel));
    }

    public void VisitFor(ForNode forNode)
    {
        string startLabel = NewLabel();
        string endLabel = NewLabel();
        
        forNode.Counter.VisitP(this);
        
        _code.Add(ThreeAddr.Create(Commands.label, startLabel));
        
        forNode.Condition.VisitP(this);
        int condTemp = _tempCounter - 1;
        _code.Add(ThreeAddr.Create(Commands.ifn, condTemp, endLabel));
        
        forNode.Stat.VisitP(this);
        forNode.Increment.VisitP(this);
        
        _code.Add(ThreeAddr.Create(Commands.go, startLabel));
        _code.Add(ThreeAddr.Create(Commands.label, endLabel));
    }

    public void VisitProcCall(ProcCallNode p)
    {
        foreach (var param in p.Pars.lst)
        {
            param.VisitP(this);
            int paramTemp = _tempCounter - 1;
            _code.Add(ThreeAddr.Create(Commands.param, paramTemp));
        }
        
        _code.Add(ThreeAddr.Create(Commands.call, 0, p.Name.Name));
    }

    public void VisitFuncCall(FuncCallNode f)
    {
        foreach (var param in f.Pars.lst)
        {
            param.VisitP(this);
            int paramTemp = _tempCounter - 1;
            _code.Add(ThreeAddr.Create(Commands.param, paramTemp));
        }
        
        int resultTemp = NewTemp();
        _code.Add(ThreeAddr.Create(Commands.call, resultTemp, f.Name.Name));
    }

    public void FinalizeCode()
    {
        _code.Add(ThreeAddr.Create(Commands.stop));
        ResolveLabels();
    }

    private void ResolveLabels()
    {
        _labelAddresses.Clear();
        for (int i = 0; i < _code.Count; i++)
        {
            if (_code[i].command == Commands.label)
            {
                _labelAddresses[_code[i].Label] = i;
            }
        }
    }
}