using MyInterpreter.SemanticCheck;

namespace MyInterpreter;

using static SymbolTree;
using static TypeChecker;

public class SemanticCheckVisitor : AutoVisitor
{
    // Храним информацию о текущей проверяемой функции для рекурсивных вызовов
    private static readonly Stack<FunctionSpecialization> _currentCheckingFunctionSpecialization = new();
    private static NameSpace _currentNamespace;

    static SemanticCheckVisitor()
    {
        _currentNamespace = SymbolTree.Global;
        if (FunctionTable.TryGetValue("Main", out var functionTable))
            _currentCheckingFunctionSpecialization.Push(functionTable.Specializations.First());
        else
            throw new CompilerExceptions.UnExpectedException(
                "Something went wrong! We have no Main function in function table!");
    }

    public static void Reset()
    {
        _currentNamespace = SymbolTree.Global;
        _currentCheckingFunctionSpecialization.Clear();
        if (FunctionTable.TryGetValue("Main", out var functionTable))
            _currentCheckingFunctionSpecialization.Push(functionTable.Specializations.First());
        else
            throw new CompilerExceptions.UnExpectedException(
                "Something went wrong! We have no Main function in function table!");
    }

    public override void VisitDefinitionsAndStatements(DefinitionsAndStatements DefandStmts)
    {
        DefandStmts.DefinitionsList.VisitP(this);
        
        _currentNamespace = _currentCheckingFunctionSpecialization.Peek().NameSpace;
        
        DefandStmts.MainProgram.VisitP(this);
    }
    public override void VisitVarAssign(VarAssignNode vass)
    {
        vass.Expr.VisitP(this);
        var typ = CalcTypeVis(vass.Expr, _currentNamespace);
        _currentNamespace.AddVariable(vass.Ident.Name,typ);
        vass.Ident.ValueType = typ;
    }

    
    public override void VisitBlockNode(BlockNode bln)
    {
        bln.BlockNameSpace = _currentNamespace.CreateLightWeightChild("BlockOfCode");
        _currentNamespace = bln.BlockNameSpace;
        bln.lst.VisitP(this);
        _currentNamespace = _currentNamespace.Parent;
    }

    public override void VisitAssign(AssignNode ass)
    {
        ass.Expr.VisitP(this);
        // Вычислить тип
        var typ = CalcTypeVis(ass.Expr, _currentNamespace);
        ass.Ident.ValueType = typ;
        var variable = _currentNamespace.LookupVariable(ass.Ident.Name);
        if (_currentCheckingFunctionSpecialization.Count > 0 && variable != null)
        {
            if (FunctionTable.ContainsKey(ass.Ident.Name))
                CompilerExceptions.SemanticError("Переменная не может иметь имя функции!", ass.Ident.Pos);
            // Функции присваивать нельзя
            if (variable.Kind == KindType.FuncName)
                CompilerExceptions.SemanticError(
                    $"Имени стандартной функции {ass.Ident.Name} нельзя присвоить значение", ass.Ident.Pos);
            // Вычислить тип
            var idtyp = variable.Type;

            if (!AssignComparable(idtyp, typ))
                CompilerExceptions.SemanticError(
                    $"Переменной {ass.Ident.Name} типа {idtyp} нельзя присвоить выражение типа {typ}", ass.Ident.Pos);
            ass.Ident.ValueType = typ;
        }
        else
        {
            CompilerExceptions.SemanticError($"Переменная с именем {ass.Ident.Name} не объявлена!", ass.Ident.Pos);
        }
    }

