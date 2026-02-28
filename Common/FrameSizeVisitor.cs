using System.Text;
using MyInterpreter.SemanticCheck;

namespace MyInterpreter.Common;

public class FrameSizeVisitor : IVisitorP
{
    private readonly Stack<string> _currentFunctionName = new();
    private readonly Stack<FunctionSpecialization> _currentFunctionSpecialization = new();
    private NameSpace _currentNameSpace = SymbolTree.Global;
    
    private readonly Dictionary<string, int> _frameSizes = new() {{"GlobalVariables",SymbolTree.Global.Variables.Count}};
    private readonly Dictionary<string, int> _tempCounters = new();
    private int _currentTempCounter;
    
    private readonly HashSet<string> _processedFunctions = new();

    public FrameSizeVisitor()
    {
        // Инициализация для главной функции
        _currentFunctionName.Push("MainFrame");
        _currentFunctionSpecialization.Push(SymbolTree.FunctionTable["Main"].Specializations.First());
        
        var mainSpecialization = SymbolTree.FunctionTable["Main"].Specializations.First();
        var initialTemps = mainSpecialization.NameSpace.Variables.Count(x => x.Value.Kind == KindType.VarName);
        
        _tempCounters["MainFrame"] = initialTemps;
        _frameSizes["MainFrame"] = initialTemps;
        _currentTempCounter = initialTemps;
    }

    public void VisitNode(Node node) { }
    public void VisitDefinitionNode(DefinitionNode defNode) { }
    public void VisitExprNode(ExprNode node) { }
    public void VisitStatementNode(StatementNode node) { }

    public void VisitBinOp(BinOpNode bin)
    {
        bin.Left.VisitP(this);
        bin.Right.VisitP(this);

        var leftType = TypeChecker.CalcType(bin.Left, _currentNameSpace);
        var rightType = TypeChecker.CalcType(bin.Right, _currentNameSpace);

        // Конвертация типов при необходимости
        if (leftType == SemanticType.IntType && rightType == SemanticType.DoubleType)
            NewTemp();
        else if (leftType == SemanticType.DoubleType && rightType == SemanticType.IntType)
            NewTemp();

        // Результат операции
        NewTemp();
    }

    public void VisitStatementList(StatementListNode stl)
    {
        foreach (var st in stl.lst)
            st.VisitP(this);
    }

    public void VisitBlockNode(BlockNode bin)
    {
        if (!_currentNameSpace.Children.Contains(bin.BlockNameSpace))
            throw new CompilerExceptions.UnExpectedException(
                $"The namespace {bin.BlockNameSpace.Name} is not child for {_currentNameSpace.Name}! Tree is wrong!");

        var previousNamespace = _currentNameSpace;
        var previousTempCounter = _currentTempCounter;
        
        _currentNameSpace = bin.BlockNameSpace;
        bin.lst.VisitP(this);
        
        _currentTempCounter = previousTempCounter;
        _currentNameSpace = previousNamespace;
    }

    public void VisitExprList(ExprListNode exlist)
    {
        foreach (var ex in exlist.lst)
            ex.VisitP(this);
    }

    public void VisitInt(IntNode n)
    {
        NewTemp();
    }

    public void VisitDouble(DoubleNode d)
    {
        NewTemp();
    }

    public void VisitId(IdNode id)
    {
        NewTemp();
    }

    public void VisitAssign(AssignNode ass)
    {
        // Если константа - временная переменная не создаётся
        if (ass.Expr is IntNode || ass.Expr is DoubleNode)
            return;

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
        var exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);

        if (varType == SemanticType.DoubleType && exprType == SemanticType.IntType)
            NewTemp(); // convertedTemp

