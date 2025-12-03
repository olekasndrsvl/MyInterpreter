using MyInterpreter.SemanticCheck;

namespace MyInterpreter;

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
    T VisitAssignOp(AssignOpNode ass);
    T VisitIf(IfNode ifn);
    T VisitWhile(WhileNode whn);
    T VisitFor(ForNode forNode);
    T VisitProcCall(ProcCallNode p);
    T VisitFuncCall(FuncCallNode f);
    T VisitFuncDef(FuncDefNode f);
    T VisitFuncDefList(FuncDefListNode lst);
    T VisitReturn(ReturnNode r);
    T VisitFunDefAndStatements(FuncDefAndStatements fdandStmts);
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
    void VisitAssignOp(AssignOpNode ass);
    void VisitIf(IfNode ifn);
    void VisitWhile(WhileNode whn);
    void VisitFor(ForNode forNode);
    void VisitProcCall(ProcCallNode p);
    void VisitFuncCall(FuncCallNode f);
    void VisitFuncDef(FuncDefNode f);
    void VisitFuncDefList(FuncDefListNode lst);
    void VisitFunDefAndStatements(FuncDefAndStatements fdandStmts);
    void VisitReturn(ReturnNode r);
}

public class Node
{
    public Position Pos { get; set; }
    
    public virtual T Visit<T>(IVisitor<T> v) => v.VisitNode(this);
    public virtual void VisitP(IVisitorP v) => v.VisitNode(this);
}

public class ExprNode : Node
{
    public override T Visit<T>(IVisitor<T> v) => v.VisitExprNode(this);
    public override void VisitP(IVisitorP v) => v.VisitExprNode(this);
}

public class StatementNode : Node
{
    public override T Visit<T>(IVisitor<T> v) => v.VisitStatementNode(this);
    public override void VisitP(IVisitorP v) => v.VisitStatementNode(this);
}

public class BinOpNode : ExprNode
{
    public ExprNode Left { get; set; }
    public ExprNode Right { get; set; }
    public TokenType Op { get; set; }
    
    public BinOpNode(ExprNode Left, ExprNode Right, TokenType Op, Position pos = null)
    {
        this.Left = Left;
        this.Right = Right;
        this.Op = Op;
        this.Pos = pos;
    }
    
    public string OpToStr()
    {
        return Op switch
        {
            TokenType.Plus => "+",
            TokenType.Minus => "-",
            TokenType.Multiply => "*",
            TokenType.Divide => "/",
            TokenType.Less => "<",
            TokenType.Greater => ">",
            TokenType.LessEqual => "<=",
            TokenType.GreaterEqual => ">=",
            TokenType.Equal => "==",
            TokenType.NotEqual => "!=",
            TokenType.tkAnd => "&&",
            TokenType.tkOr => "||",
            _ => throw new ArgumentException($"Unknown operator: {Op}")
        };
    }
    
    public override string ToString() => $"({Left}{OpToStr()}{Right})";
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitBinOp(this);
    public override void VisitP(IVisitorP v) => v.VisitBinOp(this);
}

public class StatementListNode : StatementNode
{
    public List<StatementNode> lst = new List<StatementNode>();
    
    public void Add(StatementNode st) => lst.Add(st);
    
    public override string ToString() => string.Join("; ", lst);
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitStatementList(this);
    public override void VisitP(IVisitorP v) => v.VisitStatementList(this);
}

public class FuncDefListNode : FuncDefNode
{
    public List<FuncDefNode> lst = new List<FuncDefNode>();
    public void Add(FuncDefNode f) => lst.Add(f);
    public override string ToString() => string.Join(";", lst);
    public override T Visit<T>(IVisitor<T> v) => v.VisitFuncDefList(this);
    public override void VisitP(IVisitorP v) => v.VisitFuncDefList(this);
   
}

public class ExprListNode : Node
{
    public List<ExprNode> lst = new List<ExprNode>();
    
    public void Add(ExprNode ex) => lst.Add(ex);
    
    public override string ToString() => string.Join(",", lst);
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitExprList(this);
    public override void VisitP(IVisitorP v) => v.VisitExprList(this);
}

public class IntNode : ExprNode
{
    public int Val { get; set; }
    
    public IntNode(int Value, Position p = null)
    {
        Val = Value;
        Pos = p;
    }
    
    public override string ToString() => Val.ToString();
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitInt(this);
    public override void VisitP(IVisitorP v) => v.VisitInt(this);
}

public class DoubleNode : ExprNode
{
    public double Val { get; set; }
    
    public DoubleNode(double Value, Position p = null)
    {
        Val = Value;
        Pos = p;
    }
    
    public override string ToString() => Val.ToString();
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitDouble(this);
    public override void VisitP(IVisitorP v) => v.VisitDouble(this);
}

public class IdNode : ExprNode
{
    public string Name { get; set; }
    public int ind; // индекс в таблице VarValues
    public SemanticType ValueType { get; set; } // тип значения
    
    public IdNode(string name, Position p = null)
    {
        Name = name;
        Pos = p;
    }
    
    public override string ToString() => Name;
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitId(this);
    public override void VisitP(IVisitorP v) => v.VisitId(this);
}

public class AssignNode : StatementNode
{
    public IdNode Ident { get; set; }
    public ExprNode Expr { get; set; }
    
    public AssignNode(IdNode Ident, ExprNode Expr, Position p = null)
    {
        this.Ident = Ident;
        this.Expr = Expr;
        Pos = p;
    }
    