    public override void VisitAssignOp(AssignOpNode ass)
    {
      
        ass.Expr.VisitP(this);

        if (_currentNamespace.LookupVariable(ass.Ident.Name)==null)
        {
            CompilerExceptions.SemanticError($"Переменная {ass.Ident.Name} не определена", ass.Ident.Pos);
        }
        else
        {
            // Функции присваивать нельзя
            if (_currentNamespace.LookupVariable(ass.Ident.Name).Kind == KindType.FuncName)
                CompilerExceptions.SemanticError(
                    $"Имени стандартной функции {ass.Ident.Name} нельзя присвоить значение", ass.Ident.Pos);

            var typ = CalcTypeVis(ass.Expr, _currentNamespace);
            var idtyp = _currentNamespace.LookupVariable(ass.Ident.Name).Type;

            if (idtyp != SemanticType.IntType && idtyp != SemanticType.DoubleType)
                CompilerExceptions.SemanticError($"Операция {ass.Op} не определена для типа {idtyp}", ass.Ident.Pos);

            if (idtyp == SemanticType.IntType && ass.Op == '/')
                CompilerExceptions.SemanticError($"Операция {ass.Op} не определена для типа {idtyp}", ass.Ident.Pos);

            if (!AssignComparable(idtyp, typ))
                CompilerExceptions.SemanticError(
                    $"Переменной {ass.Ident.Name} типа {idtyp} нельзя присвоить выражение типа {typ}", ass.Ident.Pos);


            ass.Ident.ValueType = typ;
        }
    }

    public override void VisitIf(IfNode ifn)
    {
        ifn.Condition.VisitP(this);
        var typ = CalcTypeVis(ifn.Condition, _currentNamespace);
        
        
        if (typ != SemanticType.BoolType)
            CompilerExceptions.SemanticError($"Ожидалось выражение логического типа, а встречено выражение типа {typ}",
                ifn.Condition.Pos);
        
        
        ifn.ThenNameSpaceSpace = _currentNamespace.CreateLightWeightChild("if_then");
        _currentNamespace = ifn.ThenNameSpaceSpace;
        ifn.ThenStat.VisitP(this);
        _currentNamespace = _currentNamespace.Parent;
        
        if (ifn.ElseStat != null)
        {  
            ifn.ElseNameSpace = _currentNamespace.CreateLightWeightChild("if_else");
            _currentNamespace = ifn.ThenNameSpaceSpace;
            ifn.ElseStat.VisitP(this);
            _currentNamespace = _currentNamespace.Parent;
        }
    }

    public override void VisitWhile(WhileNode whn)
    {
        whn.WhileNameSpace = _currentNamespace.CreateLightWeightChild("while");
        _currentNamespace = whn.WhileNameSpace;
        
        whn.Condition.VisitP(this);
        var typ = CalcTypeVis(whn.Condition, _currentNamespace);
        
        if (typ != SemanticType.BoolType)
            CompilerExceptions.SemanticError($"Ожидалось выражение логического типа, а встречено выражение типа {typ}",
                whn.Condition.Pos);

        whn.Stat.VisitP(this);
        
        _currentNamespace = _currentNamespace.Parent;
    }

    public override void VisitFor(ForNode forNode)
    {
        forNode.ForNameSpace=_currentNamespace.CreateLightWeightChild("for_node");
        _currentNamespace = forNode.ForNameSpace;
        forNode.Counter.VisitP(this);
        forNode.Condition.VisitP(this);
        var typ = CalcTypeVis(forNode.Condition, _currentNamespace);
        
        if (typ != SemanticType.BoolType)
            CompilerExceptions.SemanticError($"Ожидалось выражение логического типа, а встречено выражение типа {typ}",
                forNode.Condition.Pos);
        forNode.Increment.VisitP(this);
        forNode.Stat.VisitP(this);
        _currentNamespace = _currentNamespace.Parent;
        
        
    }

    public override void VisitId(IdNode id)
    {
       
        if (  _currentNamespace.LookupVariable(id.Name) == null && _currentCheckingFunctionSpecialization.Count == 0)
        {
            CompilerExceptions.SemanticError($"Идентификатор {id.Name} не определен", id.Pos);
            return;
        }

        if (_currentCheckingFunctionSpecialization.Count > 0)
        {
            if (_currentNamespace.LookupVariable(id.Name)==null)
            {
                CompilerExceptions.SemanticError($"Идентификатор {id.Name} не определен", id.Pos);
                return;
            }

            id.ValueType =  _currentNamespace.LookupVariable(id.Name).Type;
            return;
        }

        var symbol =  _currentNamespace.LookupVariable(id.Name);
        id.ValueType = symbol.Type;
    }

