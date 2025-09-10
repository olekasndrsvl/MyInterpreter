namespace MyInterpreter;

public class ASTNodes
{
    public static class RuntimeEnvironment
    {
        public static Dictionary<string, double> VarValues = new Dictionary<string, double>();
    }

    public class Node
    {
        public Position pos { get; set; }
        public virtual T Visit<T>(IVisitor<T> visitor)=> visitor.VisitNode(this);

        public virtual void Visit(IVisitorP visitor)
        {
            visitor.VisitNode(this);
        }
    }
   
    public class ExprNode : Node
    {
        public virtual double Eval() => 0;
        
        public override T Visit<T>(IVisitor<T> visitor) => visitor.VisitExprNode(this);
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitExprNode(this);
        }
    }
    
    public class StatementNode : Node
    {
        public virtual void Execute()
        {
            
        }
        public override T Visit<T>(IVisitor<T> visitor) => visitor.VisitStatementNode(this);
       

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitStatementNode(this);
        }
    }
    public class BinOpNode :ExprNode
    {
        public ExprNode Left { get; set; }
        public ExprNode Right { get; set; }
        public char Operator { get; set; }

        public BinOpNode(ExprNode left, ExprNode right, char op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }
        public override T Visit<T>(IVisitor<T> visitor) => visitor.VisitBinOp(this);
     
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitBinOp(this);
        }
        public override double Eval()
        {
            double l = Left.Eval();
            double r = Right.Eval();

            return Operator switch
            {
                '+' => l + r,
                '*' => l * r,
                '/' => l / r,
                '<' => l < r ? 1 : 0,
                _ => throw new Exception($"Unknown operator: {Operator}")
            };
        }
    } 
    public class ExprListNode: ExprNode
    {
        public List<ExprNode> lst = new List<ExprNode>();
        public void Add(ExprNode statement)
        {
            lst.Add(statement);
        }
        public override T Visit<T>(IVisitor<T> visitor) => visitor.VisitExprList(this);
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitExprList(this);
        }
    }
    public class StatementListNode:StatementNode
    {
        public List<StatementNode> lst = new List<StatementNode>();

        public override void Execute()
        {
            foreach (var st in lst)
            {
                st.Execute();  
            }
        }
        
        public void Add(StatementNode statement)
        {
            lst.Add(statement);
        }
        public override T Visit<T>(IVisitor<T> visitor) => visitor.VisitStatementList(this);
        
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
        public override T Visit<T>(IVisitor<T> visitor) =>    visitor.VisitInt(this);
     
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitInt(this);
        }
        public override double Eval() => value;
    }
    public class DoubleNode : ExprNode
    {
        public double value;

        public DoubleNode(double value)
        {
            this.value = value;
        }
        public override T Visit<T>(IVisitor<T> visitor) =>   visitor.VisitDouble(this);

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitDouble(this);
        }
        public override double Eval() => value;
    }
    public class IdNode:ExprNode
    {
        public string Name { get; set; }

        public IdNode(string name)
        {
            Name = name;
        }
        public override T Visit<T>(IVisitor<T> visitor) =>   visitor.VisitId(this);

        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitId(this);
        }

        public override double Eval()
        {
            if (RuntimeEnvironment.VarValues.TryGetValue(Name, out double value))
            {
                return value;
            }
            return 0; // Или бросить исключение
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
        public override T Visit<T>(IVisitor<T> visitor) =>   visitor.VisitAssign(this);
        public override void Execute()
        {
            RuntimeEnvironment.VarValues[Ident.Name] = Expr.Eval();
        }
    }
    
    public class AssignPlusNode : StatementNode
    {
        public IdNode Ident { get; }
        public ExprNode Expr { get; }

        public AssignPlusNode(IdNode ident, ExprNode expr, Position pos)
        {
            Ident = ident;
            Expr = expr;
            this.pos = pos;
        }

        public override void Execute()
        {
            string name = Ident.Name;
            double current = RuntimeEnvironment.VarValues.TryGetValue(name, out double value) ? value : 0;
            RuntimeEnvironment.VarValues[name] = current + Expr.Eval();
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
        public override T Visit<T>(IVisitor<T> visitor) =>visitor.VisitIf(this);
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitIf(this);
        }
        public override void Execute()
        {
            if (Condition.Eval() > 0)
            {
                ThenStat.Execute();
            }
            else if (ElseStat != null)
            {
                ElseStat.Execute();
            }
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
        public override T Visit<T>(IVisitor<T> visitor) =>  visitor.VisitWhile(this);
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitWhile(this);
        }
        public override void Execute()
        {
            while (Condition.Eval() > 0)
            {
                Stat.Execute();
            }
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
        public override T Visit<T>(IVisitor<T> visitor) => visitor.VisitProcCall(this);
        public override void Visit(IVisitorP visitor)
        {
            visitor.VisitProcCall(this);
        }
        public override void Execute()
        {
            //встроенные процедуры
            if (Name.Name.Equals("print", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(Pars.lst[0].Eval());
                CompilerForm.Instance.ChangeOutputBoxText( Pars.lst[0].Eval().ToString()+"\n");
            }
        }
    }
    public class FuncCallNode :ExprNode
    {
       public IdNode Name { get; set; }
       public ExprListNode Pars { get; set; }

       public FuncCallNode(IdNode name, ExprListNode pars)
       {
           Name = name;
           Pars = pars;
       }
       public override T Visit<T>(IVisitor<T> visitor) =>  visitor.VisitFuncCall(this);
       public override void Visit(IVisitorP visitor)
       {
           visitor.VisitFuncCall(this);
       }
       public override double Eval()
       {
           // Реализация вызова функций (если требуется)
           return 0;
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
       
        public static BinOpNode Bin(ExprNode left, char op, ExprNode right) 
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