    public override string ToString()
    {
        var exstr = Expr.ToString();
        return $"{Ident} = {exstr}";
    }
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitAssign(this);
    public override void VisitP(IVisitorP v) => v.VisitAssign(this);
}

public class AssignOpNode : StatementNode
{
    public IdNode Ident { get; set; }
    public ExprNode Expr { get; set; }
    public char Op { get; set; } // + - * /
    
    public AssignOpNode(IdNode Ident, ExprNode Expr, char op, Position p = null)
    {
        this.Ident = Ident;
        this.Expr = Expr;
        Op = op;
        Pos = p;
    }
    
    public override string ToString()
    {
        var exstr = Expr.ToString();
        return $"{Ident} {Op}= {exstr}";
    }
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitAssignOp(this);
    public override void VisitP(IVisitorP v) => v.VisitAssignOp(this);
}

public class IfNode : StatementNode
{
    public ExprNode Condition { get; set; }
    public StatementNode ThenStat { get; set; }
    public StatementNode ElseStat { get; set; }
    
    public IfNode(ExprNode Condition, StatementNode ThenStat, StatementNode ElseStat, Position p = null)
    {
        this.Condition = Condition;
        this.ThenStat = ThenStat;
        this.ElseStat = ElseStat;
        Pos = p;
    }
    
    public override string ToString()
    {
        var result = $"if {Condition} then {ThenStat}";
        if (ElseStat != null)
            result += $" else {ElseStat}";
        return result;
    }
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitIf(this);
    public override void VisitP(IVisitorP v) => v.VisitIf(this);
}

public class WhileNode : StatementNode
{
    public ExprNode Condition { get; set; }
    public StatementNode Stat { get; set; }
    
    public WhileNode(ExprNode Condition, StatementNode Stat, Position p = null)
    {
        this.Condition = Condition;
        this.Stat = Stat;
        Pos = p;
    }
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitWhile(this);
    public override void VisitP(IVisitorP v) => v.VisitWhile(this);
}
public class ForNode : StatementNode
{
    public AssignNode Counter { get; set; }
    public ExprNode Condition { get; set; }
    
    public AssignOpNode Increment { get; set; }
    public StatementNode Stat { get; set; }
    
    public ForNode(AssignNode counter,ExprNode Condition, AssignOpNode increment, StatementNode Stat, Position p = null)
    {
        this.Condition = Condition;
        this.Counter = counter;
        this.Increment = increment;
        this.Stat = Stat;
        Pos = p;
    }
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitFor(this);
    public override void VisitP(IVisitorP v) => v.VisitFor(this);
}
public class ProcCallNode : StatementNode
{
    public IdNode Name { get; set; }
    public ExprListNode Pars { get; set; }
    
    public ProcCallNode(IdNode Name, ExprListNode Pars, Position p = null)
    {
        this.Name = Name;
        this.Pars = Pars;
        Pos = p;
    }
    
    public override string ToString() => $"{Name}({string.Join(",", Pars.lst)})";
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitProcCall(this);
    public override void VisitP(IVisitorP v) => v.VisitProcCall(this);
}

public class FuncDefAndStatements : Node
{
    public StatementNode StatementList { get; set; }
    public FuncDefNode FuncDefList { get; set; }

    public FuncDefAndStatements(FuncDefNode funcDefList,StatementNode statementList,  Position p = null)
    {
        this.StatementList = statementList;
        this.FuncDefList = funcDefList;
        Pos = p;
    }
    public override T Visit<T>(IVisitor<T> v) => v.VisitFunDefAndStatements(this);
    public override void VisitP(IVisitorP v) => v.VisitFunDefAndStatements(this);
}

public class FuncDefNode :Node
{
    public IdNode Name { get; set; }
    public List<IdNode> Params { get; set; }
    public StatementNode Body { get; set; }
    public SemanticType ReturnType { get; set; }

    public FuncDefNode()
    {
     
    }
    public FuncDefNode(IdNode Name, List<IdNode> Params, StatementNode Body, Position p = null)
    {
        this.Name = Name;
        this.Params = Params;
        this.Body = Body;
        Pos = p;
    }
        
    public override string ToString() => $"def {Name}({string.Join(",", Params)}) {Body}";
    public override T Visit<T>(IVisitor<T> v) => v.VisitFuncDef(this);
    public override void VisitP(IVisitorP v) => v.VisitFuncDef(this);
}
public class ReturnNode : StatementNode
{
    public ExprNode Expr { get; set; }
    
    public ReturnNode(ExprNode expr, Position p = null)
    {
        Expr = expr;
        Pos = p;
    }
    
    public override string ToString() => $"return {Expr}";
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitReturn(this);
    public override void VisitP(IVisitorP v) => v.VisitReturn(this);
}

public class FuncCallNode : ExprNode
{
    public IdNode Name { get; set; }
    public ExprListNode Pars { get; set; }
    public SemanticType ValueType;
    public int SpecializationId { get; set; } = -1; // -1 означает отсутствие специализации
    public FuncCallNode(IdNode Name, ExprListNode Pars, Position p = null)
    {
        this.Name = Name;
        this.Pars = Pars;
        Pos = p;
    }
    
    public override string ToString() => $"{Name}({string.Join(",", Pars.lst)})";
    
    public override T Visit<T>(IVisitor<T> v) => v.VisitFuncCall(this);
    public override void VisitP(IVisitorP v) => v.VisitFuncCall(this);
}