    public override void VisitFuncDef(FuncDefNode node)
    {
        
        // Проверяем, не объявлена ли уже функция с таким именем
        if (FunctionTable.ContainsKey(node.Name.Name))
        {
            CompilerExceptions.SemanticError($"Функция '{node.Name.Name}' уже объявлена", node.Name.Pos);
            return;
        }

        if (_currentNamespace != SymbolTree.Global)
        {
            throw new CompilerExceptions.UnExpectedException("Определение функции оказалось не в глобальном пространстве имен!");
        }
        
        // Сохраняем определение функции
        FunctionTable[node.Name.Name] = new FunctionInfo();
        FunctionTable[node.Name.Name].Definition = node;

       
        
        // Добавляем функцию с типами AnyType - конкретные типы будут выведены при вызовах
        var paramTypes = node.Params.Select(p => SemanticType.AnyType).ToArray();
        var paramNames = FunctionTable[node.Name.Name].Definition.Params.Select(x => x.Name).ToArray();
        // var funcSpec = new FunctionSpecialization(paramTypes, FunctionTable[node.Name.Name],
        //    SemanticType.AnyType, paramNames);

        var funcSpec = FunctionTable[node.Name.Name].FindOrCreateSpecialization(paramTypes);
        // Тут проверим тело функции для AnyType
        _currentCheckingFunctionSpecialization.Push(funcSpec);
        _currentNamespace = funcSpec.NameSpace;
        var returnTypes = new List<SemanticType>();
        CollectReturnTypes(FunctionTable[node.Name.Name].Definition.Body, returnTypes);

        // Выводим тип возвращаемого значения
        if (returnTypes.Count > 0)
        {
            // Находим общий тип всех return statements
            var inferredReturnType = returnTypes[0];
            for (var i = 1; i < returnTypes.Count; i++)
                inferredReturnType = GetMoreGeneralType(inferredReturnType, returnTypes[i]);
            funcSpec.ReturnType = inferredReturnType;
        }
        else
        {
            // Если нет return statements, то тип NoType
            funcSpec.ReturnType = SemanticType.NoType;
        }

      

        node.Body.VisitP(this);
        _currentNamespace = _currentNamespace.Parent;
        if (_currentCheckingFunctionSpecialization.Count > 1)
            _currentCheckingFunctionSpecialization.Pop();
        //Global.Children.Remove(Global.Children.Find(x=>x.Name == node.Name.Name+"_0"));
    }

    public override void VisitFuncCall(FuncCallNode f)
    {
        if (!FunctionTable.ContainsKey(f.Name.Name))
        {
            CompilerExceptions.SemanticError("Функция с именем " + f.Name.Name + " не определена", f.Name.Pos);
            return;
        }

        var funcInfo = FunctionTable[f.Name.Name];


      
        // Вычисляем типы аргументов
        var argTypes = new List<SemanticType>();
        foreach (var arg in f.Pars.lst)
        {
            arg.VisitP(this);
            argTypes.Add(CalcTypeVis(arg, _currentNamespace));
        }
        
        if (SymbolTree.FunctionTable[f.Name.Name].FindOrCreateSpecialization(argTypes.ToArray()).ReturnType == SemanticType.NoType)
        {
            CompilerExceptions.SemanticError("Попытка вызвать процедуру " + f.Name.Name + " как функцию", f.Name.Pos);
            return;
        }

        // Проверяем количество параметров
        if (funcInfo.Definition.Params.Count() != f.Pars.lst.Count)
        {
            CompilerExceptions.SemanticError("Несоответствие количества параметров при вызове функции " + f.Name.Name,
                f.Name.Pos);
            return;
        }

        // Ищем подходящую специализацию функции
        var specialization = FunctionTable[f.Name.Name].FindOrCreateSpecialization(argTypes.ToArray());
        
        // Проверяем совместимость типов аргументов
        for (var i = 0; i < specialization.ParameterTypes.Length; i++)
        {
            var argType = argTypes[i];
            var paramType = specialization.ParameterTypes[i];

            if (!AssignComparable(paramType, argType))
                CompilerExceptions.SemanticError(
                    $"Тип аргумента функции {argType} не соответствует типу формального параметра {paramType}",
                    f.Name.Pos);
        }

        // Если тело функции еще не проверено для этой специализации, проверяем его
        if (!specialization.BodyChecked)
        {
            specialization.BodyChecked = true;
            var oldNamespace = _currentNamespace;
            _currentNamespace = specialization.NameSpace;
            CheckFunctionBodyWithSpecialization(f.Name.Name, specialization);
            _currentNamespace=oldNamespace;
        }

        // Устанавливаем тип возвращаемого значения для вызова функции
        f.ValueType = specialization.ReturnType;
        f.SpecializationId = specialization.SpecializationId;
    }

