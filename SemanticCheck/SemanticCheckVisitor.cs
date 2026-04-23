using MyInterpreter.SemanticCheck;

namespace MyInterpreter;

using static SymbolTree;
using static TypeChecker;

public class SemanticCheckVisitor : AutoVisitor
{
    // Храним информацию о текущей проверяемой функции для рекурсивных вызовов
    private static readonly Stack<FunctionSpecialization> CurrentCheckingFunctionSpecialization = new();
    private static NameSpace _currentNamespace;

    static SemanticCheckVisitor()
    {
        _currentNamespace = SymbolTree.Global;
        if (FunctionTable.TryGetValue("Main", out var functionTable))
            CurrentCheckingFunctionSpecialization.Push(functionTable.Specializations.First());
        else
            throw new CompilerExceptions.UnExpectedException(
                "Something went wrong! We have no Main function in function table!");
    }

    public static void Reset()
    {
        _currentNamespace = SymbolTree.Global;
        CurrentCheckingFunctionSpecialization.Clear();
        if (FunctionTable.TryGetValue("Main", out var functionTable))
            CurrentCheckingFunctionSpecialization.Push(functionTable.Specializations.First());
        else
            throw new CompilerExceptions.UnExpectedException(
                "Something went wrong! We have no Main function in function table!");
    }

    public override void VisitDefinitionsAndStatements(DefinitionsAndStatements DefandStmts)
    {
        RegisterFunctionDeclarations(DefandStmts.DefinitionsList);
        DefandStmts.DefinitionsList.VisitP(this);
        
        _currentNamespace = CurrentCheckingFunctionSpecialization.Peek().NameSpace;
        
        DefandStmts.MainProgram.VisitP(this);
    }

    private void RegisterFunctionDeclarations(DefinitionsListNode definitions)
    {
        foreach (var definition in definitions.lst)
        {
            if (definition is FuncDefNode funcDef)
                SymbolTree.RegisterFunctionDeclaration(funcDef, _currentNamespace);
        }
    }

    public override void VisitVarAssign(VarAssignNode vass)
    {
        vass.Expr.VisitP(this);
        var typ = CalcTypeVis(vass.Expr, _currentNamespace);
        try
        {
            _currentNamespace.AddVariable(vass.Ident.Name, typ);
        }
        catch (InvalidOperationException ex)
        {
            CompilerExceptions.SemanticError(
                $"Переменная {vass.Ident.Name} уже объявлена!", vass.Ident.Pos);
        }

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
       
        var variable = _currentNamespace.LookupVariable(ass.Ident.Name);
        if (CurrentCheckingFunctionSpecialization.Count > 0 && variable != null)
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
       
        if (  _currentNamespace.LookupVariable(id.Name) == null && CurrentCheckingFunctionSpecialization.Count == 0)
        {
            CompilerExceptions.SemanticError($"Идентификатор {id.Name} не определен", id.Pos);
            return;
        }

        if (CurrentCheckingFunctionSpecialization.Count > 0)
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
        var funcSpec = SymbolTree.RegisterFunctionDeclaration(node, _currentNamespace);

        // Тут проверим тело функции
        CurrentCheckingFunctionSpecialization.Push(funcSpec);
        _currentNamespace = funcSpec.NameSpace;
        InferReturnType(node.Name.Name, funcSpec);
        CheckFunctionBody(funcSpec, node.Body);
        _currentNamespace = _currentNamespace.Parent;
        
        if (CurrentCheckingFunctionSpecialization.Count > 1)
            CurrentCheckingFunctionSpecialization.Pop();
    }

