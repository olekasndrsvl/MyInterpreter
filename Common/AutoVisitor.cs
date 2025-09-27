namespace MyInterpreter;

public class AutoVisitor : IVisitorP
{
    public virtual void VisitNode(Node n) { }
    public virtual void VisitExprNode(ExprNode n) { }
    public virtual void VisitStatementNode(StatementNode n) { }
    public virtual void VisitInt(IntNode n) { }
    public virtual void VisitDouble(DoubleNode n) { }
    public virtual void VisitId(IdNode n) { }

    public virtual void VisitBinOp(BinOpNode bin)
    {
        bin.Left.VisitP(this);
        bin.Right.VisitP(this);
    }

    public virtual void VisitStatementList(StatementListNode stl)
    {
        foreach (var x in stl.lst)
            x.VisitP(this);
    }

    public virtual void VisitExprList(ExprListNode exlist)
    {
        foreach (var x in exlist.lst)
            x.VisitP(this);
    }

    public virtual void VisitAssign(AssignNode ass)
    {
        ass.Ident.VisitP(this);
        ass.Expr.VisitP(this);
    }

    public virtual void VisitAssignOp(AssignOpNode ass)
    {
        ass.Ident.VisitP(this);
        ass.Expr.VisitP(this);
    }

    public virtual void VisitIf(IfNode ifn)
    {
        ifn.Condition.VisitP(this);
        ifn.ThenStat.VisitP(this);
        if (ifn.ElseStat != null)
            ifn.ElseStat.VisitP(this);
    }

    public virtual void VisitWhile(WhileNode whn)
    {
        whn.Condition.VisitP(this);
        whn.Stat.VisitP(this);
    }

    public void VisitFor(ForNode forNode)
    {
        forNode.Counter.VisitP(this);
        forNode.Condition.VisitP(this);
        forNode.Increment.VisitP(this);
        forNode.Stat.VisitP(this);
    }

    public virtual void VisitProcCall(ProcCallNode p)
    {
        p.Pars.VisitP(this);
    }

    public virtual void VisitFuncCall(FuncCallNode f)
    {
        f.Pars.VisitP(this);
    }
}