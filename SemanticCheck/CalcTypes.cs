using MyInterpreter;

namespace MyInterpreter;
using static MyInterpreter.SymbolTable;

public static class TypeChecker
{
    public static bool AssignComparable(SemanticType leftvar, SemanticType rightexpr)
    {
        if (leftvar == rightexpr)
            return true;
        else if (leftvar == SemanticType.DoubleType && rightexpr == SemanticType.IntType)
            return true;
        else if (leftvar == SemanticType.AnyType && rightexpr != SemanticType.NoType)
            return true;
        else 
            return false;
    }
    
    public static SemanticType CalcTypeVis(ExprNode ex) => ex.Visit(new CalcTypeVisitor());

    // Альтернативная реализация без визитора с использованием pattern matching
    public static SemanticType CalcType(ExprNode ex)
    {
        switch (ex)
        {
            case FuncCallNode funccall:
                return CalcTypeVis(funccall);
            
            case IdNode id:
                if (SymbolTable.SymTable.ContainsKey(id.Name))
                    return SymbolTable.SymTable[id.Name].Type;
                else
                    return SemanticType.BadType;
        
            case IntNode i:
                return SemanticType.IntType;
        
            case DoubleNode d:
                return SemanticType.DoubleType;
        
            case BinOpNode bin:
                var lt = CalcType(bin.Left);
                var rt = CalcType(bin.Right);
            
                if (Constants.ArithmeticOperations.Contains(bin.Op))
                {
                    if (!Constants.NumTypes.Contains(lt) || !Constants.NumTypes.Contains(rt))
                        return SemanticType.BadType;
                    else if (bin.Op == TokenType.Divide)
                        return SemanticType.DoubleType;
                    else if (lt == rt)
                        return lt;
                    else 
                        return SemanticType.DoubleType;
                }
                else if (Constants.LogicalOperations.Contains(bin.Op))
                {
                    if (lt != SemanticType.BoolType || rt != SemanticType.BoolType)
                        return SemanticType.BadType;
                    else 
                        return SemanticType.BoolType;
                }
                else if (Constants.CompareOperations.Contains(bin.Op))
                {
                    if (!Constants.NumTypes.Contains(lt) || !Constants.NumTypes.Contains(rt))
                        return SemanticType.BadType;
                    else 
                        return SemanticType.BoolType;
                }
                break;
        }
    
        return SemanticType.BadType;
    }
    
    // Вспомогательные методы для проверки операций
    public static bool IsArithmeticOperation(TokenType op)
    {
        return op == TokenType.Plus || op == TokenType.Minus ||
               op == TokenType.Multiply || op == TokenType.Divide;
    }

    public static bool IsLogicalOperation(TokenType op)
    {
        return op == TokenType.tkAnd || op == TokenType.tkOr;
    }

    public static bool IsCompareOperation(TokenType op)
    {
        return op == TokenType.Less || op == TokenType.LessEqual ||
               op == TokenType.Greater || op == TokenType.GreaterEqual ||
               op == TokenType.Equal || op == TokenType.NotEqual;
    }
}

public class CalcTypeVisitor : IVisitor<SemanticType>
{
    public SemanticType CalcTypeVis(ExprNode ex) => ex.Visit(this);
    
    public SemanticType VisitNode(Node bin) => SemanticType.NoType;
    
    public SemanticType VisitExprNode(ExprNode bin) => SemanticType.NoType;
    
    public SemanticType VisitStatementNode(StatementNode bin) => SemanticType.NoType;
    
