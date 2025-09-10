using System.Text;
using static MyInterpreter.ASTNodes;
namespace MyInterpreter;

public class RefactorVisitor : IVisitor<string>
{
    public string VisitNode(Node n) => n.Visit(this);
    public string VisitExprNode(ExprNode ex) => ex.Visit(this);
    public string VisitStatementNode(StatementNode st) => st.Visit(this);
    
    public string VisitBinOp(BinOpNode bin) =>
        VisitNode(bin.Left) + bin.Operator + VisitNode(bin.Right);

    public string VisitInt(IntNode n) => n.value.ToString();
    public string VisitDouble(DoubleNode d) => d.value.ToString();
    public string VisitId(IdNode id) => id.Name;

    public string VisitAssign(AssignNode ass) => ass.Ident.Name + " = " + VisitNode(ass.Expr);

    public string VisitIf(IfNode ifn)
    {
        string result = "if " + VisitNode(ifn.Condition)  + VisitNode(ifn.ThenStat);
        if (ifn.ElseStat != null)
            result += " else " + VisitNode(ifn.ElseStat);
        return result;
    }

    public string VisitWhile(WhileNode whn) => "while " + VisitNode(whn.Condition) + " do\n" + VisitNode(whn.Stat);

    public string VisitStatementList(StatementListNode stl)
    {
        string statements = string.Join(";\n", stl.lst.Select(x=>x.Visit(this)));
        // var sb = new StringBuilder();
        // foreach (var x in stl.lst)
        // {
        //     sb.Append(VisitNode(x));
        //     sb.Append(";\n");
        // }
        // statements = sb.ToString();
        return "{\n" + statements + "\n}";
    }

    public string VisitExprList(ExprListNode exlist) =>
        string.Join(",", exlist.lst.Select(VisitNode));

    public string VisitProcCall(ProcCallNode p) =>
        p.Name.Name + "(" + VisitNode(p.Pars) + ")";

    public string VisitFuncCall(FuncCallNode f) =>
        f.Name.Name + "(" + VisitNode(f.Pars) + ")";
}