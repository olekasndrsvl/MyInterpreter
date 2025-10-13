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
        
        switch (bin.Op)
        {
            case TokenType.Plus:
                _code.Add(ThreeAddr.CreateBinary(Commands.iadd, leftTemp, rightTemp, resultTemp));
                break;
            case TokenType.Less:
                _code.Add(ThreeAddr.CreateBinary(Commands.ilt, leftTemp, rightTemp, resultTemp));
                break;
            // Добавьте другие операторы по необходимости
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
        _code.Add(ThreeAddr.CreateAssign(Commands.iass, tempIndex, varAddress));
    }

    public void VisitAssign(AssignNode ass)
    {
        ass.Expr.VisitP(this);
        int exprResultTemp = _tempCounter - 1;
        
        int varAddress = GetVariableAddress(ass.Ident.Name);
        _code.Add(ThreeAddr.CreateAssign(Commands.iass, varAddress, exprResultTemp));
    }

    public void VisitAssignOp(AssignOpNode ass)
    {
        int varAddress = GetVariableAddress(ass.Ident.Name);
        
        int currentValueTemp = NewTemp();
        _code.Add(ThreeAddr.CreateAssign(Commands.iass, currentValueTemp, varAddress));
        
        ass.Expr.VisitP(this);
        int exprResultTemp = _tempCounter - 1;
        
        int operationResultTemp = NewTemp();
        switch (ass.Op)
        {
            case '+':
                _code.Add(ThreeAddr.CreateBinary(Commands.iadd, currentValueTemp, exprResultTemp, operationResultTemp));
                break;
        }
        
        _code.Add(ThreeAddr.CreateAssign(Commands.iass, varAddress, operationResultTemp));
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