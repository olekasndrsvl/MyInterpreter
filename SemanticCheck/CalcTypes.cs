using MyInterpreter;

namespace MyInterpreter;
using static MyInterpreter.SymbolTable;

public static class TypeCalculator
{
    // Проверка совместимости типов при присваивании
  

  
}
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
            
                if ( Constants.ArithmeticOperations.Contains(bin.Op))
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
        
        if ( Constants.ArithmeticOperations.Contains(bin.Op))
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
        
        return SemanticType.NoType;
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
        
        return SemanticType.NoType;
    }
    
    public SemanticType VisitAssign(AssignNode ass) => SemanticType.NoType;
    
    public SemanticType VisitAssignOp(AssignOpNode ass) => SemanticType.NoType;
    
    public SemanticType VisitIf(IfNode ifn) => SemanticType.NoType;
    
    public SemanticType VisitWhile(WhileNode whn) => SemanticType.NoType;
    public SemanticType VisitFor(ForNode forNode)=> SemanticType.NoType;
    
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
        // Думаю, вычисление типа функции надо совмещать с проверкой всех её аргументов
        if (!SymbolTable.SymTable.ContainsKey(f.Name.Name))
            CompilerExceptions.SemanticError("Функция с именем " + f.Name.Name + " не определена", f.Name.Pos);
        
        var sym = SymbolTable.SymTable[f.Name.Name];
        if (sym.Kind != KindType.FuncName)
            CompilerExceptions.SemanticError("Данное имя " + f.Name.Name + " не является именем функции", f.Name.Pos);
        
        if (sym.Type == SemanticType.NoType) // Это процедура
            CompilerExceptions.SemanticError("Попытка вызвать процедуру " + f.Name.Name + " как функцию", f.Name.Pos);
        
        if (sym.Params.Count() != f.Pars.lst.Count)
            CompilerExceptions.SemanticError("Несоответствие количества параметров при вызове функции " + f.Name.Name, f.Name.Pos);
        
        for (int i = 0; i < sym.Params.Count(); i++)
        {
            var tp = CalcTypeVis(f.Pars.lst[i]);
            if (!TypeChecker.AssignComparable(sym.Params[i], tp))
                MyInterpreter.CompilerExceptions.SemanticError("Тип аргумента функции " + tp.ToString() + " не соответствует типу формального параметра " + sym.Params[i].ToString(), f.Name.Pos);
        }
        
        return sym.Type; // тип возвращаемого значения
    }
}



    
  
   