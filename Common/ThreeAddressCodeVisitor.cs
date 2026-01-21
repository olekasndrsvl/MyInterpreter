using MyInterpreter.SemanticCheck;

namespace MyInterpreter.Common;

public class ThreeAddressCodeVisitor : IVisitorP
{
    // Таблица для бинарных операций: (тип_левого, тип_правого, токен) -> команда
    private static readonly Dictionary<(SemanticType, SemanticType, TokenType), Commands> _binOpTable =
        new()
        {
            // Целочисленные операции
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Plus), Commands.iadd },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Minus), Commands.isub },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Multiply), Commands.imul },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Divide), Commands.idiv },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Less), Commands.ilt },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Greater), Commands.igt },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.Equal), Commands.ieq },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.NotEqual), Commands.ineq },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.LessEqual), Commands.ic2le },
            { (SemanticType.IntType, SemanticType.IntType, TokenType.GreaterEqual), Commands.ic2ge },

            // Вещественные операции
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Plus), Commands.radd },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Minus), Commands.rsub },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Multiply), Commands.rmul },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Divide), Commands.rdiv },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Less), Commands.rlt },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Greater), Commands.rgt },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.Equal), Commands.req },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.NotEqual), Commands.rneq },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.LessEqual), Commands.rc2le },
            { (SemanticType.DoubleType, SemanticType.DoubleType, TokenType.GreaterEqual), Commands.rc2ge },

            // Смешанные типы (int-double) - преобразуются к double
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Plus), Commands.radd },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Plus), Commands.radd },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Minus), Commands.rsub },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Minus), Commands.rsub },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Multiply), Commands.rmul },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Multiply), Commands.rmul },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Divide), Commands.rdiv },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Divide), Commands.rdiv },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Less), Commands.rlt },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Less), Commands.rlt },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Greater), Commands.rgt },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Greater), Commands.rgt },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.Equal), Commands.req },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.Equal), Commands.req },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.NotEqual), Commands.rneq },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.NotEqual), Commands.rneq },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.LessEqual), Commands.rc2le },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.LessEqual), Commands.rc2le },
            { (SemanticType.IntType, SemanticType.DoubleType, TokenType.GreaterEqual), Commands.rc2ge },
            { (SemanticType.DoubleType, SemanticType.IntType, TokenType.GreaterEqual), Commands.rc2ge }
        };

    // Таблица для операций присваивания с операцией: (тип_переменной, символ_операции) -> команда
    private static readonly Dictionary<(SemanticType, char), Commands> _assignOpTable =
        new()
        {
            { (SemanticType.IntType, '+'), Commands.iadd },
            { (SemanticType.IntType, '-'), Commands.isub },
            { (SemanticType.IntType, '*'), Commands.imul },
            { (SemanticType.IntType, '/'), Commands.idiv },

            { (SemanticType.DoubleType, '+'), Commands.radd },
            { (SemanticType.DoubleType, '-'), Commands.rsub },
            { (SemanticType.DoubleType, '*'), Commands.rmul },
            { (SemanticType.DoubleType, '/'), Commands.rdiv }
        };

    private readonly List<string> _alreadyGeneratedFunctionDefinitions = new();

    private readonly Stack<string> _currentGeneratingFunctionName = new();
    private readonly Stack<FunctionSpecialization> _currentGeneratingFunctionSpecialization = new();
    private NameSpace _currentNameSpace = SymbolTree.Global;

    public Dictionary<string, int> _currentTempIndexes =new Dictionary<string, int>();
    
    private Dictionary<string, int> _frameSizes = new()
        { {"GlobalVariables",SymbolTree.Global.Variables.Count},{ "MainFrame", 40 }, { "factorial0", 20 }, { "factorial1", 20 } };

    private readonly Dictionary<string, List<ThreeAddr>> _function_codes = new();

    private int _labelCounter;

    //private Dictionary<string, int> _variableAddresses = new Dictionary<string, int>();
    private int _nextVarAddress = 0;
    private int _tempCounter;
    private int _globalVariablesCounter=0;
 
    public ThreeAddressCodeVisitor()
    {
        _currentGeneratingFunctionName.Push("MainFrame");
        _function_codes["MainFrame"]= new List<ThreeAddr>();
        _currentGeneratingFunctionSpecialization.Push(SymbolTree.FunctionTable["Main"].Specializations.First());
        _tempCounter = SymbolTree.FunctionTable["Main"].Specializations.First().NameSpace.Variables
            .Count(x => x.Value.Kind == KindType.VarName);
    }

    public List<ThreeAddr> Code { get; } = new();

    public Dictionary<string, int> LabelAddresses { get; } = new();

    public void VisitNode(Node node)
    {
    }

    public void VisitDefinitionNode(DefinitionNode defNode)
    {
    }

    public void VisitExprNode(ExprNode node)
    {
    }

    public void VisitStatementNode(StatementNode node)
    {
    }

    public void VisitBinOp(BinOpNode bin)
    {
        bin.Left.VisitP(this);
        var leftTemp = _tempCounter - 1;

        bin.Right.VisitP(this);
        var rightTemp = _tempCounter - 1;
        var leftType = TypeChecker.CalcType(bin.Left, _currentNameSpace);
        var rightType = TypeChecker.CalcType(bin.Right, _currentNameSpace);

        
            // Автоматическое преобразование типов при необходимости
            if (leftType == SemanticType.IntType && rightType == SemanticType.DoubleType)
            {
                // Convert left int to double
                var convertedTemp = NewTemp();
                _function_codes[_currentGeneratingFunctionName.Peek()]
                    .Add(ThreeAddr.CreateConvert(Commands.citr, leftTemp, convertedTemp, true, true));
                leftTemp = convertedTemp;
                leftType = SemanticType.DoubleType;
            }
            else if (leftType == SemanticType.DoubleType && rightType == SemanticType.IntType)
            {
                // Convert right int to double
                var convertedTemp = NewTemp();
                _function_codes[_currentGeneratingFunctionName.Peek()]
                    .Add(ThreeAddr.CreateConvert(Commands.citr, rightTemp, convertedTemp, true, true));
                rightTemp = convertedTemp;
                rightType = SemanticType.DoubleType;
            }

            var resultTemp = NewTemp();
            // Поиск команды в таблице
            if (_binOpTable.TryGetValue((leftType, rightType, bin.Op), out var command))
                _function_codes[_currentGeneratingFunctionName.Peek()]
                    .Add(ThreeAddr.CreateBinary(command, leftTemp, rightTemp, resultTemp, true, true, true));
            else
                throw new InvalidOperationException(
                    $"Unsupported binary operation {bin.Op} for types {leftType} and {rightType}");
        
    }

    public void VisitStatementList(StatementListNode stl)
    {
        foreach (var st in stl.lst) st.VisitP(this);
    }

    public void VisitBlockNode(BlockNode bin)
    {
        var lastCheckedNamespace = _currentNameSpace;
        _currentNameSpace = bin.BlockNameSpace;
        bin.lst.VisitP(this);
        _currentNameSpace=lastCheckedNamespace;
    }

    public void VisitExprList(ExprListNode exlist)
    {
        foreach (var ex in exlist.lst) ex.VisitP(this);
    }

    public void VisitInt(IntNode n)
    {
        var tempIndex = NewTemp();
        _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.CreateConst(Commands.icass, tempIndex, n.Val, true));
    }

    public void VisitDouble(DoubleNode d)
    {
        var tempIndex = NewTemp();
        _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.CreateConst(Commands.rcass, tempIndex, d.Val, true));
    }

    public void VisitId(IdNode id)
    {
        var tempIndex = NewTemp();
        var varAddress = GetVariableAddress(id.Name);
        var varType = TypeChecker.CalcType(id, _currentNameSpace);
        var command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.CreateAssign(command, tempIndex, varAddress, true, true));
    }

    public void VisitAssign(AssignNode ass)
    {
        var varAddress = GetVariableAddress(ass.Ident.Name);
        var varType = TypeChecker.CalcType(ass.Ident, _currentNameSpace);

        
         // Оптимизация для констант
            if (ass.Expr is IntNode intNode)
            {
                if (varType == SemanticType.DoubleType)
                    // int -> double: создаем double константу напрямую
                    _function_codes[_currentGeneratingFunctionName.Peek()]
                        .Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, (double)intNode.Val, true));
                else
                    _function_codes[_currentGeneratingFunctionName.Peek()]
                        .Add(ThreeAddr.CreateConst(Commands.icass, varAddress, intNode.Val, true));
            }
            else if (ass.Expr is DoubleNode doubleNode)
            {
                _function_codes[_currentGeneratingFunctionName.Peek()]
                    .Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, doubleNode.Val, true));
            }
            else
            {
                ass.Expr.VisitP(this);
                var exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);
                var exprResultTemp = _tempCounter - 1;
                varAddress = GetVariableAddress(ass.Ident.Name);
                if (varType == exprType)
                {
                    var command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
                    _function_codes[_currentGeneratingFunctionName.Peek()]
                        .Add(ThreeAddr.CreateAssign(command, varAddress, exprResultTemp, true, true));
                }
                else
                {
                    if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                    {
                        var convertedTemp = NewTemp();
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(
                            ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp, true, true));
                        _function_codes[_currentGeneratingFunctionName.Peek()]
                            .Add(ThreeAddr.CreateAssign(Commands.rass, varAddress, convertedTemp, true, true));
                    }
                    else
                    {
                        throw new CompilerExceptions.UnExpectedException(
                            "Something went wrong! During generation code for assignment!");
                    }
                }
            }
        
    }

    public void VisitAssignOp(AssignOpNode ass)
    {
        var varAddress = GetVariableAddress(ass.Ident.Name);
        var varType = TypeChecker.CalcType(ass.Ident, _currentNameSpace);

        var currentValueTemp = NewTemp();
        var loadCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.CreateAssign(loadCommand, currentValueTemp, varAddress));

            ass.Expr.VisitP(this);
            var exprResultTemp = _tempCounter - 1;
            var exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);

            // Handle type conversion if needed
            if (varType == SemanticType.DoubleType && exprType == SemanticType.IntType)
            {
                var convertedTemp = NewTemp();
                _function_codes[_currentGeneratingFunctionName.Peek()]
                    .Add(ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp));
                exprResultTemp = convertedTemp;
            }

            var operationResultTemp = NewTemp();

            // Использование таблицы для операций присваивания
            if (_assignOpTable.TryGetValue((varType, ass.Op), out var operationCommand))
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateBinary(operationCommand,
                    currentValueTemp, exprResultTemp,
                    operationResultTemp));
            else
                throw new InvalidOperationException(
                    $"Unsupported assignment operation '{ass.Op}' for type {varType}");

            var storeCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.CreateAssign(storeCommand, varAddress, operationResultTemp));
        
    }

    public void VisitVarAssign(VarAssignNode ass)
    {
      
            var varAddress = _currentNameSpace.Variables.Count(x=>x.Value.VariableAddress != -1);
            _currentNameSpace.Variables[ass.Ident.Name].VariableAddress = varAddress;
            var varType = TypeChecker.CalcType(ass.Ident, _currentNameSpace);
             // Оптимизация для констант
            if (ass.Expr is IntNode intNode)
            {
                if (varType == SemanticType.DoubleType)
                    // int -> double: создаем double константу напрямую
                    _function_codes[_currentGeneratingFunctionName.Peek()]
                        .Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, (double)intNode.Val, true));
                else
                    _function_codes[_currentGeneratingFunctionName.Peek()]
                        .Add(ThreeAddr.CreateConst(Commands.icass, varAddress, intNode.Val, true));
            }
            else if (ass.Expr is DoubleNode doubleNode)
            {
                _function_codes[_currentGeneratingFunctionName.Peek()]
                    .Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, doubleNode.Val, true));
            }
            else
            {
                ass.Expr.VisitP(this);
                var exprType = TypeChecker.CalcType(ass.Expr, _currentNameSpace);
                var exprResultTemp = _tempCounter - 1;
                varAddress = GetVariableAddress(ass.Ident.Name);
                if (varType == exprType)
                {
                    var command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
                    _function_codes[_currentGeneratingFunctionName.Peek()]
                        .Add(ThreeAddr.CreateAssign(command, varAddress, exprResultTemp, true, true));
                }
                else
                {
                    if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                    {
                        var convertedTemp = NewTemp();
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(
                            ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp, true, true));
                        _function_codes[_currentGeneratingFunctionName.Peek()]
                            .Add(ThreeAddr.CreateAssign(Commands.rass, varAddress, convertedTemp, true, true));
                    }
                    else
                    {
                        throw new CompilerExceptions.UnExpectedException(
                            "Something went wrong! During generation code for assignment!");
                    }
                }
            }
          
        
    }


    public void VisitIf(IfNode ifn)
    {
        var elseLabel = NewLabel();
        var endLabel = NewLabel();

        ifn.Condition.VisitP(this);
        var condTemp = _tempCounter - 1;

        if (_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
            Code.Add(ThreeAddr.Create(Commands.ifn, condTemp, elseLabel));
            ifn.ThenStat.VisitP(this);
            Code.Add(ThreeAddr.Create(Commands.go, endLabel));

            Code.Add(ThreeAddr.Create(Commands.label, elseLabel));
            ifn.ElseStat?.VisitP(this);

            Code.Add(ThreeAddr.Create(Commands.label, endLabel));
        }
        else
        {
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.ifn, condTemp, elseLabel, true));
            ifn.ThenStat.VisitP(this);
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.go, endLabel));

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, elseLabel));
            ifn.ElseStat?.VisitP(this);

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, endLabel));
        }
    }

    public void VisitWhile(WhileNode whn)
    {
        var startLabel = NewLabel();
        var endLabel = NewLabel();

        Code.Add(ThreeAddr.Create(Commands.label, startLabel));

        whn.Condition.VisitP(this);
        var condTemp = _tempCounter - 1;

        Code.Add(ThreeAddr.Create(Commands.ifn, condTemp, endLabel));
        whn.Stat.VisitP(this);
        Code.Add(ThreeAddr.Create(Commands.go, startLabel));

        Code.Add(ThreeAddr.Create(Commands.label, endLabel));
    }

    public void VisitFor(ForNode forNode)
    {
        var startLabel = NewLabel();
        var endLabel = NewLabel();

      
            forNode.Counter.VisitP(this);

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, startLabel));

            forNode.Condition.VisitP(this);
            var condTemp = _tempCounter - 1;
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.ifn, condTemp, endLabel));

            forNode.Stat.VisitP(this);
            forNode.Increment.VisitP(this);

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.go, startLabel));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, endLabel));
        
    }


    public void VisitProcCall(ProcCallNode p)
    {
        // Нужно найти правильную специализацию
        var argTypes = new List<SemanticType>();
        var paramindex = 0;
        foreach (var param in p.Pars.lst)
        {
            param.VisitP(this);
            argTypes.Add(TypeChecker.CalcType(param, _currentNameSpace));

          
                switch (TypeChecker.CalcType(param, _currentNameSpace))
                {
                    case SemanticType.IntType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.iass,
                            _frameSizes[_currentGeneratingFunctionName.Peek()] + paramindex++, _tempCounter - 1, true,
                            true));
                        break;
                    case SemanticType.DoubleType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.rass,
                            _frameSizes[_currentGeneratingFunctionName.Peek()] + paramindex++, _tempCounter - 1, true,
                            true));
                        break;
                    case SemanticType.BoolType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.bass,
                            _frameSizes[_currentGeneratingFunctionName.Peek()] + paramindex++, _tempCounter - 1, true,
                            true));
                        break;
                }
        }


        // Затем вызываем функцию
        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.call, p.Name.Name));
    }

    public void VisitFuncCall(FuncCallNode f)
    {
        // Нужно найти правильную специализацию
        var argTypes = new List<SemanticType>();
        var paramindex = 0;
        foreach (var param in f.Pars.lst)
        {
            param.VisitP(this);
            argTypes.Add(TypeChecker.CalcType(param, _currentNameSpace));
                switch (TypeChecker.CalcType(param, _currentNameSpace))
                {
                    case SemanticType.IntType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.iass,
                            _frameSizes[_currentGeneratingFunctionName.Peek()] + paramindex++, _tempCounter - 1, true,
                            true));
                        break;
                    case SemanticType.DoubleType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.rass,
                            _frameSizes[_currentGeneratingFunctionName.Peek()] + paramindex++, _tempCounter - 1, true,
                            true));
                        break;
                    case SemanticType.BoolType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.bass,
                            _frameSizes[_currentGeneratingFunctionName.Peek()] + paramindex++, _tempCounter - 1, true,
                            true));
                        break;
                }
        }

        // Найти специализацию по типам аргументов
        var specialization = SymbolTree.FunctionTable[f.Name.Name].Specializations
            .Find(x => x.SpecializationId == f.SpecializationId);


        //Обработка тела функции
        if (!_currentGeneratingFunctionName.Contains(f.Name.Name + f.SpecializationId) &&
            !_alreadyGeneratedFunctionDefinitions.Contains(f.Name.Name + f.SpecializationId))
        {
            _currentTempIndexes[_currentGeneratingFunctionName.Peek()] = _tempCounter;

            _tempCounter = _currentGeneratingFunctionSpecialization.Peek().NameSpace.Variables.Count;

            _currentGeneratingFunctionName.Push(f.Name.Name + f.SpecializationId);
            if (!_function_codes.ContainsKey(f.Name.Name + f.SpecializationId))
                _function_codes[f.Name.Name + f.SpecializationId] = new List<ThreeAddr>();

            

            _currentGeneratingFunctionSpecialization.Push(SymbolTree.FunctionTable[f.Name.Name].Specializations
                .Find(x => x.SpecializationId == f.SpecializationId));

            int i = 0;
            foreach (var x in  specialization.NameSpace.Variables)
            {
                x.Value.VariableAddress = i++;
            }
            
            SymbolTree.FunctionTable[f.Name.Name].Definition.VisitP(this);


            _tempCounter = _currentTempIndexes[_currentGeneratingFunctionName.Peek()];
        }

        // Затем вызываем функцию и сохраняем результат
        var resultTemp = NewTemp();
       
        _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.call, f.Name.Name + f.SpecializationId));
        _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.movout, resultTemp, true));
        
    }

    public void VisitFuncDef(FuncDefNode f)
    {
        _function_codes[_currentGeneratingFunctionName.Peek()]
            .Add(ThreeAddr.Create(Commands.label, _currentGeneratingFunctionName.Peek()));
        f.Body.VisitP(this);
        _currentGeneratingFunctionSpecialization.Pop();
        _currentTempIndexes[_currentGeneratingFunctionName.Peek()] = _tempCounter;
        _alreadyGeneratedFunctionDefinitions.Add(_currentGeneratingFunctionName.Pop());
    }

    public void VisitDefinitionsAndStatements(DefinitionsAndStatements DefandStmts)
    {
        DefandStmts.DefinitionsList.VisitP(this);
        DefandStmts.MainProgram.VisitP(this);
    }

    public void VisitDefinitionsList(DefinitionsListNode defList)
    {
        foreach (var x in defList.lst) 
            if(x is not FuncDefNode)
                x.VisitP(this);
    }

    public void VisitVariableDeclarationNode(VariableDeclarationNode vardecl)
    {
        var varAddress = _globalVariablesCounter++;
        SymbolTree.Global.Variables[vardecl.vass.Ident.Name].VariableAddress = varAddress;
         var varType = TypeChecker.CalcType(vardecl.vass.Ident, _currentNameSpace);
             // Оптимизация для констант
            if (vardecl.vass.Expr is IntNode intNode)
            {
                if (varType == SemanticType.DoubleType)
                    // int -> double: создаем double константу напрямую
                    Code
                        .Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, (double)intNode.Val));
                else
                    Code
                        .Add(ThreeAddr.CreateConst(Commands.icass, varAddress, intNode.Val));
            }
            else if (vardecl.vass.Expr is DoubleNode doubleNode)
            {
                Code
                    .Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, doubleNode.Val));
            }
            else
            {
                vardecl.vass.Expr.VisitP(this);
                var exprType = TypeChecker.CalcType(vardecl.vass.Expr, _currentNameSpace);
                var exprResultTemp = _tempCounter - 1;
                varAddress = GetVariableAddress(vardecl.vass.Ident.Name);
                if (varType == exprType)
                {
                    var command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
                    Code
                        .Add(ThreeAddr.CreateAssign(command, varAddress, exprResultTemp));
                }
                else
                {
                    if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                    {
                        var convertedTemp = NewTemp();
                        Code.Add(
                            ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp));
                        Code
                            .Add(ThreeAddr.CreateAssign(Commands.rass, varAddress, convertedTemp));
                    }
                    else
                    {
                        throw new CompilerExceptions.UnExpectedException(
                            "Something went wrong! During generation code for assignment!");
                    }
                }
            }
    }


    public void VisitReturn(ReturnNode r)
    {
        r.Expr.VisitP(this);

        var returnType = TypeChecker.CalcType(r.Expr, _currentNameSpace);

        if (returnType == SemanticType.IntType &&
            _currentGeneratingFunctionSpecialization.Peek().ReturnType == SemanticType.DoubleType)
        {
            var convertedTemp = NewTemp();
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.CreateConvert(Commands.citr, _tempCounter - 2, convertedTemp, true, true));
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.movin, convertedTemp, true));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.creturn));
            return;
        }

        if (returnType == _currentGeneratingFunctionSpecialization.Peek().ReturnType)
        {
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.movin, _tempCounter - 1, true));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.creturn));
        }
        else
        {
            throw new CompilerExceptions.UnExpectedException(
                "Something went wrong during generation code for return in function:" +
                _currentGeneratingFunctionName.Peek() + returnType +
                _currentGeneratingFunctionSpecialization.Peek().ReturnType);
        }
    }

    public void GiveFrameSizes(Dictionary<string, int> frameSizes)
    {
        _frameSizes = frameSizes;
    }

    public Dictionary<string, int> GetFrameSizes()
    {
        return _frameSizes;
    }

    public List<ThreeAddr> GetCode()
    {
        Code.AddRange(_function_codes["MainFrame"]);
        Code.Add(ThreeAddr.Create(Commands.stop));
        foreach (var x in _function_codes) 
            if(x.Key!="MainFrame")
                Code.AddRange(x.Value);

        _currentTempIndexes[_currentGeneratingFunctionName.Peek()] = _tempCounter;
        var tmp = _currentGeneratingFunctionName.Pop();
        if (tmp != "MainFrame")
            throw new CompilerExceptions.UnExpectedException($"The last func is {tmp}, but expected 'MainFrame'.");
        ResolveLabels();
        return Code;
    }

    private int NewTemp()
    {
        return _tempCounter++;
    }

    private string NewLabel()
    {
        return $"L{_labelCounter++}";
    }

    private int GetVariableAddress(string varName)
    {

        var temp = _currentNameSpace.LookupVariable(varName);
        if (temp != null)
        {
            if(temp.VariableAddress!=-1)
                return temp.VariableAddress;
            else
            {
                throw new CompilerExceptions.UnExpectedException($"Variable {varName} has no definition!");
            }
        }
        else
        {
            throw new CompilerExceptions.UnExpectedException("Something went wrong during bulding namespaceTree!");
        }
      
    }


    private void ResolveLabels()
    {
        LabelAddresses.Clear();
        for (var i = 0; i < Code.Count; i++)
            if (Code[i].command == Commands.label)
                LabelAddresses[Code[i].Label] = i;
    }
}