using System.Text;

namespace MyInterpreter;

public class FormatCodeVisitor : IVisitor<string>
{
    private int ident = 0;
    
    private string Ind()
    {
       // Console.WriteLine(ident);
        return new string(' ', ident);
        
    }

    private string IndInc()
    {
        ident+=2;
        //Console.WriteLine(ident);
        return "";
    }
    private string IndDec()
    {
        ident-=2;
        //Console.WriteLine(ident);
        return "";
    }
    public string VisitNode(Node n) =>  n.Visit(this);
    public string VisitExprNode(ExprNode ex) => ex.Visit(this);
    public string VisitStatementNode(StatementNode st) => Ind() + st.Visit(this);
    
    public string VisitBinOp(BinOpNode bin) =>
        VisitNode(bin.Left) + bin.OpToStr() + VisitNode(bin.Right);

    public string VisitInt(IntNode n) => n.Val.ToString();
    public string VisitDouble(DoubleNode d) => d.Val.ToString();
    public string VisitId(IdNode id) => id.Name;

    public string VisitAssign(AssignNode ass) => Ind() + ass.Ident.Name + " = " + VisitNode(ass.Expr);
    public string VisitAssignOp(AssignOpNode ass)
    {
        return Ind() +  ass.ToString();
    }


    public string VisitIf(IfNode ifn)
    {
        string result = Ind() + "if " + VisitNode(ifn.Condition) + " then"+ '\n' + IndInc() + VisitNode(ifn.ThenStat) + IndDec() + '\n';
        if (ifn.ElseStat != null)
            result += Ind() + " else " +'\n' + IndInc()  + VisitNode(ifn.ElseStat) + IndDec() + '\n';
        return result;
    }

    public string VisitWhile(WhileNode whn) => Ind() + "while " + VisitNode(whn.Condition) + " do\n" + VisitNode(whn.Stat);
    public string VisitFor(ForNode forNode)=> Ind() + "for ("+ forNode.Counter.ToString()+"; " + forNode.Condition.ToString()+ "; " + forNode.Increment.ToString() + ") do\n" + VisitNode(forNode.Stat);
   
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
}
//  x = 1; while x < 10 do { print(x); x = x + 1 }