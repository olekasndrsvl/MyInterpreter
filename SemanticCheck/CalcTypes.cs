using MyInterpreter;
using MyInterpreter.SemanticCheck;

namespace MyInterpreter;
using static SymbolTree;

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
    
    public static SemanticType CalcTypeVis(ExprNode ex, NameSpace context)
    { 
        return ex.Visit(new CalcTypeVisitor(context));
    }

    public static SemanticType CalcTypeVis(ExprNode ex, NameSpace context,
        Func<string, SemanticType[], Position, SemanticType> functionReturnTypeResolver)
    {
        return ex.Visit(new CalcTypeVisitor(context, functionReturnTypeResolver));
    }

    // Альтернативная реализация без визитора с использованием pattern matching
    public static SemanticType CalcType(ExprNode ex, NameSpace context)
    {
        switch (ex)
        {
            case FuncCallNode funccall:
                return CalcTypeVis(funccall, context);
            
            case IdNode id:
                if (context.LookupVariable(id.Name)!= null)
                    return context.LookupVariable(id.Name).Type;
                else
                    return SemanticType.BadType;
        
            case IntNode i:
                return SemanticType.IntType;
        
            case DoubleNode d:
                return SemanticType.DoubleType;
        
            case BinOpNode bin:
                var lt = CalcType(bin.Left, context);
                var rt = CalcType(bin.Right, context);
            
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
    private readonly NameSpace _context;
    private readonly Func<string, SemanticType[], Position, SemanticType> _functionReturnTypeResolver;

    public CalcTypeVisitor(NameSpace context,
        Func<string, SemanticType[], Position, SemanticType> functionReturnTypeResolver = null)
    {
        _context = context;
        _functionReturnTypeResolver = functionReturnTypeResolver;
    }

    public SemanticType CalcTypeVis(ExprNode ex) => ex.Visit(this);
    
    public SemanticType VisitNode(Node bin) => SemanticType.NoType;
    public SemanticType VisitTypeNode(TypeNode tpn) => SemanticType.NoType;
    public SemanticType VisitIntTypeNode(IntTypeNode itn) => itn.Type;

    public SemanticType VisitBoolTypeNode(BoolTypeNode btn) => btn.Type;

    public SemanticType VisitDoubleTypeNode(DoubleTypeNode dtn) => dtn.Type;
    public SemanticType VisitDefinitionNode(DefinitionNode def) => SemanticType.NoType;
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
            // Особые случаи для UnknownType
            if (lt == SemanticType.UnknownType || rt == SemanticType.UnknownType)
            { 
                return lt == SemanticType.UnknownType ? rt : lt;
            }
            
            
            // Особые случаи для AnyType
            if (lt == SemanticType.AnyType || rt == SemanticType.AnyType)
            { 
                return SemanticType.AnyType;
            }
            else if (bin.Op == TokenType.Divide)
            {
                return SemanticType.DoubleType;
            }
            else if (lt == rt)
            {
                return lt;
            }
            else 
            {
                // IntType и DoubleType -> DoubleType
                return SemanticType.DoubleType;
            }
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
    public SemanticType VisitBlockNode(BlockNode bin) => SemanticType.NoType;
   

    public SemanticType VisitExprList(ExprListNode exlist) => SemanticType.NoType;
    
    public SemanticType VisitInt(IntNode n) => SemanticType.IntType;
    
    public SemanticType VisitDouble(DoubleNode d) => SemanticType.DoubleType;
    
    public SemanticType VisitId(IdNode id)
    {
        var variable = _context.LookupVariable(id.Name);
        if (variable == null)
             CompilerExceptions.SemanticError("Идентификатор " + id.Name + " не определен", id.Pos);
        else 
            return variable.Type;
        
        return SemanticType.BadType;
    }
    
    public SemanticType VisitAssign(AssignNode ass) => SemanticType.NoType;
    public SemanticType VisitVarAssign(VarAssignNode ass)  => SemanticType.NoType;
    public SemanticType VisitAssignOp(AssignOpNode ass) => SemanticType.NoType;
    
    public SemanticType VisitIf(IfNode ifn) => SemanticType.NoType;
    
    public SemanticType VisitWhile(WhileNode whn) => SemanticType.NoType;
    
    public SemanticType VisitFor(ForNode forNode) => SemanticType.NoType;
    
    public SemanticType VisitProcCall(ProcCallNode f)
    {
        // Вычисляем типы аргументов
        var argTypes = new List<SemanticType>();
        foreach (var arg in f.Pars.lst)
        {
            argTypes.Add(CalcTypeVis(arg));
        }
        var spec = ResolveCallSpecialization(f.Name.Name, argTypes.ToArray(), f.Name.Pos);

        if (spec.ReturnType != SemanticType.NoType)
             CompilerExceptions.SemanticError("Попытка вызвать функцию " + f.Name.Name + " как процедуру", f.Name.Pos);
        
        for (int i = 0; i < f.Pars.lst.Count; i++)
        {
            var tp = CalcTypeVis(f.Pars.lst[i]);
            if (i < spec.ParameterTypes.Length && !TypeChecker.AssignComparable(spec.ParameterTypes[i], tp))
                CompilerExceptions.SemanticError("Тип аргумента процедуры " + tp.ToString() + " не соответствует типу формального параметра " + spec.ParameterTypes[i].ToString(), f.Name.Pos);
        }
        
        return SemanticType.NoType;
    }
    
    public SemanticType VisitFuncCall(FuncCallNode f)
    {
        // Вычисляем типы аргументов
        var argTypes = new List<SemanticType>();
        foreach (var arg in f.Pars.lst)
        {
            argTypes.Add(CalcTypeVis(arg));
        }
        var specialization = ResolveCallSpecialization(f.Name.Name, argTypes.ToArray(), f.Name.Pos);

        // Проверяем совместимость типов аргументов
        for (int i = 0; i < specialization.ParameterTypes.Length; i++)
        {
            var argType = argTypes[i];
            var paramType = specialization.ParameterTypes[i];
            
            if (!TypeChecker.AssignComparable(paramType, argType))
                CompilerExceptions.SemanticError($"Тип аргумента функции {argType} не соответствует типу формального параметра {paramType}", f.Name.Pos);
        }
        if (_functionReturnTypeResolver != null)
            return _functionReturnTypeResolver(f.Name.Name, argTypes.ToArray(), f.Name.Pos);

        return specialization.ReturnType;
    }

    public SemanticType VisitFuncDef(FuncDefNode f) => SemanticType.NoType;
    public SemanticType VisitReturn(ReturnNode r) => SemanticType.NoType;
    public SemanticType VisitDefinitionsAndStatements(DefinitionsAndStatements fdandStmts) => SemanticType.NoType;

    public SemanticType VisitVariableDeclarationNode(VariableDeclarationNode varDecl) => SemanticType.NoType;

    public SemanticType VisitDefinitionsList(DefinitionsListNode defList) => SemanticType.NoType;


}
