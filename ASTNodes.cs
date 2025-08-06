namespace MyInterpreter;

public class ASTNodes
{
    public class Node
    {
        public virtual void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitNode(this);
        }

        public virtual void Visit(IVisitorP visitor)
        {
            visitor.VisitNode(this);
        }
    }
   
    public class ExprNode : Node
    {
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitExprNode(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitExprNode(this);
        }
    }
    
    public class StatementNode : Node
    {
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitStatementNode(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitStatementNode(this);
        }
    }
    public class BinOpNode :ExprNode
    {
        public ExprNode Left { get; set; }
        public ExprNode Right { get; set; }
        public string Operator { get; set; }

        public BinOpNode(ExprNode left, ExprNode right, string op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitBinOp(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitBinOp(this);
        }
    } 
    public class ExprListNode: ExprNode
    {
        public List<ExprNode> lst = new List<ExprNode>();
        public void Add(ExprNode statement)
        {
            lst.Add(statement);
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitExprList(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitExprList(this);
        }
    }
    public class StatementListNode:StatementNode
    {
        public List<StatementNode> lst = new List<StatementNode>();

        public void Add(StatementNode statement)
        {
            lst.Add(statement);
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitStatementList(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitStatementList(this);
        }
    }
    public class IntNode :ExprNode
    {
        public int value;

        public IntNode(int value)
        {
            this.value = value;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitInt(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitInt(this);
        }
    }
    public class DoubleNode : ExprNode
    {
        public double value;

        public DoubleNode(double value)
        {
            this.value = value;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitDouble(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitDouble(this);
        }
    }
    public class IdNode:ExprNode
    {
        public string Name { get; set; }

        public IdNode(string name)
        {
            Name = name;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitId(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitId(this);
        }
    }
  
    public class AssignNode :StatementNode
    {
        public IdNode Ident { get; set; }
        public ExprNode Expr { get; set; }

        public AssignNode(IdNode ident, ExprNode expr)
        {
            Ident = ident;
            Expr = expr;
        }
        
    }
    public class IfNode : StatementNode
    {
        public ExprNode Condition { get; set; }
        public StatementNode ThenStat { get; set; }
        public StatementNode ElseStat { get; set; }

        public IfNode(ExprNode condition, StatementNode thenStat, StatementNode elseStat)
        {
            Condition = condition;
            ThenStat = thenStat;
            ElseStat = elseStat;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitIf(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitIf(this);
        }
    }
    public class WhileNode :StatementNode
    {
        public ExprNode Condition { get; set; }
        public StatementNode Stat { get; set; }

        public WhileNode(ExprNode condition, StatementNode stat)
        {
            Condition = condition;
            Stat = stat;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitWhile(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitWhile(this);
        }
    }
    public class ProcCallNode : StatementNode
    { 
        public IdNode Name { get; set; }
        public ExprListNode Pars { get; set; }

        public ProcCallNode(IdNode name, ExprListNode pars)
        {
            Name = name;
            Pars = pars;
        }
        public override void Visit<T>(IVisitor<T> visitor)
        {
            visitor.VisitProcCall(this);
        }

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitProcCall(this);
        }
    }
    public class FuncCallNode :StatementNode
    {
       public IdNode Name { get; set; }
       public ExprListNode Pars { get; set; }

       public FuncCallNode(IdNode name, ExprListNode pars)
       {
           Name = name;
           Pars = pars;
       }
       public override void Visit<T>(IVisitor<T> visitor)
       {
           visitor.VisitFuncCall(this);
       }

       public override void Visit(IVisitorP visitor)
       {
           visitor.VisitFuncCall(this);
       }
    }
   
    public interface IVisitor<T>
    {
        T VisitNode(Node bin);
        T VisitExprNode(ExprNode bin);
        T VisitStatementNode(StatementNode bin);
        T VisitBinOp(BinOpNode bin);
        T VisitStatementList(StatementListNode stl);
        T VisitExprList(ExprListNode exlist);
        T VisitInt(IntNode n);
        T VisitDouble(DoubleNode d);
        T VisitId(IdNode id);
        T VisitAssign(AssignNode ass);
        T VisitIf(IfNode ifn);
        T VisitWhile(WhileNode whn);
        T VisitProcCall(ProcCallNode p);
        T VisitFuncCall(FuncCallNode f);
    }
    public interface IVisitorP
    {
        void VisitNode(Node bin);
        void VisitExprNode(ExprNode bin);
        void VisitStatementNode(StatementNode bin);
        void VisitBinOp(BinOpNode bin);
        void VisitStatementList(StatementListNode stl);
        void VisitExprList(ExprListNode exlist);
        void VisitInt(IntNode n);
        void VisitDouble(DoubleNode d);
        void VisitId(IdNode id);
        void VisitAssign(AssignNode ass);
        void VisitIf(IfNode ifn);
        void VisitWhile(WhileNode whn);
        void VisitProcCall(ProcCallNode p);
        void VisitFuncCall(FuncCallNode f);
    }
    public static class ASTBuilder
    {
       
        public static BinOpNode Bin(ExprNode left, string op, ExprNode right) 
            => new BinOpNode(left, right, op);
    
      
        public static AssignNode Ass(IdNode ident, ExprNode expr) 
            => new AssignNode(ident, expr);
    
       
        public static IdNode Id(string name) 
            => new IdNode(name);
    
     
        public static DoubleNode Num(double value) 
            => new DoubleNode(value);
    
       
        public static IfNode Iff(ExprNode cond, StatementNode thenBranch, StatementNode elseBranch) 
            => new IfNode(cond, thenBranch, elseBranch);
    
        
        public static WhileNode Wh(ExprNode cond, StatementNode body) 
            => new WhileNode(cond, body);
    
      
        public static StatementListNode StL(params StatementNode[] statements)
            => new StatementListNode{ lst = statements.ToList()};
    
       
        public static ExprListNode ExL(params ExprNode[] expressions)
            => new ExprListNode {  lst = expressions.ToList() };
    
       
        public static ProcCallNode ProcCall(IdNode name, ExprListNode args)
            => new ProcCallNode(name, args);
    }
}
