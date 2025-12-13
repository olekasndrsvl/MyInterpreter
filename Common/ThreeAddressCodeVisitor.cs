using System.Security;
using MyInterpreter.SemanticCheck;

namespace MyInterpreter.Common;

public class ThreeAddressCodeVisitor : IVisitorP
{
    private List<ThreeAddr> _code = new List<ThreeAddr>();
    private Dictionary<string,List<ThreeAddr>> _function_codes = new Dictionary<string, List<ThreeAddr>>();
    private int _tempCounter;
    private int _labelCounter = 0;
    private Dictionary<string, int> _labelAddresses = new Dictionary<string, int>();
    //private Dictionary<string, int> _variableAddresses = new Dictionary<string, int>();
    private int _nextVarAddress = 0;
    
    public Dictionary<string,int> _currentTempIndexes = new Dictionary<string, int>(){};
    private Dictionary<string, int> _frameSizes = new Dictionary<string, int>(){{"MainFrame",40}, {"factorial0",20}, {"factorial1",20}};
    private Dictionary<string, Dictionary<string, int>> _variableAddresses =new Dictionary<string, Dictionary<string, int>>();
    
    private Stack<string> _currentGeneratingFunctionName = new Stack<string>();
    private Stack<FunctionSpecialization> _currentGeneratingFunctionSpecialization = new Stack<FunctionSpecialization>();
    private List<string> _alreadyGeneratedFunctionDefinitions = new List<string>();

    public ThreeAddressCodeVisitor()
    {
        _currentGeneratingFunctionName.Push("MainFrame");
        _variableAddresses.Add("MainFrame", new Dictionary<string, int>());
        _currentGeneratingFunctionSpecialization.Push(SymbolTable.FunctionTable["Main"].Specializations.First());
        _tempCounter = SymbolTable.FunctionTable["Main"].Specializations.First().LocalVariableTypes.Count(x => x.Value.Kind == KindType.VarName);
    }

    public void GiveFrameSizes(Dictionary<string, int> frameSizes)
    {
        _frameSizes= frameSizes;
    }
    
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

    public Dictionary<string, int> GetFrameSizes()
    {
        return _frameSizes;
    }
    public List<ThreeAddr> GetCode()
    {
        _code.Add(ThreeAddr.Create(Commands.stop));
        foreach (var x in _function_codes)
        {
            _code.AddRange(x.Value);
        }
       
        _currentTempIndexes[_currentGeneratingFunctionName.Peek()] = _tempCounter;
        var tmp =_currentGeneratingFunctionName.Pop();
        if (tmp != "MainFrame")
        {
            throw new CompilerExceptions.UnExpectedException($"The last func is {tmp}, but expected 'MainFrame'.");
        }
        ResolveLabels();
        return _code;
    }
    
    public Dictionary<string, int> LabelAddresses => _labelAddresses;

    private int NewTemp() => _tempCounter++;
    private string NewLabel() => $"L{_labelCounter++}";
    
    private int GetVariableAddress(string varName)
    {
        var funcName = _currentGeneratingFunctionName.Peek();
        
        if (!_variableAddresses[funcName].ContainsKey(varName))
        {
            if(_variableAddresses[funcName].Count>0)
            {
                _variableAddresses[funcName][varName] = _variableAddresses[funcName].MaxBy(x => x.Value).Value +1;
            }
            else
            {
                _variableAddresses[funcName][varName] = 0; //_currentGeneratingFunctionSpecialization.Peek().ParameterTypes.Length;
            }
        }
        
        return _variableAddresses[funcName][varName];
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
        var leftType = TypeChecker.CalcType(bin.Left, _currentGeneratingFunctionSpecialization.Peek());
        var rightType = TypeChecker.CalcType(bin.Right,_currentGeneratingFunctionSpecialization.Peek());
        
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
          

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
        else
        {
            // Автоматическое преобразование типов при необходимости
            if (leftType == SemanticType.IntType && rightType == SemanticType.DoubleType)
            {
                // Convert left int to double
                int convertedTemp = NewTemp();
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConvert(Commands.citr, leftTemp, convertedTemp,true,true));
                leftTemp = convertedTemp;
                leftType = SemanticType.DoubleType;
            }
            else if (leftType == SemanticType.DoubleType && rightType == SemanticType.IntType)
            {
                // Convert right int to double
                int convertedTemp = NewTemp();
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConvert(Commands.citr, rightTemp, convertedTemp,true,true));
                rightTemp = convertedTemp;
                rightType = SemanticType.DoubleType;
            }