    public override void VisitProcCall(ProcCallNode p)
    {
          // Вычисляем типы аргументов
          var argTypes = new List<SemanticType>();
          foreach (var arg in p.Pars.lst)
          {
              arg.VisitP(this);
              argTypes.Add(CalcTypeVis(arg, _currentNamespace));
          }
          var specialization = SymbolTree.ResolveCallSpecialization(p.Name.Name, argTypes.ToArray(), p.Pos);
        
          // Проверяем совместимость типов аргументов
          for (var i = 0; i < specialization.ParameterTypes.Length; i++)
          {
              var argType = argTypes[i];
              var paramType = specialization.ParameterTypes[i];

              if (paramType != argType)
                  CompilerExceptions.SemanticError(
                      $"Тип аргумента процедуры {argType} не соответствует типу формального параметра {paramType}",
                      p.Name.Pos);
          }
        
          
          
          
          InferReturnTypeInSpecializationNamespace(p.Name.Name, specialization);

          // Если тело функции еще не проверено для этой специализации, проверяем его
          try
          {
              if (!specialization.BodyChecked && !IsStandardFunction(p.Name.Name))
              {
                  ExecuteInSpecializationNamespace(specialization,
                      () => CheckFunctionBodyWithSpecialization(p.Name.Name, specialization));
              }
          }
          catch (CompilerExceptions.SemanticException ex)
          {
              CompilerExceptions.SemanticError($"Ошибка при вызове процедуры {p.Name}: {ex.Message}", p.Pos);
          }
          
          if (specialization.ReturnType != SemanticType.NoType)
          {
              CompilerExceptions.SemanticError("Попытка вызвать функцию " + p.Name.Name + " как процедуру", p.Name.Pos);
              return;
          }
          p.SpecializationId = specialization.SpecializationId;
    }

    public override void VisitFuncCall(FuncCallNode f)
    {
        // Вычисляем типы аргументов
        var argTypes = new List<SemanticType>();
        foreach (var arg in f.Pars.lst)
        {
            arg.VisitP(this);
            argTypes.Add(CalcTypeVis(arg, _currentNamespace));
        }
        var specialization = SymbolTree.ResolveCallSpecialization(f.Name.Name, argTypes.ToArray(), f.Pos);
        
        // Проверяем совместимость типов аргументов
        for (var i = 0; i < specialization.ParameterTypes.Length; i++)
        {
            var argType = argTypes[i];
            var paramType = specialization.ParameterTypes[i];

            if (paramType != argType)
                CompilerExceptions.SemanticError(
                    $"Тип аргумента функции {argType} не соответствует типу формального параметра {paramType}",
                    f.Name.Pos);
        }

        InferReturnTypeInSpecializationNamespace(f.Name.Name, specialization);
        
        if (specialization.ReturnType == SemanticType.NoType)
        {
            CompilerExceptions.SemanticError("Попытка вызвать процедуру " + f.Name.Name + " как функцию", f.Name.Pos);
            return;
        }
        if(specialization.ReturnType == SemanticType.UnknownType)
            CompilerExceptions.SemanticError($"Невозможно автоматически вывести тип возвращаемого значения функции {f.Name.Name}",f.Name.Pos);
        // Если тело функции еще не проверено для этой специализации, проверяем его
        try
        {
            if (!specialization.BodyChecked && !IsStandardFunction(f.Name.Name))
            {
                ExecuteInSpecializationNamespace(specialization,
                    () => CheckFunctionBodyWithSpecialization(f.Name.Name, specialization));
            }
        }
        catch (CompilerExceptions.SemanticException ex)
        {
            CompilerExceptions.SemanticError($"Ошибка при вызове функцииx {f.Name}: {ex.Message}", f.Pos);
        }

        // Устанавливаем тип возвращаемого значения для вызова функции
        f.ValueType = specialization.ReturnType;
        f.SpecializationId = specialization.SpecializationId;
    }

