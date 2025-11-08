namespace MyInterpreter;
using static InterpreterTree;
using static TypeChecker;

// Ref-обертки для доступа к значениям по индексу
public class RuntimeValueRefInt : InterpreterTree.Ref<int>
{
    private readonly int _index;
    
    public RuntimeValueRefInt(int index)
    {
        _index = index;
    }
    
    public override int Value
    {
        get => SymbolTable.VarValues[_index].I;
        set => SymbolTable.VarValues[_index].I = value;
    }
}

public class RuntimeValueRefDouble : InterpreterTree.Ref<double>
{
    private readonly int _index;
    
    public RuntimeValueRefDouble(int index)
    {
        _index = index;
    }
    
    public override double Value
    {
        get => SymbolTable.VarValues[_index].R;
        set => SymbolTable.VarValues[_index].R = value;
    }
}

public class RuntimeValueRefBool : InterpreterTree.Ref<bool>
{
    private readonly int _index;
    
    public RuntimeValueRefBool(int index)
    {
        _index = index;
    }
    
    public override bool Value
    {
        get => SymbolTable.VarValues[_index].B;
        set => SymbolTable.VarValues[_index].B = value;
    }
}


public class ConvertASTToInterpretTreeVisitor : IVisitor<InterpreterTree.NodeI>
{
    public InterpreterTree.NodeI VisitNode(Node node) => null;
    public InterpreterTree.NodeI VisitExprNode(ExprNode node) => null;
    public InterpreterTree.NodeI VisitStatementNode(StatementNode node) => null;

    public InterpreterTree.NodeI VisitStatementList(StatementListNode stl)
    {
        var L = new InterpreterTree.StatementListNodeI();
        foreach (var x in stl.lst)
            L.Add(x.Visit(this) as InterpreterTree.StatementNodeI);
        return L;
    }

    public InterpreterTree.NodeI VisitExprList(ExprListNode exlist)
    {
        var L = new InterpreterTree.ExprListNodeI();
        foreach (var x in exlist.lst)
            L.Add(x.Visit(this) as InterpreterTree.ExprNodeI);
        return L;
    }

    public InterpreterTree.NodeI VisitInt(IntNode n) => new InterpreterTree.IntNodeI(n.Val);
    public InterpreterTree.NodeI VisitDouble(DoubleNode d) => new InterpreterTree.DoubleNodeI(d.Val);

    public InterpreterTree.NodeI VisitId(IdNode id)
    {
        // Создаем Ref-обертки для доступа к значениям по индексу
        switch (id.ValueType)
        {
            case SemanticType.IntType:
                return new InterpreterTree.IdNodeI(new RuntimeValueRefInt(id.ind));
            case SemanticType.DoubleType:
                return new InterpreterTree.IdNodeR(new RuntimeValueRefDouble(id.ind));
            case SemanticType.BoolType:
                return new InterpreterTree.IdNodeB(new RuntimeValueRefBool(id.ind));
            default:
                throw new InvalidOperationException($"Unknown type for variable {id.Name}");
        }
    }

    public InterpreterTree.NodeI VisitWhile(WhileNode whn)
    {
        return new InterpreterTree.WhileNodeI(
            whn.Condition.Visit(this) as InterpreterTree.ExprNodeI,
            whn.Stat.Visit(this) as InterpreterTree.StatementNodeI
        );
    }

    public NodeI VisitFor(ForNode forNode)
    {
        var temp = forNode.Increment.Visit(this);
        return new ForNodeI(forNode.Counter.Visit(this) as AssignIntNodeI, forNode.Condition.Visit(this) as ExprNodeI, 
           temp as AssignIntCNodeI, forNode.Stat.Visit(this) as StatementNodeI);
    }

    public InterpreterTree.NodeI VisitIf(IfNode ifn)
    {
        var thenStat = ifn.ThenStat.Visit(this) as InterpreterTree.StatementNodeI;
        InterpreterTree.StatementNodeI elseStat = null;
        if (ifn.ElseStat != null)
            elseStat = ifn.ElseStat.Visit(this) as InterpreterTree.StatementNodeI;

        return new InterpreterTree.IfNodeI(
            ifn.Condition.Visit(this) as InterpreterTree.ExprNodeI,
            thenStat,
            elseStat
        );
    }

    public InterpreterTree.NodeI VisitAssign(AssignNode ass)
    {
        var expr = ass.Expr.Visit(this) as InterpreterTree.ExprNodeI;

        switch (ass.Ident.ValueType)
        {
            case SemanticType.IntType:
                return new InterpreterTree.AssignIntNodeI(new RuntimeValueRefInt(ass.Ident.ind), expr);
            case SemanticType.DoubleType:
                return new InterpreterTree.AssignRealNodeI(new RuntimeValueRefDouble(ass.Ident.ind), expr);
            case SemanticType.BoolType:
                return new InterpreterTree.AssignBoolNodeI(new RuntimeValueRefBool(ass.Ident.ind), expr);
            default:
                throw new InvalidOperationException($"Unknown type for assignment to {ass.Ident.Name}");
        }
    }



