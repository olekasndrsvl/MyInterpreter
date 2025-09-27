namespace MyInterpreter;
using static MyInterpreter.SymbolTable;
using static MyInterpreter.TypeCalculator;
using static MyInterpreter.TypeChecker;
public class SemanticCheckVisitor : AutoVisitor
{
    public override void VisitAssign(AssignNode ass)
    {
        ass.Expr.VisitP(this);
        
        if (!SymTable.ContainsKey(ass.Ident.Name))
        {
            // Вычислить тип
            var typ = CalcTypeVis(ass.Expr);
            
            // Добавить переменную в таблицу переменных с нулевым значением
            switch (typ)
            {
                case SemanticType.IntType:
                    VarValues.Add(new RuntimeValue(0));
                    break;
                case SemanticType.DoubleType:
                    VarValues.Add(new RuntimeValue(0.0));
                    break;
                case SemanticType.BoolType:
                    VarValues.Add(new RuntimeValue(false));
                    break;
            }
            
            ass.Ident.ind = VarValues.Count - 1;
            ass.Ident.ValueType = typ;
            
            // Добавить в таблицу символов
            SymTable[ass.Ident.Name] = new SymbolInfo(ass.Ident.Name, KindType.VarName, typ, ass.Ident.ind);
        }
        else
        {
            // Функции присваивать нельзя
            if (SymTable[ass.Ident.Name].Kind == KindType.FuncName)
                CompilerExceptions.SemanticError($"Имени стандартной функции {ass.Ident.Name} нельзя присвоить значение", ass.Ident.Pos);
            
            // Вычислить тип
            var typ = CalcTypeVis(ass.Expr);
            var idtyp = SymTable[ass.Ident.Name].Type;
            
            if (!AssignComparable(idtyp, typ))
                CompilerExceptions.SemanticError($"Переменной {ass.Ident.Name} типа {idtyp} нельзя присвоить выражение типа {typ}", ass.Ident.Pos);
            
            // Найти индекс существующей переменной
            var ind = SymTable[ass.Ident.Name].Index;
            ass.Ident.ind = ind;
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
            
            var ind = SymTable[ass.Ident.Name].Index;
            ass.Ident.ind = ind;
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
        if (!SymTable.ContainsKey(id.Name))
        {
            CompilerExceptions.SemanticError($"Идентификатор {id.Name} не определен", id.Pos);
            return;
        }
        
        var symbol = SymTable[id.Name];
        id.ind = symbol.Index;
        id.ValueType = symbol.Type;
    }
}