    private SemanticType InferReturnType(string functionName, FunctionSpecialization specialization)
    {
        if (specialization.State is FunctionSpecializationState.Inferred
            or FunctionSpecializationState.Checking
            or FunctionSpecializationState.Checked)
            return specialization.ReturnType;

        if (specialization.State == FunctionSpecializationState.Failed)
            return SemanticType.BadType;

        if (specialization.State == FunctionSpecializationState.Inferring)
            return SemanticType.UnknownType;

        if (IsStandardFunction(functionName))
            return specialization.ReturnType;

        if (!FunctionTable.TryGetValue(functionName, out var functionDef))
        {
            CompilerExceptions.SemanticError($"Не найдено определение функции '{functionName}'", new Position(0, 0));
            return SemanticType.BadType;
        }

        var definition = specialization.Definition ?? functionDef.Definition;
        if (definition == null)
            throw new CompilerExceptions.UnExpectedException($"Не найдено определение функции '{functionName}'");

        if (definition.IsReturnTypeDeclared)
        {
            try
            {
                specialization.ReturnType = definition.ReturnType;
                specialization.State = FunctionSpecializationState.Inferred;
                ValidateReturnTypes(definition.Body, definition.ReturnType, definition.Pos);
                return specialization.ReturnType;
            }
            catch
            {
                specialization.ReturnType = SemanticType.BadType;
                specialization.State = FunctionSpecializationState.Failed;
                throw;
            }
        }

        specialization.State = FunctionSpecializationState.Inferring;
        specialization.ReturnType = SemanticType.UnknownType;

        try
        {
            var returnTypes = new List<SemanticType>();
            CollectReturnTypes(
                functionDef.IsTemplateFunction
                    ? functionDef.Definition.Body
                    : definition.Body,
                returnTypes);

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

            specialization.State = FunctionSpecializationState.Inferred;
            return specialization.ReturnType;
        }
        catch
        {
            specialization.ReturnType = SemanticType.BadType;
            specialization.State = FunctionSpecializationState.Failed;
            throw;
        }
    }

    private SemanticType ResolveReturnTypeForInference(string functionName, SemanticType[] argTypes, Position position)
    {
        var specialization = SymbolTree.ResolveCallSpecialization(functionName, argTypes, position);
        return InferReturnTypeInSpecializationNamespace(functionName, specialization);
    }

    private SemanticType InferReturnTypeInSpecializationNamespace(string functionName, FunctionSpecialization specialization)
    {
        if (IsStandardFunction(functionName))
            return InferReturnType(functionName, specialization);

        var oldNamespace = _currentNamespace;
        _currentNamespace = specialization.NameSpace ?? _currentNamespace;
        try
        {
            return InferReturnType(functionName, specialization);
        }
        finally
        {
            _currentNamespace = oldNamespace;
        }
    }

    private void ExecuteInSpecializationNamespace(FunctionSpecialization specialization, Action action)
    {
        var oldNamespace = _currentNamespace;
        _currentNamespace = specialization.NameSpace ?? _currentNamespace;
        try
        {
            action();
        }
        finally
        {
            _currentNamespace = oldNamespace;
        }
    }