    public SemanticType VisitBinOp(BinOpNode bin)
    {
        var lt = bin.Left.Visit(this);
        var rt = bin.Right.Visit(this);
        
        if (Constants.ArithmeticOperations.Contains(bin.Op))
        {
            if (!Constants.NumTypes.Contains(lt) || !Constants.NumTypes.Contains(rt))
                CompilerExceptions.SemanticError($"Операция {bin.OpToStr()} не определена для типов {lt} и {rt}", bin.Left.Pos);
            else if (bin.Op == TokenType.Divide)
                return SemanticType.DoubleType;
            else if (lt == rt)
                return lt;
            else 
                return SemanticType.DoubleType;
        }
        else if (Constants.LogicalOperations.Contains(bin.Op))
        {
            if (lt != SemanticType.BoolType || rt != SemanticType.BoolType)
                CompilerExceptions.SemanticError($"Операция {bin.OpToStr()} не определена для типов {lt} и {rt}", bin.Left.Pos);
            else 
                return SemanticType.BoolType;
        }
        else if (Constants.CompareOperations.Contains(bin.Op))
        {
            if (!Constants.NumTypes.Contains(lt) || !Constants.NumTypes.Contains(rt))
                 CompilerExceptions.SemanticError($"Операция {bin.OpToStr()} не определена для типов {lt} и {rt}", bin.Left.Pos);
            else 
                return SemanticType.BoolType;
        }
        
        return SemanticType.BadType;
    }
    
    public SemanticType VisitStatementList(StatementListNode stl) => SemanticType.NoType;
    
    public SemanticType VisitExprList(ExprListNode exlist) => SemanticType.NoType;
    
    public SemanticType VisitInt(IntNode n) => SemanticType.IntType;
    
    public SemanticType VisitDouble(DoubleNode d) => SemanticType.DoubleType;
    
    public SemanticType VisitId(IdNode id)
    {
        if (!SymbolTable.SymTable.ContainsKey(id.Name))
             CompilerExceptions.SemanticError("Идентификатор " + id.Name + " не определен", id.Pos);
        else 
            return SymbolTable.SymTable[id.Name].Type;
        
        return SemanticType.BadType;
    }
    
    public SemanticType VisitAssign(AssignNode ass) => SemanticType.NoType;
    
    public SemanticType VisitAssignOp(AssignOpNode ass) => SemanticType.NoType;
    
    public SemanticType VisitIf(IfNode ifn) => SemanticType.NoType;
    
    public SemanticType VisitWhile(WhileNode whn) => SemanticType.NoType;
    
    public SemanticType VisitFor(ForNode forNode) => SemanticType.NoType;
    
    public SemanticType VisitProcCall(ProcCallNode f)
    {
        if (!SymbolTable.SymTable.ContainsKey(f.Name.Name))
             CompilerExceptions.SemanticError("Функция с именем " + f.Name.Name + " не определена", f.Name.Pos);
        
        var sym = SymbolTable.SymTable[f.Name.Name];
        if (sym.Kind != KindType.FuncName)
             CompilerExceptions.SemanticError("Данное имя " + f.Name.Name + " не является именем функции", f.Name.Pos);
        
        if (sym.Type != SemanticType.NoType) // Это функция
             CompilerExceptions.SemanticError("Попытка вызвать функцию " + f.Name.Name + " как процедуру", f.Name.Pos);
        
        if (sym.Params.Count() != f.Pars.lst.Count)
             CompilerExceptions.SemanticError("Несоответствие количества параметров при вызове процедуры " + f.Name.Name, f.Name.Pos);
        
        for (int i = 0; i < sym.Params.Count(); i++)
        {
            var tp = CalcTypeVis(f.Pars.lst[i]);
            if (!TypeChecker.AssignComparable(sym.Params[i], tp))
                CompilerExceptions.SemanticError("Тип аргумента процедуры " + tp.ToString() + " не соответствует типу формального параметра " + sym.Params[i].ToString(), f.Name.Pos);
        }
        
        return SemanticType.NoType;
    }
    
