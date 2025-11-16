namespace MyInterpreter;
using static MyInterpreter.SymbolTable;
using static MyInterpreter.TypeChecker;

public class SemanticCheckVisitor : AutoVisitor
{
    // Храним информацию о текущей проверяемой функции для рекурсивных вызовов
    private string _currentFunction = null;
    //private Dictionary<string, List<FunctionSpecialization>> _functionSpecializations = new();
   // private Dictionary<string, FuncDefNode> _functionDefinitions = new();
    private Stack<FunctionSpecialization> _currentCheckingFunctionSpecialization = new Stack<FunctionSpecialization>();
   
    public override void VisitAssign(AssignNode ass)
    {
        ass.Expr.VisitP(this);
        
         // Вычислить тип
        var typ = CalcTypeVis(ass.Expr);
        ass.Ident.ValueType = typ;
            
        // Добавить в таблицу символов
        if (_currentCheckingFunctionSpecialization.Count > 0 && !_currentCheckingFunctionSpecialization.Peek().LocalVariableTypes.ContainsKey(ass.Ident.Name) )
        {
            _currentCheckingFunctionSpecialization.Peek().LocalVariableTypes.Add(ass.Ident.Name,typ);
        }
        else if(!SymTable.ContainsKey(ass.Ident.Name))
        {
            SymTable[ass.Ident.Name] = new SymbolInfo(ass.Ident.Name, KindType.VarName, typ );
        }
        else
        {
            // Функции присваивать нельзя
            if (SymTable[ass.Ident.Name].Kind == KindType.FuncName)
                CompilerExceptions.SemanticError($"Имени стандартной функции {ass.Ident.Name} нельзя присвоить значение", ass.Ident.Pos);
            
            // Вычислить тип
            var idtyp = SymTable[ass.Ident.Name].Type;
            
            if (!AssignComparable(idtyp, typ))
                CompilerExceptions.SemanticError($"Переменной {ass.Ident.Name} типа {idtyp} нельзя присвоить выражение типа {typ}", ass.Ident.Pos);
            ass.Ident.ValueType = typ;
        }
    }
    
    public override void VisitAssignOp(AssignOpNode ass)
    {
        ass.Expr.VisitP(this);
        
        if (!SymTable.ContainsKey(ass.Ident.Name))
        {
            CompilerExceptions.SemanticError($"Переменная {ass.Ident.Name} не определена", ass.Ident.Pos);
        }
        else
        {
            // Функции присваивать нельзя
            if (SymTable[ass.Ident.Name].Kind == KindType.FuncName)
                CompilerExceptions.SemanticError($"Имени стандартной функции {ass.Ident.Name} нельзя присвоить значение", ass.Ident.Pos);
            
            var typ = CalcTypeVis(ass.Expr);
            var idtyp = SymTable[ass.Ident.Name].Type;
            
            if (idtyp != SemanticType.IntType && idtyp != SemanticType.DoubleType)
                CompilerExceptions.SemanticError($"Операция {ass.Op} не определена для типа {idtyp}", ass.Ident.Pos);
            
            if (idtyp == SemanticType.IntType && ass.Op == '/')
                CompilerExceptions.SemanticError($"Операция {ass.Op} не определена для типа {idtyp}", ass.Ident.Pos);
            
            if (!AssignComparable(idtyp, typ))
                CompilerExceptions.SemanticError($"Переменной {ass.Ident.Name} типа {idtyp} нельзя присвоить выражение типа {typ}", ass.Ident.Pos);
            
            
         
            ass.Ident.ValueType = typ;
        }
    }
    
    public override void VisitIf(IfNode ifn)
    {
        ifn.Condition.VisitP(this);
        var typ = CalcTypeVis(ifn.Condition);
        if (typ != SemanticType.BoolType)
            CompilerExceptions.SemanticError($"Ожидалось выражение логического типа, а встречено выражение типа {typ}", ifn.Condition.Pos);
        
        ifn.ThenStat.VisitP(this);
        if (ifn.ElseStat != null)
            ifn.ElseStat.VisitP(this);
    }
    
    public override void VisitWhile(WhileNode whn)
    {
        whn.Condition.VisitP(this);
        var typ = CalcTypeVis(whn.Condition);
        if (typ != SemanticType.BoolType)
            CompilerExceptions.SemanticError($"Ожидалось выражение логического типа, а встречено выражение типа {typ}", whn.Condition.Pos);
        
        whn.Stat.VisitP(this);
    }
    
    public override void VisitId(IdNode id)
    {
        if (!SymTable.ContainsKey(id.Name) && _currentCheckingFunctionSpecialization.Count==0)
        {
            CompilerExceptions.SemanticError($"Идентификатор {id.Name} не определен", id.Pos);
            return;
        }

        if (_currentCheckingFunctionSpecialization.Count > 0)
        {
            id.ValueType = _currentCheckingFunctionSpecialization.Peek().LocalVariableTypes[id.Name];
            return;
        }
        
        var symbol = SymTable[id.Name];
        id.ValueType = symbol.Type;
    }
    
    public override void VisitFuncDef(FuncDefNode node)
    {
        // Проверяем, не объявлена ли уже функция с таким именем
        if (SymbolTable.SymTable.ContainsKey(node.Name.Name))
        {
            CompilerExceptions.SemanticError($"Функция '{node.Name.Name}' уже объявлена", node.Name.Pos);
            return;
        }

        // Сохраняем определение функции
        FunctionTable[node.Name.Name] = new FunctionInfo();
        FunctionTable[node.Name.Name].Definition = node;

        // Добавляем функцию с типами AnyType - конкретные типы будут выведены при вызовах
        var paramTypes = node.Params.Select(p => SemanticType.AnyType).ToArray();
        SymbolTable.AddFunction(node.Name.Name, paramTypes, SemanticType.AnyType);

        // Инициализируем специализации для этой функции
        if (FunctionTable.ContainsKey(node.Name.Name))
        {
            FunctionTable[node.Name.Name].Specializations =new List<FunctionSpecialization>();
        }
    }

