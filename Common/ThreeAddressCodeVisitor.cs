namespace MyInterpreter.Common;

public class ThreeAddressCodeVisitor : IVisitorP
{
    private List<ThreeAddr> _code = new List<ThreeAddr>();
    private int _tempCounter = 100;
    private int _labelCounter = 0;
    private Dictionary<string, int> _labelAddresses = new Dictionary<string, int>();
    private Dictionary<string, int> _variableAddresses = new Dictionary<string, int>();
    private int _nextVarAddress = 0;
    
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
        
        int resultTemp = NewTemp();

        var leftType = TypeChecker.CalcType(bin.Left);
        var rightType = TypeChecker.CalcType(bin.Right);
        var resultType = TypeChecker.CalcType(bin);

        // Handle type conversion if needed
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

        if (leftType == SemanticType.IntType && rightType == SemanticType.IntType)
        {
            switch (bin.Op)
            {
                case TokenType.Plus:
                    _code.Add(ThreeAddr.CreateBinary(Commands.iadd, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.Less:
                    _code.Add(ThreeAddr.CreateBinary(Commands.ilt, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.Greater:
                    _code.Add(ThreeAddr.CreateBinary(Commands.igt, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.Equal:
                    _code.Add(ThreeAddr.CreateBinary(Commands.ieq, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.NotEqual:
                    _code.Add(ThreeAddr.CreateBinary(Commands.ineq, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.GreaterEqual:
                    _code.Add(ThreeAddr.CreateBinary(Commands.ic2ge, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.LessEqual:
                    _code.Add(ThreeAddr.CreateBinary(Commands.ic2le, leftTemp, rightTemp, resultTemp));
                    break;
            }
        }
        else if (leftType == SemanticType.DoubleType || rightType == SemanticType.DoubleType)
        {
            switch (bin.Op)
            {
                case TokenType.Plus:
                    _code.Add(ThreeAddr.CreateBinary(Commands.radd, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.Less:
                    _code.Add(ThreeAddr.CreateBinary(Commands.rlt, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.Greater:
                    _code.Add(ThreeAddr.CreateBinary(Commands.rgt, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.Equal:
                    _code.Add(ThreeAddr.CreateBinary(Commands.req, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.NotEqual:
                    _code.Add(ThreeAddr.CreateBinary(Commands.rneq, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.GreaterEqual:
                    _code.Add(ThreeAddr.CreateBinary(Commands.rc2ge, leftTemp, rightTemp, resultTemp));
                    break;
                case TokenType.LessEqual:
                    _code.Add(ThreeAddr.CreateBinary(Commands.rc2le, leftTemp, rightTemp, resultTemp));
                    break;
            }
        }
        // Add boolean type handling if needed
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
        ass.Expr.VisitP(this);
        var exprType = TypeChecker.CalcType(ass.Expr);
        int exprResultTemp = _tempCounter - 1;
        int varAddress = GetVariableAddress(ass.Ident.Name);
        
        if (exprType == SemanticType.IntType)
        {
            _code.Add(ThreeAddr.CreateAssign(Commands.iass, varAddress, exprResultTemp));
        }
        else if (exprType == SemanticType.DoubleType)
        {
            _code.Add(ThreeAddr.CreateAssign(Commands.rass, varAddress, exprResultTemp));
        }
        // Add boolean type handling if needed
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
        
        if (varType == SemanticType.DoubleType)
        {
            switch (ass.Op)
            {
                case '+':
                    _code.Add(ThreeAddr.CreateBinary(Commands.rassadd, currentValueTemp, exprResultTemp, operationResultTemp));
                    break;
                case '-':
                    _code.Add(ThreeAddr.CreateBinary(Commands.rasssub, currentValueTemp, exprResultTemp, operationResultTemp));
                    break;
                // Add other operations as needed
            }
        }
        else
        {
            switch (ass.Op)
            {
                case '+':
                    _code.Add(ThreeAddr.CreateBinary(Commands.iadd, currentValueTemp, exprResultTemp, operationResultTemp));
                    break;
                case '-':
                    _code.Add(ThreeAddr.CreateBinary(Commands.isub, currentValueTemp, exprResultTemp, operationResultTemp));
                    break;
                // Add other operations as needed
            }
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