    public SemanticType VisitFuncCall(FuncCallNode f)
    {
        if (!SymbolTable.SymTable.ContainsKey(f.Name.Name))
            CompilerExceptions.SemanticError("Функция с именем " + f.Name.Name + " не определена", f.Name.Pos);
        
        var sym = SymbolTable.SymTable[f.Name.Name];
        if (sym.Kind != KindType.FuncName)
            CompilerExceptions.SemanticError("Данное имя " + f.Name.Name + " не является именем функции", f.Name.Pos);
        
        if (sym.Type == SemanticType.NoType) // Это процедура
            CompilerExceptions.SemanticError("Попытка вызвать процедуру " + f.Name.Name + " как функцию", f.Name.Pos);
        
        // Вычисляем типы аргументов
        var argTypes = new List<SemanticType>();
        foreach (var arg in f.Pars.lst)
        {
            argTypes.Add(CalcTypeVis(arg));
        }
        
        // Получаем информацию о функции с учетом выведенных типов
        var functionSymbol = GetFunctionWithInferredTypes(f.Name.Name, argTypes);
        
        if (functionSymbol.Params.Count() != f.Pars.lst.Count)
            CompilerExceptions.SemanticError("Несоответствие количества параметров при вызове функции " + f.Name.Name, f.Name.Pos);
        
        // Проверяем совместимость типов аргументов
        for (int i = 0; i < functionSymbol.Params.Count(); i++)
        {
            var argType = argTypes[i];
            var paramType = functionSymbol.Params[i];
            
            if (!TypeChecker.AssignComparable(paramType, argType))
                CompilerExceptions.SemanticError($"Тип аргумента функции {argType} не соответствует типу формального параметра {paramType}", f.Name.Pos);
        }
        
        return functionSymbol.Type; // тип возвращаемого значения (выведенный)
    }

    // Метод для получения информации о функции с учетом вывода типов
    private SymbolInfo GetFunctionWithInferredTypes(string functionName, List<SemanticType> argumentTypes)
    {
        if (!SymbolTable.SymTable.ContainsKey(functionName) || SymbolTable.SymTable[functionName].Kind != KindType.FuncName)
            throw new KeyNotFoundException($"Function '{functionName}' not found");

        var functionSymbol = SymbolTable.SymTable[functionName];
        
        // Если функция уже имеет конкретные типы, возвращаем их
        if (functionSymbol.Params.All(t => t != SemanticType.AnyType) && 
            functionSymbol.Type != SemanticType.AnyType)
            return functionSymbol;

        // Выводим типы параметров на основе аргументов
        var inferredParamTypes = new List<SemanticType>();
        for (int i = 0; i < argumentTypes.Count; i++)
        {
            if (i < functionSymbol.Params.Length && functionSymbol.Params[i] == SemanticType.AnyType)
                inferredParamTypes.Add(argumentTypes[i]);
            else if (i < functionSymbol.Params.Length)
                inferredParamTypes.Add(functionSymbol.Params[i]); // Уже имеет конкретный тип
            else
                inferredParamTypes.Add(argumentTypes[i]); // Новый параметр
        }

        // Выводим тип возвращаемого значения (упрощенно - берем тип первого параметра)
        SemanticType inferredReturnType = functionSymbol.Type;
        if (inferredReturnType == SemanticType.AnyType && inferredParamTypes.Count > 0)
            inferredReturnType = inferredParamTypes[0];

        // Обновляем типы функции
        UpdateFunctionTypes(functionName, inferredParamTypes.ToArray(), inferredReturnType);
        
        return SymbolTable.SymTable[functionName];
    }

    // Метод для обновления типов функции
    private void UpdateFunctionTypes(string functionName, SemanticType[] newParamTypes, SemanticType newReturnType)
    {
        if (!SymbolTable.SymTable.ContainsKey(functionName) || SymbolTable.SymTable[functionName].Kind != KindType.FuncName)
            throw new InvalidOperationException($"Function '{functionName}' not found");

        var oldSymbol = SymbolTable.SymTable[functionName];
        SymbolTable.SymTable[functionName] = new SymbolInfo(functionName, KindType.FuncName, newParamTypes, newReturnType);
    }

