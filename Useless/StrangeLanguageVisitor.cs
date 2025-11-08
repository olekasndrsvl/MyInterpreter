using System.Text;
namespace MyInterpreter;

public class StrangeLanguageVisitor : IVisitor<string>
{
    private int ident = 0;
    
    private string Ind()
    {
        Console.WriteLine(ident);
        return new string(' ', ident);
        
    }

    private string IndInc()
    {
        ident+=2;
        Console.WriteLine(ident);
        return "";
    }
    private string IndDec()
    {
        ident-=2;
        Console.WriteLine(ident);
        return "";
    }
    public string VisitNode(Node n) =>  n.Visit(this);
    public string VisitExprNode(ExprNode ex) => ex.Visit(this);
    public string VisitStatementNode(StatementNode st) => Ind() + st.Visit(this);
    
    public string VisitBinOp(BinOpNode bin) =>
        VisitNode(bin.Left) + bin.Op + VisitNode(bin.Right);

    public string VisitInt(IntNode n) => n.Val.ToString();
    public string VisitDouble(DoubleNode d) => d.Val.ToString();
    public string VisitId(IdNode id) => id.Name;

    public string VisitAssign(AssignNode ass) => Ind() + ass.Ident.Name + " = " + VisitNode(ass.Expr);
    public string VisitAssignOp(AssignOpNode ass)
    {
        throw new NotImplementedException();
    }

    public string VisitIf(IfNode ifn)
    {
        string result = Ind() + "Если " + VisitNode(ifn.Condition) + '\n' + IndInc() + VisitNode(ifn.ThenStat) + IndDec() + '\n';
        if (ifn.ElseStat != null)
            result += Ind() + " Иначе " +'\n' + IndInc()  + VisitNode(ifn.ElseStat) + IndDec() + '\n';
        return result;
    }

    public string VisitWhile(WhileNode whn) => Ind() + "Пока " + VisitNode(whn.Condition) + " делать\n" + VisitNode(whn.Stat);
    public string VisitFor(ForNode forNode)
    {
        throw new NotImplementedException();
    }

    public string VisitStatementList(StatementListNode stl)
    {
    
        return Ind()+  "{\n" + IndInc() + string.Join(";\n", stl.lst.Select(x=>x.Visit(this))) + IndDec() +'\n' +Ind() + "}";
    }

    public string VisitExprList(ExprListNode exlist) =>
        string.Join(",", exlist.lst.Select(VisitNode));

    public string VisitProcCall(ProcCallNode p) => Ind()+
        p.Name.Name + "(" + VisitNode(p.Pars) + ")";

    public string VisitFuncCall(FuncCallNode f) =>
        f.Name.Name + "(" + VisitNode(f.Pars) + ")";

    public string VisitFuncDef(FuncDefNode f)
    {
        throw new NotImplementedException();
    }

    public string VisitFuncDefList(FuncDefListNode lst)
    {
        throw new NotImplementedException();
    }

    public string VisitReturn(ReturnNode r)
    {
        throw new NotImplementedException();
    }
}