    private void CheckFunctionBodyWithSpecialization(string functionName, FunctionSpecialization specialization)
    {
        if (!FunctionTable.TryGetValue(functionName, out var functionDef))
        {
            CompilerExceptions.SemanticError($"Не найдено определение функции '{functionName}'", new Position(0, 0));
            return;
        }
        
        try
        {
           

            // Обходим тело функции и собираем типы всех return statements
            _currentCheckingFunctionSpecialization.Push(specialization);

            var returnTypes = new List<SemanticType>();
            CollectReturnTypes(functionDef.Definition.Body, returnTypes);

            // Выводим тип возвращаемого значения
            if (returnTypes.Count > 0)
            {
                // Находим общий тип всех return statements
                var inferredReturnType = returnTypes[0];
                for (var i = 1; i < returnTypes.Count; i++)
                    inferredReturnType = GetMoreGeneralType(inferredReturnType, returnTypes[i]);
                specialization.ReturnType = inferredReturnType;
            }
            else
            {
                // Если нет return statements, то тип NoType
                specialization.ReturnType = SemanticType.NoType;
            }

            functionDef.Definition.Body.VisitP(this);
        }
        finally
        {
            if (_currentCheckingFunctionSpecialization.Count > 1)
                _currentCheckingFunctionSpecialization.Pop();
        }
        
        
    }

    private void CollectReturnTypes(StatementNode node, List<SemanticType> returnTypes)
    {
        if (node is ReturnNode returnNode)
        {
            if (returnNode.Expr != null)
            {
                // Вычисляем тип выражения в return
                var returnType = CalcTypeVis(returnNode.Expr, _currentNamespace);
                if (returnTypes.Count == 0)
                    _currentCheckingFunctionSpecialization.Peek().ReturnType = returnType;
                returnTypes.Add(returnType);
            }
            else
            {
                returnTypes.Add(SemanticType.NoType);
            }
        }
        else if (node is StatementListNode statementList)
        {
            foreach (var stmt in statementList.lst) CollectReturnTypes(stmt, returnTypes);
        }
        else if (node is BlockNode blcNode)
        {
            foreach (var stmt in blcNode.lst.lst) CollectReturnTypes(stmt, returnTypes);
        }
        else if (node is IfNode ifNode)
        {
            CollectReturnTypes(ifNode.ThenStat, returnTypes);
            if (ifNode.ElseStat != null) CollectReturnTypes(ifNode.ElseStat, returnTypes);
        }
        else if (node is WhileNode whileNode)
        {
            CollectReturnTypes(whileNode.Stat, returnTypes);
        }
        else if (node is ForNode forNode)
        {
            CollectReturnTypes(forNode.Stat, returnTypes);
        }
    }

    private SemanticType GetMoreGeneralType(SemanticType type1, SemanticType type2)
    {
        if (type1 == type2) return type1;
        if (type1 == SemanticType.AnyType || type2 == SemanticType.AnyType) return SemanticType.AnyType;
        if ((type1 == SemanticType.DoubleType && type2 == SemanticType.IntType) ||
            (type1 == SemanticType.IntType && type2 == SemanticType.DoubleType))
            return SemanticType.DoubleType;

        // Если типы несовместимы, возвращаем BadType
        return SemanticType.BadType;
    }

    public override void VisitReturn(ReturnNode node)
    {
        if (node.Expr != null)
        {
            node.Expr.VisitP(this);
            var returnType = CalcTypeVis(node.Expr, _currentNamespace);
        }
    }
}