    public SemanticType VisitFuncDef(FuncDefNode f)
    {
        // Проверяем, не объявлена ли уже функция с таким именем
        if (SymbolTable.SymTable.ContainsKey(f.Name.Name))
        {
            CompilerExceptions.SemanticError($"Функция '{f.Name.Name}' уже объявлена", f.Name.Pos);
            return SemanticType.NoType;
        }

        // Создаем временную область видимости для параметров
      
        
        try
        {
            // Добавляем параметры в таблицу символов с типом AnyType
            var paramTypes = new List<SemanticType>();
            foreach (var param in f.Params)
            {
                // Для параметров используем AnyType - тип будет выведен при вызове
                SymbolTable.AddVariable(param.Name, SemanticType.AnyType);
                paramTypes.Add(SemanticType.AnyType);
               
            }

            // Устанавливаем тип возвращаемого значения как AnyType
            f.ReturnType = SemanticType.AnyType;
            
            // Добавляем функцию в таблицу символов с типами AnyType
            SymbolTable.AddFunction(f.Name.Name, paramTypes.ToArray(), SemanticType.AnyType);
            
            // Проверяем тело функции
            f.Body.Visit(this);
            
            // Пытаемся вывести тип возвращаемого значения из return statements
            InferReturnTypeFromBody(f);
        }
        finally
        {
          
            
            // Удаляем параметры из таблицы символов
            foreach (var param in f.Params)
            {
                SymbolTable.SymTable.Remove(param.Name);
            }
        }
        
        return SemanticType.NoType;
    }

    // Метод для вывода типа возвращаемого значения из тела функции
    private void InferReturnTypeFromBody(FuncDefNode funcDef)
    {
        var returnTypes = new List<SemanticType>();
        CollectReturnTypes(funcDef.Body, returnTypes);
        
        if (returnTypes.Count > 0)
        {
            // Выводим общий тип из всех return statements
            SemanticType inferredType = returnTypes[0];
            foreach (var returnType in returnTypes)
            {
                if (returnType != inferredType)
                {
                    // Если типы разные, используем более общий тип
                    if ((inferredType == SemanticType.IntType && returnType == SemanticType.DoubleType) ||
                        (inferredType == SemanticType.DoubleType && returnType == SemanticType.IntType))
                    {
                        inferredType = SemanticType.DoubleType;
                    }
                    else
                    {
                        CompilerExceptions.SemanticError(
                            $"Несогласованные типы возвращаемых значений в функции '{funcDef.Name.Name}': {inferredType} и {returnType}", 
                            funcDef.Name.Pos);
                    }
                }
            }
            
            // Обновляем тип возвращаемого значения функции
            funcDef.ReturnType = inferredType;
            UpdateFunctionTypes(funcDef.Name.Name, 
                funcDef.Params.Select(p => SemanticType.AnyType).ToArray(), 
                inferredType);
        }
    }

    // Рекурсивный сбор типов из return statements
    private void CollectReturnTypes(StatementNode node, List<SemanticType> returnTypes)
    {
        if (node is ReturnNode returnNode && returnNode.Expr != null)
        {
            returnTypes.Add(CalcTypeVis(returnNode.Expr));
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
    }

    public SemanticType VisitReturn(ReturnNode r)
    {
        if (r.Expr != null)
        {
            var returnType = CalcTypeVis(r.Expr);
            // Проверяем, что возвращаемый тип допустим
            if (returnType == SemanticType.BadType || returnType == SemanticType.NoType)
                CompilerExceptions.SemanticError("Недопустимый тип возвращаемого значения: " + returnType, r.Pos);
            
            return returnType;
        }
        return SemanticType.NoType;
    }

    public SemanticType VisitFuncDefList(FuncDefListNode lst) => SemanticType.NoType;

    public SemanticType VisitFunDefAndStatements(FuncDefAndStatements fdandStmts) => SemanticType.NoType;
}