    public override void VisitFuncCall(FuncCallNode f)
    {
       
        if (!FunctionTable.ContainsKey(f.Name.Name)&&!SymbolTable.SymTable.ContainsKey(f.Name.Name))
        {
            CompilerExceptions.SemanticError("Функция с именем " + f.Name.Name + " не определена", f.Name.Pos);
            return;
        }
        var funcInfo = FunctionTable[f.Name.Name];
        var sym = SymbolTable.SymTable[f.Name.Name];
        if (sym.Kind != KindType.FuncName)
        {
            CompilerExceptions.SemanticError("Данное имя " + f.Name.Name + " не является именем функции", f.Name.Pos);
            return;
        }
        
        if (sym.Type == SemanticType.NoType)
        {
            CompilerExceptions.SemanticError("Попытка вызвать процедуру " + f.Name.Name + " как функцию", f.Name.Pos);
            return;
        }
        
        // Вычисляем типы аргументов
        var argTypes = new List<SemanticType>();
        foreach (var arg in f.Pars.lst)
        {
            arg.VisitP(this);
            argTypes.Add(CalcTypeVis(arg));
        }
        
        // Проверяем количество параметров
        if (sym.Params.Count() != f.Pars.lst.Count)
        {
            CompilerExceptions.SemanticError("Несоответствие количества параметров при вызове функции " + f.Name.Name, f.Name.Pos);
            return;
        }
        
        // Ищем подходящую специализацию функции
        FunctionSpecialization specialization = FunctionTable[f.Name.Name].FindOrCreateSpecialization(argTypes.ToArray());
        //FunctionSpecialization specialization = FindOrCreateSpecialization(f.Name.Name, argTypes.ToArray());
        
        // Проверяем совместимость типов аргументов
        for (int i = 0; i < specialization.ParameterTypes.Length; i++)
        {
            var argType = argTypes[i];
            var paramType = specialization.ParameterTypes[i];
            
            if (!AssignComparable(paramType, argType))
            {
                CompilerExceptions.SemanticError($"Тип аргумента функции {argType} не соответствует типу формального параметра {paramType}", f.Name.Pos);
            }
        }
        
        // Если тело функции еще не проверено для этой специализации, проверяем его
        if (!specialization.BodyChecked)
        {
            specialization.BodyChecked = true;
            CheckFunctionBodyWithSpecialization(f.Name.Name, specialization);
          
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

        // Сохраняем текущую функцию
        var previousFunction = _currentFunction;
        _currentFunction = functionName;

        try
        {
            // Создаем временную область видимости для параметров
           
            var oldSymbols = new Dictionary<string, SymbolInfo>(SymbolTable.SymTable);

            try
            {
                // Добавляем параметры в таблицу символов с конкретными типами из специализации
                for (int i = 0; i < functionDef.Definition.Params.Count; i++)
                {
                    var param = functionDef.Definition.Params[i];
                    var paramType = specialization.ParameterTypes[i];
                    SymbolTable.AddVariable(param.Name, paramType);
                    specialization.LocalVariableTypes.Add(param.Name, paramType);
                }
                
                
                
                // Обходим тело функции и собираем типы всех return statements
                _currentCheckingFunctionSpecialization.Push(specialization);
                functionDef.Definition.Body.VisitP(this);
                var returnTypes = new List<SemanticType>();
                CollectReturnTypes(functionDef.Definition.Body, returnTypes);

                // Выводим тип возвращаемого значения
                if (returnTypes.Count > 0)
                {
                    // Находим общий тип всех return statements
                    SemanticType inferredReturnType = returnTypes[0];
                    for (int i = 1; i < returnTypes.Count; i++)
                    {
                        inferredReturnType = GetMoreGeneralType(inferredReturnType, returnTypes[i]);
                    }
                    specialization.ReturnType = inferredReturnType;
                }
                else
                {
                    // Если нет return statements, то тип NoType
                    specialization.ReturnType = SemanticType.NoType;
                }
            }
            finally
            {
                // Восстанавливаем таблицу символов
                SymbolTable.SymTable.Clear();
                foreach (var kvp in oldSymbols)
                {
                    SymbolTable.SymTable[kvp.Key] = kvp.Value;
                }
                if(_currentCheckingFunctionSpecialization.Count>0)
                    _currentCheckingFunctionSpecialization.Pop();
            }
        }
        finally
        {
            _currentFunction = previousFunction;
        }
    }

    private void CollectReturnTypes(StatementNode node, List<SemanticType> returnTypes)
    {
        if (node is ReturnNode returnNode)
        {
            if (returnNode.Expr != null)
            {
                // Вычисляем тип выражения в return
                var returnType = CalcTypeVis(returnNode.Expr);
                returnTypes.Add(returnType);
            }
            else
            {
                returnTypes.Add(SemanticType.NoType);
            }
        }
        else if (node is StatementListNode statementList)
        {
            foreach (var stmt in statementList.lst)
            {
                CollectReturnTypes(stmt, returnTypes);
            }
        }
        else if (node is IfNode ifNode)
        {
            CollectReturnTypes(ifNode.ThenStat, returnTypes);
            if (ifNode.ElseStat != null)
            {
                CollectReturnTypes(ifNode.ElseStat, returnTypes);
            }
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
            var returnType = TypeChecker.CalcTypeVis(node.Expr);
        }
    }
}