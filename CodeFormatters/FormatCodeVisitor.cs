using System.Text;
using System.Text;

namespace MyInterpreter;

public class FormatCodeVisitor : IVisitor<string>
{
    private int ident = 0;
    
    private string Ind()
    {
        return new string(' ', ident);
    }

    private string IndInc()
    {
        ident += 2;
        return "";
    }
    
    private string IndDec()
    {
        ident -= 2;
        return "";
    }
    
    public string VisitNode(Node n) => n.Visit(this);
    public string VisitExprNode(ExprNode ex) => ex.Visit(this);
    public string VisitStatementNode(StatementNode st) => Ind() + st.Visit(this);
    
    public string VisitBinOp(BinOpNode bin) =>
        VisitNode(bin.Left) + bin.OpToStr() + VisitNode(bin.Right);

    public string VisitInt(IntNode n) => n.Val.ToString();
    public string VisitDouble(DoubleNode d) => d.Val.ToString().Replace(',','.');
    public string VisitId(IdNode id) => id.Name;

    public string VisitAssign(AssignNode ass) => Ind()+ ass.Ident.Name + " = " + VisitNode(ass.Expr);
    public string VisitAssignOp(AssignOpNode ass) => ass.ToString();

    public string VisitIf(IfNode ifn)
    {
        string result = Ind() + "if " + VisitNode(ifn.Condition) + " then\n" + 
                       IndInc() + VisitNode(ifn.ThenStat) + IndDec();
        if (ifn.ElseStat != null)
            result += "\n" + Ind() + "else\n" + IndInc() + VisitNode(ifn.ElseStat) + IndDec();
        return result;
    }

    public string VisitWhile(WhileNode whn) => 
        Ind() + "while " + VisitNode(whn.Condition) + " do\n" + 
        IndInc() + VisitNode(whn.Stat) + IndDec();

    public string VisitFor(ForNode forNode) => 
        Ind() + "for (" + forNode.Counter.ToString() + "; " + 
        forNode.Condition.ToString() + "; " + forNode.Increment.ToString() + 
        ") do\n" + IndInc() + VisitNode(forNode.Stat) + IndDec();
   
    public string VisitStatementList(StatementListNode stl)
    {
        string result = Ind() + "{\n" + IndInc();
        var statements = stl.lst.Select(x => x.Visit(this)).ToList();
        result += string.Join(";\n", statements);
        result += IndDec() + "\n" + Ind() + "}";
        return result;
    }

    public string VisitExprList(ExprListNode exlist) =>
        string.Join(", ", exlist.lst.Select(VisitNode));

    public string VisitProcCall(ProcCallNode p) =>
       Ind()+ p.Name.Name + "(" + VisitNode(p.Pars) + ")";

    public string VisitFuncCall(FuncCallNode f) =>
       f.Name.Name + "(" + VisitNode(f.Pars) + ")";

    // Добавляем методы для форматирования функций и return
    public string VisitFuncDef(FuncDefNode f)
    {
        string parameters = string.Join(", ", f.Params.Select(p => p.Name));
        string result = Ind() + "def " + f.Name.Name + "(" + parameters + ")\n" +
                       IndInc() + VisitNode(f.Body) + IndDec();
        return result;
    }

    public string VisitFuncDefList(FuncDefListNode lst)
    {
        string result = Ind() + "\n" + IndInc();
        var functions = lst.lst.Select(x => x.Visit(this)).ToList();
        result += string.Join(";\n", functions);
        result += IndDec() + "\n" + Ind() + "";
        return result;
    }

    public string VisitReturn(ReturnNode r)
    {
        if (r.Expr != null)
            return Ind() + "return " + VisitNode(r.Expr);
        else
            return Ind() + "return";
    }

    public string VisitFunDefAndStatements(FuncDefAndStatements fdandStmts)
    {
        return fdandStmts.FuncDefList.Visit(this)+'\n'+
        fdandStmts.StatementList.Visit(this);
    }
}
//  x = 1; while x < 10 do { print(x); x = x + 1 }
// x = 1; while x < 10 do {  x = x + 1 }