    public InterpreterTree.NodeI VisitAssignOp(AssignOpNode ass)
    {
        if (ass.Op == '+')
        {
            var typ = SymbolTable.SymTable[ass.Ident.Name].Type;
            if (typ == SemanticType.IntType)
            {
                var intc = ass.Expr;
                if (ass.Expr is IntNode)
                {
                    return new AssignPlusIntCNodeI(new RuntimeValueRefInt(ass.Ident.ind), (intc as IntNode).Val);
                }
                else
                {
                    return new AssignPlusIntNodeI(new RuntimeValueRefInt(ass.Ident.ind),
                        ass.Expr.Visit(this) as ExprNodeI);
                }
            }
            else
            {
                if (typ == SemanticType.IntType)
                {
                    var intc = ass.Expr;
                    if (ass.Expr is IntNode)
                    {
                        return new AssignPlusRealCNodeI(new RuntimeValueRefDouble(ass.Ident.ind),
                            (intc as IntNode).Val);
                    }
                    else
                    {
                        //return new AssignPlusRealCNodeI(new RuntimeValueRefDouble(ass.Ident.ind),ass.Expr.Visit(this) as ExprNodeI);
                    }
                }
            }
        }
        return null;
    }

    public InterpreterTree.NodeI VisitFuncCall(FuncCallNode f) => null;
    public NodeI VisitFunDef(FuncDefNode f)
    {
        throw new NotImplementedException();
    }

    public NodeI VisitReturn(ReturnNode r)
    {
        throw new NotImplementedException();
    }

    public InterpreterTree.NodeI VisitBinOp(BinOpNode bin)
    {
        var lt = CalcType(bin.Left);
        var rt = CalcType(bin.Right);
        var linterpr = bin.Left.Visit(this) as InterpreterTree.ExprNodeI;
        var rinterpr = bin.Right.Visit(this) as InterpreterTree.ExprNodeI;

        // int real bool
        // 0: i i
        // 1: i r
        // 2: i b
        // 3: r i +3
        // 4: r r
        // 5: r b
        // 6: b i +6
        // 7: b r
        // 8: b b
        int sit = 0;
        if (rt == SemanticType.DoubleType)
            sit += 1;
        else if (rt == SemanticType.BoolType)
            sit += 2;

        if (lt == SemanticType.DoubleType)
            sit += 3;
        else if (lt == SemanticType.BoolType)
            sit += 6;

        switch (bin.Op)
        {
            case TokenType.Plus:
                switch (sit)
                {
                    case 0:
                        if (rinterpr is InterpreterTree.IntNodeI ric)
                            return new InterpreterTree.PlusIC(linterpr, ric.Val);
                        else
                            return new InterpreterTree.PlusII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.PlusRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.PlusIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.PlusRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Plus operation for types {lt}, {rt}");
                }

            case TokenType.Minus:
                switch (sit)
                {
                    case 0: return new InterpreterTree.MinusII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.MinusRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.MinusIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.MinusRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Minus operation for types {lt}, {rt}");
                }

            case TokenType.Multiply:
                switch (sit)
                {
                    case 0: return new InterpreterTree.MultII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.MultRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.MultIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.MultRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Multiply operation for types {lt}, {rt}");
                }

            case TokenType.Divide:
                switch (sit)
                {
                    case 0: return new InterpreterTree.DivII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.DivRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.DivIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.DivRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Divide operation for types {lt}, {rt}");
                }

            case TokenType.Equal:
                switch (sit)
                {
                    case 0: return new InterpreterTree.EqII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.EqRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.EqIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.EqRI(linterpr, rinterpr);
                    case 8: return new InterpreterTree.EqBB(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Equal operation for types {lt}, {rt}");
                }

            case TokenType.NotEqual:
                switch (sit)
                {
                    case 0: return new InterpreterTree.NotEqII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.NotEqRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.NotEqIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.NotEqRI(linterpr, rinterpr);
                    case 8: return new InterpreterTree.NotEqBB(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid NotEqual operation for types {lt}, {rt}");
                }

            case TokenType.Less:
                switch (sit)
                {
                    case 0: return new InterpreterTree.LessII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.LessRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.LessIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.LessRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Less operation for types {lt}, {rt}");
                }

            case TokenType.LessEqual:
                switch (sit)
                {
                    case 0: return new InterpreterTree.LessEqII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.LessEqRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.LessEqIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.LessEqRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid LessEqual operation for types {lt}, {rt}");
                }

            case TokenType.Greater:
                switch (sit)
                {
                    case 0: return new InterpreterTree.GreaterII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.GreaterRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.GreaterIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.GreaterRI(linterpr, rinterpr);
                    default: throw new InvalidOperationException($"Invalid Greater operation for types {lt}, {rt}");
                }

            case TokenType.GreaterEqual:
                switch (sit)
                {
                    case 0: return new InterpreterTree.GreaterEqII(linterpr, rinterpr);
                    case 4: return new InterpreterTree.GreaterEqRR(linterpr, rinterpr);
                    case 1: return new InterpreterTree.GreaterEqIR(linterpr, rinterpr);
                    case 3: return new InterpreterTree.GreaterEqRI(linterpr, rinterpr);
                    default:
                        throw new InvalidOperationException($"Invalid GreaterEqual operation for types {lt}, {rt}");
                }

            default:
                throw new NotImplementedException($"Binary operation {bin.Op} not implemented");
        }
    }

    public InterpreterTree.NodeI VisitProcCall(ProcCallNode p)
    {
        return new InterpreterTree.ProcCallNodeI(
            p.Name.Name,
            p.Pars.Visit(this) as InterpreterTree.ExprListNodeI
        );
    }
}