        NewTemp(); // operationResultTemp
    }

    public void VisitVarAssign(VarAssignNode ass)
    {
        // Для объявления переменной выделяем адрес (временную переменную не создаём)
        var varAddress = _currentNameSpace.Variables.Count(x => x.Value.VariableAddress != -1);
        if (_currentNameSpace is LightWeightNameSpace)
        {
            varAddress = _currentTempCounter++;
        }
        _currentNameSpace.Variables[ass.Ident.Name].VariableAddress = varAddress;
        
        var varType = TypeChecker.CalcType(ass.Ident, _currentNameSpace);
        var previousTempCounter = _currentTempCounter;

        // Обработка выражения инициализации
        if (ass.Expr is IntNode || ass.Expr is DoubleNode)
        {
            // Константа - временная не нужна
        }
        else
        {
            ass.Expr.VisitP(this);
            var exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);
            
            if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                NewTemp(); // convertedTemp
        }

        _currentTempCounter = previousTempCounter;
    }

    public void VisitIf(IfNode ifn)
    {
        var previousTempCounter = _currentTempCounter;
        
        ifn.Condition.VisitP(this);
        
        _currentTempCounter = previousTempCounter;
        
        var previousNamespace = _currentNameSpace;
        
        _currentNameSpace = ifn.ThenNameSpaceSpace;
        ifn.ThenStat.VisitP(this);
        
        _currentTempCounter = previousTempCounter;
        
        _currentNameSpace = ifn.ElseNameSpace;
        ifn.ElseStat?.VisitP(this);
        
        _currentTempCounter = previousTempCounter;
        _currentNameSpace = previousNamespace;
    }

    public void VisitWhile(WhileNode whn)
    {
        var previousTempCounter = _currentTempCounter;
        
        whn.Condition.VisitP(this);
        
        var previousNamespace = _currentNameSpace;
        _currentNameSpace = whn.WhileNameSpace;
        whn.Stat.VisitP(this);
        
        _currentTempCounter = previousTempCounter;
        _currentNameSpace = previousNamespace;
    }

    public void VisitFor(ForNode forNode)
    {
        var previousTempCounter = _currentTempCounter;
        var previousNamespace = _currentNameSpace;
        
        _currentNameSpace = forNode.ForNameSpace;
        
        forNode.Counter.VisitP(this);
        forNode.Condition.VisitP(this);
        forNode.Stat.VisitP(this);
        forNode.Increment.VisitP(this);
        
        _currentTempCounter = previousTempCounter;
        _currentNameSpace = previousNamespace;
    }

    public void VisitProcCall(ProcCallNode p)
    {
        foreach (var param in p.Pars.lst)
        {
            param.VisitP(this);
        }
        // Параметры передаются через фрейм, временные не создаются
    }

    public void VisitFuncCall(FuncCallNode f)
    {
        // Обработка параметров
        foreach (var param in f.Pars.lst)
        {
            param.VisitP(this);
        }

        var funcFullName = f.Name.Name + f.SpecializationId;
        
        // Если функция ещё не обработана - обрабатываем её тело
        
        if (!_processedFunctions.Contains(funcFullName) && !_currentFunctionName.Contains(funcFullName) && !SymbolTree.IsStandardFunction(f.Name.Name))
        {
            _processedFunctions.Add(funcFullName);
            
            var previousFunctionName = _currentFunctionName.Peek();
            var previousSpecialization = _currentFunctionSpecialization.Peek();
            var previousTempCounter = _currentTempCounter;
            var previousNamespace = _currentNameSpace;
            
            // Переключаемся на новую функцию
            _currentFunctionName.Push(funcFullName);
            
            var specialization = SymbolTree.FunctionTable[f.Name.Name].Specializations
                .Find(x => x.SpecializationId == f.SpecializationId);
            
            _currentFunctionSpecialization.Push(specialization);
            
            // Инициализируем счётчик для новой функции количеством локальных переменных
            var initialTemps = specialization.NameSpace.Variables.Count(x => x.Value.Kind == KindType.VarName);
            _tempCounters[funcFullName] = initialTemps;
            _frameSizes[funcFullName] = initialTemps;
            _currentTempCounter = initialTemps;
            
            // Устанавливаем адреса переменных
            int i = 0;
            foreach (var variable in specialization.NameSpace.Variables)
            {
                variable.Value.VariableAddress = i++;
            }
            
            // Обрабатываем тело функции
            _currentNameSpace = specialization.NameSpace;
            specialization.Definition.VisitP(this);
            
            // Восстанавливаем контекст
            _currentNameSpace = previousNamespace;
            _currentTempCounter = previousTempCounter;
            _currentFunctionSpecialization.Pop();
            _currentFunctionName.Pop();
        }

        // Результат функции
        NewTemp();
    }

    public void VisitFuncDef(FuncDefNode f)
    {
        f.Body.VisitP(this);
    }

    public void VisitDefinitionsAndStatements(DefinitionsAndStatements DefandStmts)
    {
        DefandStmts.DefinitionsList.VisitP(this);
        _currentNameSpace = _currentNameSpace.Children.Find(x => x.Name == "Main_0");
        DefandStmts.MainProgram.VisitP(this);
    }

    public void VisitDefinitionsList(DefinitionsListNode defList)
    {
        foreach (var def in defList.lst)
        {
            if (def is not FuncDefNode) // Обрабатываем только глобальные переменные
                def.VisitP(this);
        }
    }

    public void VisitVariableDeclarationNode(VariableDeclarationNode vardecl)
    {
        // Для глобальных переменных выделяем адрес
        var varAddress = SymbolTree.Global.Variables.Count(x => x.Value.VariableAddress != -1);
        SymbolTree.Global.Variables[vardecl.vass.Ident.Name].VariableAddress = varAddress;
        SymbolTree.Global.Variables[vardecl.vass.Ident.Name].IsGlobalVariable = true;
        
        // Обрабатываем инициализатор
        if (vardecl.vass.Expr is IntNode || vardecl.vass.Expr is DoubleNode)
        {
            // Константа - временная не нужна
        }
        else
        {
            var previousTempCounter = _currentTempCounter;
            vardecl.vass.Expr.VisitP(this);
            
            var varType = TypeChecker.CalcType(vardecl.vass.Ident, _currentNameSpace);
            var exprType = TypeChecker.CalcType(vardecl.vass.Expr, _currentNameSpace);
            
            if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                NewTemp(); // convertedTemp
                
            _currentTempCounter = previousTempCounter;
        }
    }

    public void VisitReturn(ReturnNode r)
    {
        r.Expr.VisitP(this);

        var returnType = TypeChecker.CalcType(r.Expr, _currentNameSpace);
        var expectedType = _currentFunctionSpecialization.Peek().ReturnType;

        if (returnType == SemanticType.IntType && expectedType == SemanticType.DoubleType)
            NewTemp(); // convertedTemp
    }

    public Dictionary<string, int> GetFrameSizes()
    {
        return _frameSizes;
    }

    private int NewTemp()
    {
        var funcName = _currentFunctionName.Peek();
        var current = _tempCounters[funcName];
        _tempCounters[funcName] = current + 1;
        _currentTempCounter = current + 1;

        // Обновляем максимальный размер фрейма
        if (_tempCounters[funcName] > _frameSizes[funcName])
            _frameSizes[funcName] = _tempCounters[funcName];

        return current;
    }

    public void PrintFrameSizes()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Frame sizes:");
        foreach (var frame in _frameSizes)
        {
            sb.AppendLine($"  {frame.Key}: {frame.Value} \n");
        }
        Console.WriteLine(sb.ToString());
    }
}