    private void CheckFunctionBody(FunctionSpecialization specialization, StatementNode body)
    {
        if (specialization.BodyChecked)
            return;

        specialization.State = FunctionSpecializationState.Checking;
        specialization.BodyChecked = true;
        try
        {
            body.VisitP(this);
            specialization.State = FunctionSpecializationState.Checked;
        }
        catch
        {
            specialization.State = FunctionSpecializationState.Failed;
            specialization.BodyChecked = false;
            throw;
        }
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
            CurrentCheckingFunctionSpecialization.Push(specialization);
            InferReturnType(functionName, specialization);
    
            if (functionDef.Definition.Clone() is not FuncDefNode clonedDef)
                throw new CompilerExceptions.UnExpectedException($"Не удалось клонировать определение функции '{functionName}'");

            specialization.Definition = clonedDef;
            CheckFunctionBody(specialization, clonedDef.Body);
        }
        finally
        {
            if (CurrentCheckingFunctionSpecialization.Count > 1)
                CurrentCheckingFunctionSpecialization.Pop();
        }
        
        
    }

    private void CollectReturnTypes(StatementNode node, List<SemanticType> returnTypes)
    {
        if (node is ReturnNode returnNode)
        {
            if (returnNode.Expr != null)
            {
                // Вычисляем тип выражения в return
                var returnType = CalcTypeVis(returnNode.Expr, _currentNamespace, ResolveReturnTypeForInference);
                // if (returnTypes.Count == 0)
                //     CurrentCheckingFunctionSpecialization.Peek().ReturnType = returnType;
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

    private void ValidateReturnTypes(StatementNode node, SemanticType expectedReturnType, Position funcPosition)
{
    if (node is ReturnNode returnNode)
    {
        // Получаем тип выражения в return (или NoType для return без значения)
        SemanticType actualReturnType;
        
        if (returnNode.Expr != null)
        {
            actualReturnType = CalcTypeVis(returnNode.Expr, _currentNamespace, ResolveReturnTypeForInference);
            
            // Проверяем совместимость с ожидаемым типом
            if (!AreTypesCompatible(actualReturnType, expectedReturnType))
            {
                string expectedStr = expectedReturnType.ToString();
                string actualStr = actualReturnType.ToString();
                
                CompilerExceptions.SemanticError(
                    $"Несовместимый тип возвращаемого значения. Ожидалось: {expectedStr}, получено: {actualStr}",
                    returnNode.Pos ?? funcPosition);
            }
        }
        else
        {
            // return без выражения
            if (expectedReturnType != SemanticType.NoType)
            {
                CompilerExceptions.SemanticError(
                    $"Функция должна возвращать значение типа {expectedReturnType}, " +
                    "но обнаружен return без значения",
                    returnNode.Pos ?? funcPosition);
            }
        }
    }
    else if (node is StatementListNode statementList)
    {
        foreach (var stmt in statementList.lst)
        {
            ValidateReturnTypes(stmt, expectedReturnType, funcPosition);
        }
    }
    else if (node is BlockNode blcNode)
    {
        foreach (var stmt in blcNode.lst.lst)
        {
            ValidateReturnTypes(stmt, expectedReturnType, funcPosition);
        }
    }
    else if (node is IfNode ifNode)
    {
        ValidateReturnTypes(ifNode.ThenStat, expectedReturnType, funcPosition);
        if (ifNode.ElseStat != null)
        {
            ValidateReturnTypes(ifNode.ElseStat, expectedReturnType, funcPosition);
        }
        else
        {
            // Если нет else, то может не быть return в одной из веток
            CheckAllPathsReturn(ifNode.ThenStat, expectedReturnType, ifNode.Pos ?? funcPosition);
        }
    }
    else if (node is WhileNode whileNode)
    {
        ValidateReturnTypes(whileNode.Stat, expectedReturnType, funcPosition);
        // Цикл может не выполниться, поэтому нужен return после цикла
    }
    else if (node is ForNode forNode)
    {
        ValidateReturnTypes(forNode.Stat, expectedReturnType, funcPosition);
        // Цикл может не выполниться, поэтому нужен return после цикла
    }
}
    // Проверка, что все пути выполнения возвращают значение
    private void CheckAllPathsReturn(StatementNode node, SemanticType expectedReturnType, Position pos)
    {
        var returnTypes = new List<SemanticType>();
        CollectReturnTypes(node, returnTypes);
    
        if (expectedReturnType != SemanticType.NoType && returnTypes.Count == 0)
        {
            CompilerExceptions.SemanticError(
                "Не все пути выполнения возвращают значение",
                pos);
        }
    }
    private bool AreTypesCompatible(SemanticType actual, SemanticType expected)
    {
        if (expected == SemanticType.AnyType) return true;
        if (actual == SemanticType.BadType) return false;
    
        // Прямое соответствие
        if (actual == expected) return true;
    
        // Неявные преобразования
        if (expected == SemanticType.DoubleType && actual == SemanticType.IntType)
            return true;
    
        return false;
    }

    private SemanticType GetMoreGeneralType(SemanticType type1, SemanticType type2)
    {
        if (type1 == SemanticType.UnknownType) return type2;
        if (type2 == SemanticType.UnknownType) return type1;

        if (type1 == type2) return type1;

        if ((type1 == SemanticType.DoubleType && type2 == SemanticType.IntType) ||
            (type1 == SemanticType.IntType && type2 == SemanticType.DoubleType))
            return SemanticType.DoubleType;

        if (type1 == SemanticType.AnyType || type2 == SemanticType.AnyType)
            return SemanticType.AnyType;

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
