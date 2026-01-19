using MyInterpreter.SemanticCheck;


namespace MyInterpreter.Common;

public class FrameSizeVisitor : IVisitorP
{
    private Dictionary<string, int> _frameSizes = new Dictionary<string, int>();
    private Dictionary<string, int> _tempCounters = new Dictionary<string, int>();
    private Stack<FunctionSpecialization> _specializationStack = new Stack<FunctionSpecialization>();
    private Stack<string> _functionStack = new Stack<string>();
    private List<string> _alreadyGeneratedFunctions = new List<string>();
    private NameSpace _currentNameSpace;
    
    public FrameSizeVisitor()
    {
        _functionStack.Push("MainFrame");
        _specializationStack.Push(SymbolTree.FunctionTable["Main"].Specializations.First());
        
        // Начальное количество временных = количество локальных переменных Main
        int initialTemps = SymbolTree.FunctionTable["Main"].Specializations.First().NameSpace.Variables.Count(x => x.Value.Kind == KindType.VarName);
        
        _tempCounters.Add("MainFrame", initialTemps);
        _frameSizes.Add("MainFrame", initialTemps);
    }
    
    public Dictionary<string, int> GetFrameSizes() => _frameSizes;
    
    private int NewTemp()
    {
        string funcName = _functionStack.Peek();
        int current = _tempCounters[funcName];
        _tempCounters[funcName] = current + 1;
        
        // Обновляем размер фрейма если нужно
        if (_tempCounters[funcName] > _frameSizes[funcName])
        {
            _frameSizes[funcName] = _tempCounters[funcName];
        }
        
        return current;
    }
    
    public void VisitNode(Node node) { }
    public void VisitDefinitionNode(DefinitionNode defNode)
    {
        throw new NotImplementedException();
    }

    public void VisitExprNode(ExprNode node) { }
    public void VisitStatementNode(StatementNode node) { }
    
    public void VisitBinOp(BinOpNode bin)
    {
        // Левый операнд
        bin.Left.VisitP(this);
        int leftTemp = _tempCounters[_functionStack.Peek()] - 1;
        
        // Правый операнд
        bin.Right.VisitP(this);
        int rightTemp = _tempCounters[_functionStack.Peek()] - 1;
        
        var leftType = TypeChecker.CalcType(bin.Left, _currentNameSpace);
        var rightType = TypeChecker.CalcType(bin.Right, _currentNameSpace);
        
        // Конвертация типов если нужно
        if (leftType == SemanticType.IntType && rightType == SemanticType.DoubleType)
        {
            NewTemp(); // convertedTemp
        }
        else if (leftType == SemanticType.DoubleType && rightType == SemanticType.IntType)
        {
            NewTemp(); // convertedTemp
        }
        
        // Результат операции
        NewTemp(); // resultTemp
    }
    
    public void VisitStatementList(StatementListNode stl)
    {
        foreach (var st in stl.lst)
        {
            st.VisitP(this);
        }
    }

    public void VisitBlockNode(BlockNode bin)
    {
        throw new NotImplementedException();
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
        NewTemp(); // tempIndex
    }
    
    public void VisitDouble(DoubleNode d)
    {
        NewTemp(); // tempIndex
    }
    
    public void VisitId(IdNode id)
    {
        NewTemp(); // tempIndex
    }
    
    public void VisitAssign(AssignNode ass)
    {
        // Если константа - все равно создаем временный
        if (ass.Expr is IntNode || ass.Expr is DoubleNode)
        {
            return;
        }
        var varType = TypeChecker.CalcType(ass.Ident, _currentNameSpace);
        ass.Expr.VisitP(this);
        var exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);
        
        if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
            NewTemp();
    }
    
    public void VisitAssignOp(AssignOpNode ass)
    {
        NewTemp(); // currentValueTemp
        ass.Expr.VisitP(this);
        
        var varType = TypeChecker.CalcType(ass.Ident, _currentNameSpace);
        SemanticType exprType;
        exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);

        if (varType == SemanticType.DoubleType && exprType == SemanticType.IntType)
        {
            NewTemp(); // convertedTemp
        }
        
        NewTemp(); // operationResultTemp
    }

    public void VisitVarAssign(VarAssignNode ass)
    {
        throw new NotImplementedException();
    }

  
    public void VisitIf(IfNode ifn)
    {
        ifn.Condition.VisitP(this);
        
        // Then ветка
        ifn.ThenStat.VisitP(this);
        
        // Else ветка
        ifn.ElseStat?.VisitP(this);
    }
    
    public void VisitWhile(WhileNode whn)
    {
        // Start label
        whn.Condition.VisitP(this);
        whn.Stat.VisitP(this);
    }
    
    public void VisitFor(ForNode forNode)
    {
        forNode.Counter.VisitP(this);
        forNode.Condition.VisitP(this);
        forNode.Stat.VisitP(this);
        forNode.Increment.VisitP(this);
    }
    
    public void VisitProcCall(ProcCallNode p)
    {
        foreach (var param in p.Pars.lst)
        {
            param.VisitP(this);
        }
    }
    
    public void VisitFuncCall(FuncCallNode f)
    {
        // Параметры
        foreach (var param in f.Pars.lst)
        {
            param.VisitP(this);
        }
        
        // Генерация кода функции если еще не сгенерирована
        string funcFullName = f.Name.Name + f.SpecializationId;
        if (!_alreadyGeneratedFunctions.Contains(funcFullName))
        {
            _alreadyGeneratedFunctions.Add(funcFullName);
            // Устанавливаем новую функцию
            _functionStack.Push(funcFullName);
            
            // Инициализируем счетчик
            var specialization = SymbolTree.FunctionTable[f.Name.Name].Specializations
                .Find(x => x.SpecializationId == f.SpecializationId);
            _tempCounters[funcFullName] = specialization.NameSpace.Variables.Count;
            _frameSizes[funcFullName] = _tempCounters[funcFullName];
            _specializationStack.Push(specialization);
            
            // Генерируем код функции
            SymbolTree.FunctionTable[f.Name.Name].Definition.VisitP(this);
            
            // Восстанавливаем
            _specializationStack.Pop();
            _functionStack.Pop();
        }
        
        // Результат функции
        NewTemp(); // resultTemp
    }
    
    public void VisitFuncDef(FuncDefNode f)
    {
        // Метка функции уже добавлена где-то выше
        f.Body.VisitP(this);
    }

    public void VisitDefinitionsAndStatements(DefinitionsAndStatements DefandStmts)
    {
        DefandStmts.DefinitionsList.VisitP(this);
        DefandStmts.MainProgram.VisitP(this);
    }

    public void VisitDefinitionsList(DefinitionsListNode defList)
    { foreach (var def in defList.lst)
        {
            def.VisitP(this);
        }   
    }

    public void VisitVariableDeclarationNode(VariableDeclarationNode vardecl)
    {
        
    }

    public void VisitReturn(ReturnNode r)
    {
        r.Expr.VisitP(this);
        
        SemanticType returnType = TypeChecker.CalcType(r.Expr, _currentNameSpace);
        SemanticType expectedType = _specializationStack.Peek().ReturnType;
        
        if (returnType == SemanticType.IntType && expectedType == SemanticType.DoubleType)
        {
            NewTemp(); // convertedTemp
        }
        
        // movin не создает новый временный
        // creturn не создает новый временный
    }
    
    private void ResolveLabels() { } // Не нужно для подсчета размеров
}