// namespace MyInterpreter;
//
// public class Interpreter: IVisitor<object>
// {
//     private static Dictionary<string, object> SymTable = new Dictionary<string, object>();
//     
//     public object VisitNode(Node n) => null;
//     public object VisitExprNode(ExprNode ex) => null;
//     public object VisitStatementNode(StatementNode st) => null;
//     
//     public object VisitBinOp(BinOpNode bin)
//     {
//         var l = bin.Left.Visit(this);
//         var r = bin.Right.Visit(this);
//         
//         switch (bin.Operator)
//         {
//             case OpType.opPlus:
//                 return Convert.ToDouble(l) + Convert.ToDouble(r);
//             case OpType.opLess:
//                 return Convert.ToDouble(l) < Convert.ToDouble(r);
//             default:
//                 throw new Exception($"Unknown operator: {bin.Operator}");
//         }
//     }
//     
//     public object VisitStatementList(StatementListNode stl)
//     {
//         foreach (var st in stl.lst)
//         {
//             st.Visit(this);
//         }
//         return null;
//     }
//     
//     public object VisitExprList(ExprListNode exlist) => null;
//     
//     public object VisitInt(IntNode n) => n.value;
//     
//     public object VisitDouble(DoubleNode d) => d.value;
//     
//     public object VisitId(IdNode id) => SymTable[id.Name];
//     
//     public object VisitAssign(AssignNode ass)
//     {
//         var val = ass.Expr.Visit(this);
//         SymTable[ass.Ident.Name] = val;
//         return null;
//     }
//     public object VisitAssignPlus(AssignPlusNode ass)
//     {
//         var val = ass.Expr.Visit(this);
//         SymTable[ass.Ident.Name] =   SymTable[ass.Ident.Name];
//         return null;
//     }
//     public object VisitIf(IfNode ifn)
//     {
//         var cond = ifn.Condition.Visit(this);
//         if (Convert.ToBoolean(cond))
//         {
//             ifn.ThenStat.Visit(this);
//         }
//         else if (ifn.ElseStat != null)
//         {
//             ifn.ElseStat.Visit(this);
//         }
//         return null;
//     }
//     
//     public object VisitWhile(WhileNode whn)
//     {
//         while (Convert.ToBoolean(whn.Condition.Visit(this)))
//         {
//             whn.Stat.Visit(this);
//         }
//         return null;
//     }
//     
//     public object VisitProcCall(ProcCallNode p)
//     {
//         if (p.Name.Name.Equals("print", StringComparison.OrdinalIgnoreCase))
//         { 
//             CompilerForm.Instance.ChangeOutputBoxText( p.Pars.lst[0].Visit(this)+"\n");
//         }
//         return null;
//     }
//     
//     public object VisitFuncCall(FuncCallNode f)
//     {
//         if (f.Name.Name.Equals("sqrt", StringComparison.OrdinalIgnoreCase))
//         { 
//             return Math.Sqrt(double.Parse( f.Pars.lst[0].Visit(this).ToString()) );
//            
//         }
//         return null;
//     }
// }