            int resultTemp = NewTemp();
            // Поиск команды в таблице
            if (_binOpTable.TryGetValue((leftType, rightType, bin.Op), out Commands command))
            {
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateBinary(command, leftTemp, rightTemp, resultTemp,true,true,true));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported binary operation {bin.Op} for types {leftType} and {rightType}");
            }
        }
        
        
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
        int tempIndex = NewTemp();
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
            _code.Add(ThreeAddr.CreateConst(Commands.icass, tempIndex, n.Val));
        }
        else
        {
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConst(Commands.icass, tempIndex, n.Val,true));
        }
    }

    public void VisitDouble(DoubleNode d)
    { 
        int tempIndex = NewTemp();
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
            _code.Add(ThreeAddr.CreateConst(Commands.rcass, tempIndex, d.Val));
        }
        else
        {
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConst(Commands.rcass, tempIndex, d.Val,true));
        }
    }

    public void VisitId(IdNode id)
    {
        int tempIndex = NewTemp();
        int varAddress = GetVariableAddress(id.Name);
        var varType = TypeChecker.CalcType(id,_currentGeneratingFunctionSpecialization.Peek());
        
        Commands command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
            _code.Add(ThreeAddr.CreateAssign(command, tempIndex, varAddress));
        else
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(command, tempIndex, varAddress,true,true));
    }

    public void VisitAssign(AssignNode ass)
    {
        int varAddress = GetVariableAddress(ass.Ident.Name);
        var varType = TypeChecker.CalcType(ass.Ident,_currentGeneratingFunctionSpecialization.Peek());
        
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
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
                _code.Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, doubleNode.Val));
            }
            else
            {
                ass.Expr.VisitP(this);
                var exprType = TypeChecker.CalcType(ass.Expr,_currentGeneratingFunctionSpecialization.Peek());
                int exprResultTemp = _tempCounter - 1;
                varAddress = GetVariableAddress(ass.Ident.Name);
                if(varType== exprType)
                {
                    Commands command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
                    _code.Add(ThreeAddr.CreateAssign(command, varAddress, exprResultTemp));
                }
                else
                {
                    if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                    {
                        
                        int convertedTemp = NewTemp();
                        _code.Add(ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp));
                        _code.Add(ThreeAddr.CreateAssign( Commands.rass, varAddress, convertedTemp));
                    }
                    else
                    {
                        throw new CompilerExceptions.UnExpectedException(
                            "Something went wrong! During generation code for assignment!");
                    }
                }
            }
        }
        else
        {
            // Оптимизация для констант
            if (ass.Expr is IntNode intNode)
            {
                if (varType == SemanticType.DoubleType)
                {
                    // int -> double: создаем double константу напрямую
                    _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, (double)intNode.Val,true));
                }
                else
                {
                    _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConst(Commands.icass, varAddress, intNode.Val,true));
                }
            }
            else if (ass.Expr is DoubleNode doubleNode)
            {
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConst(Commands.rcass, varAddress, doubleNode.Val,true));
            }
            else
            {
                
                ass.Expr.VisitP(this);
                var exprType = TypeChecker.CalcType(ass.Expr,_currentGeneratingFunctionSpecialization.Peek());
                int exprResultTemp = _tempCounter - 1;
                varAddress = GetVariableAddress(ass.Ident.Name);
                if(varType== exprType)
                {
                    Commands command = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
                    _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(command, varAddress, exprResultTemp,true,true));
                }
                else
                {
                    if (exprType == SemanticType.IntType && varType == SemanticType.DoubleType)
                    {
                        
                        int convertedTemp = NewTemp();
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp,true,true));
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign( Commands.rass, varAddress, convertedTemp,true,true));
                    }
                    else
                    {
                        throw new CompilerExceptions.UnExpectedException(
                            "Something went wrong! During generation code for assignment!");
                    }
                }
            }
        }
    }

    public void VisitAssignOp(AssignOpNode ass)
    {
        int varAddress = GetVariableAddress(ass.Ident.Name);
        var varType = TypeChecker.CalcType(ass.Ident,_currentGeneratingFunctionSpecialization.Peek());
        
        int currentValueTemp = NewTemp();
        Commands loadCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
            _code.Add(ThreeAddr.CreateAssign(loadCommand, currentValueTemp, varAddress));

            ass.Expr.VisitP(this);
            int exprResultTemp = _tempCounter - 1;
            var exprType = TypeChecker.CalcType(ass.Expr,_currentGeneratingFunctionSpecialization.Peek());

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
                _code.Add(ThreeAddr.CreateBinary(operationCommand, currentValueTemp, exprResultTemp,
                    operationResultTemp));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported assignment operation '{ass.Op}' for type {varType}");
            }

            Commands storeCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
            _code.Add(ThreeAddr.CreateAssign(storeCommand, varAddress, operationResultTemp));
        }
        else
        {
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(loadCommand, currentValueTemp, varAddress));

            ass.Expr.VisitP(this);
            int exprResultTemp = _tempCounter - 1;
            var exprType = TypeChecker.CalcType(ass.Expr,_currentGeneratingFunctionSpecialization.Peek());

            // Handle type conversion if needed
            if (varType == SemanticType.DoubleType && exprType == SemanticType.IntType)
            {
                int convertedTemp = NewTemp();
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConvert(Commands.citr, exprResultTemp, convertedTemp));
                exprResultTemp = convertedTemp;
            }

            int operationResultTemp = NewTemp();

            // Использование таблицы для операций присваивания
            if (_assignOpTable.TryGetValue((varType, ass.Op), out Commands operationCommand))
            {
                _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateBinary(operationCommand, currentValueTemp, exprResultTemp,
                    operationResultTemp));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported assignment operation '{ass.Op}' for type {varType}");
            }

            Commands storeCommand = varType == SemanticType.DoubleType ? Commands.rass : Commands.iass;
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(storeCommand, varAddress, operationResultTemp));
        }
    }

    public void VisitIf(IfNode ifn)
    {
        string elseLabel = NewLabel();
        string endLabel = NewLabel();
        
        ifn.Condition.VisitP(this);
        int condTemp = _tempCounter - 1;
        
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
            _code.Add(ThreeAddr.Create(Commands.ifn, condTemp, elseLabel));
            ifn.ThenStat.VisitP(this);
            _code.Add(ThreeAddr.Create(Commands.go, endLabel));

            _code.Add(ThreeAddr.Create(Commands.label, elseLabel));
            ifn.ElseStat?.VisitP(this);

            _code.Add(ThreeAddr.Create(Commands.label, endLabel));
        }
        else
        {
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.ifn, condTemp, elseLabel,true));
            ifn.ThenStat.VisitP(this);
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.go, endLabel));

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, elseLabel));
            ifn.ElseStat?.VisitP(this);
            
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, endLabel));
        }
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
        
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
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
        else
        {
            forNode.Counter.VisitP(this);

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, startLabel));

            forNode.Condition.VisitP(this);
            int condTemp = _tempCounter - 1;
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.ifn, condTemp, endLabel));

            forNode.Stat.VisitP(this);
            forNode.Increment.VisitP(this);

            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.go, startLabel));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, endLabel));
        }
    }

   
    public void VisitProcCall(ProcCallNode p)
    {
        // Нужно найти правильную специализацию
        var argTypes = new List<SemanticType>();
        int paramindex = 0;
        foreach (var param in p.Pars.lst)
        {
            param.VisitP(this);
            argTypes.Add(TypeChecker.CalcType(param,_currentGeneratingFunctionSpecialization.Peek()));

            if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
            {
                switch (TypeChecker.CalcType(param,_currentGeneratingFunctionSpecialization.Peek()))
                {
                    case SemanticType.IntType:
                        _code.Add(ThreeAddr.CreateAssign(Commands.iass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1));
                        break;
                    case SemanticType.DoubleType:
                        _code.Add(ThreeAddr.CreateAssign(Commands.rass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1));
                        break;
                    case SemanticType.BoolType:
                        _code.Add(ThreeAddr.CreateAssign(Commands.bass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1));
                        break;
                }
            }
            else
            {
                switch (TypeChecker.CalcType(param,_currentGeneratingFunctionSpecialization.Peek()))
                {
                    case SemanticType.IntType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.iass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1,true,true));
                        break;
                    case SemanticType.DoubleType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.rass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1,true,true));
                        break;
                    case SemanticType.BoolType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.bass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1,true,true));
                        break;
                }
            }
        }
    
        
    
    
        // Затем вызываем функцию
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
            _code.Add(ThreeAddr.Create(Commands.call, p.Name.Name));
        else
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.call, p.Name.Name));
        
    }
    public void VisitFuncCall(FuncCallNode f)
    {
        
        // Нужно найти правильную специализацию
        var argTypes = new List<SemanticType>();
        int paramindex = 0;
        foreach (var param in f.Pars.lst)
        {
            param.VisitP(this);
            argTypes.Add(TypeChecker.CalcType(param,_currentGeneratingFunctionSpecialization.Peek()));
            
            if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
            {
                switch (TypeChecker.CalcType(param,_currentGeneratingFunctionSpecialization.Peek()))
                {
                    case SemanticType.IntType:
                        _code.Add(ThreeAddr.CreateAssign(Commands.iass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1));
                        break;
                    case SemanticType.DoubleType:
                        _code.Add(ThreeAddr.CreateAssign(Commands.rass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1));
                        break;
                    case SemanticType.BoolType:
                        _code.Add(ThreeAddr.CreateAssign(Commands.bass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1));
                        break;
                }
            }
            else
            {
                switch (TypeChecker.CalcType(param,_currentGeneratingFunctionSpecialization.Peek()))
                {
                    case SemanticType.IntType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.iass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1,true,true));
                        break;
                    case SemanticType.DoubleType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.rass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1,true,true));
                        break;
                    case SemanticType.BoolType:
                        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateAssign(Commands.bass, _frameSizes[_currentGeneratingFunctionName.Peek()]+(paramindex++),_tempCounter-1,true,true));
                        break;
                }
            }
        }
    
        // Найти специализацию по типам аргументов
        var specialization = SymbolTable.FunctionTable[f.Name.Name].Specializations
            .Find(x => x.SpecializationId == f.SpecializationId);
        
        
        //Обработка тела функции
        if (!_currentGeneratingFunctionName.Contains(f.Name.Name+f.SpecializationId) && !_alreadyGeneratedFunctionDefinitions.Contains(f.Name.Name+f.SpecializationId))
        {
            _currentTempIndexes[_currentGeneratingFunctionName.Peek()] = _tempCounter;
            
            _tempCounter=_currentGeneratingFunctionSpecialization.Peek().LocalVariableTypes.Count;
            
            _currentGeneratingFunctionName.Push(f.Name.Name+f.SpecializationId);
            if(!_function_codes.ContainsKey(f.Name.Name+f.SpecializationId))
                _function_codes[f.Name.Name+f.SpecializationId] = new List<ThreeAddr>();
            
            _variableAddresses.Add(f.Name.Name+f.SpecializationId, new Dictionary<string, int>());
            
            _currentGeneratingFunctionSpecialization.Push(SymbolTable.FunctionTable[f.Name.Name].Specializations.Find(x=>x.SpecializationId==f.SpecializationId));
            
            for (int i = 0; i < SymbolTable.FunctionTable[f.Name.Name].Definition.Params.Count; i++)
            {
                _variableAddresses[f.Name.Name+f.SpecializationId][SymbolTable.FunctionTable[f.Name.Name].Definition.Params[i].Name]= i;
            }
            
            SymbolTable.FunctionTable[f.Name.Name].Definition.VisitP(this);
            
            
            _tempCounter = _currentTempIndexes[_currentGeneratingFunctionName.Peek()];
        } 
      
        // Затем вызываем функцию и сохраняем результат
        int resultTemp = NewTemp();
        if(_currentGeneratingFunctionName.Peek().Equals("MainFrame"))
        {
            _code.Add(ThreeAddr.Create(Commands.call, f.Name.Name+f.SpecializationId));
            _code.Add(ThreeAddr.Create(Commands.movout, resultTemp));
        }
        else
        {
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.call, f.Name.Name+f.SpecializationId));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.movout, resultTemp,true));
        }
    
        
    }

    public void VisitFuncDef(FuncDefNode f)
    {
        _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.label, _currentGeneratingFunctionName.Peek()));
        f.Body.VisitP(this);
        _currentGeneratingFunctionSpecialization.Pop();
        _currentTempIndexes[_currentGeneratingFunctionName.Peek()] = _tempCounter;
        _alreadyGeneratedFunctionDefinitions.Add(_currentGeneratingFunctionName.Pop());
    }

    public void VisitFuncDefList(FuncDefListNode lst)
    {
        foreach (var VARIABLE in lst.lst) 
        {
            VARIABLE.VisitP(this);
        }
    }

    public void VisitFunDefAndStatements(FuncDefAndStatements fdandStmts)
    {
       // fdandStmts.FuncDefList.VisitP(this);
        fdandStmts.StatementList.VisitP(this);
    }

    public void VisitReturn(ReturnNode r)
    {
        r.Expr.VisitP(this);

        SemanticType returnType = TypeChecker.CalcType(r.Expr, _currentGeneratingFunctionSpecialization.Peek());

        if (returnType == SemanticType.IntType &&
            _currentGeneratingFunctionSpecialization.Peek().ReturnType == SemanticType.DoubleType)
        {
            int convertedTemp = NewTemp();
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.CreateConvert(Commands.citr, _tempCounter-2, convertedTemp,true,true));
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.movin, convertedTemp, true));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.creturn));
            return;
        }
        
        if(returnType ==  _currentGeneratingFunctionSpecialization.Peek().ReturnType)
        {
            _function_codes[_currentGeneratingFunctionName.Peek()]
                .Add(ThreeAddr.Create(Commands.movin, _tempCounter - 1, true));
            _function_codes[_currentGeneratingFunctionName.Peek()].Add(ThreeAddr.Create(Commands.creturn));
        }
        else
        {
            throw new CompilerExceptions.UnExpectedException("Something went wrong during generation code for return in function:"+_currentGeneratingFunctionName.Peek()+ returnType+  _currentGeneratingFunctionSpecialization.Peek().ReturnType );
        }
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