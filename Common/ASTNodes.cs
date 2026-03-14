using MyInterpreter.SemanticCheck;

namespace MyInterpreter;

public interface IVisitor<T>
{
    T VisitNode(Node bin);
    T VisitTypeNode(TypeNode tpn);
    T VisitIntTypeNode(IntTypeNode itn);
    T VisitBoolTypeNode(BoolTypeNode btn);
    T VisitDoubleTypeNode(DoubleTypeNode dtn);
    T VisitDefinitionNode(DefinitionNode def);
    T VisitExprNode(ExprNode bin);
    T VisitStatementNode(StatementNode bin);
    T VisitBinOp(BinOpNode bin);
    T VisitStatementList(StatementListNode stl);
    T VisitBlockNode(BlockNode bin);
    T VisitExprList(ExprListNode exlist);
    T VisitInt(IntNode n);
    T VisitDouble(DoubleNode d);
    T VisitId(IdNode id);
    T VisitAssign(AssignNode ass);
    T VisitVarAssign(VarAssignNode ass);
    T VisitAssignOp(AssignOpNode ass);
    T VisitIf(IfNode ifn);
    T VisitWhile(WhileNode whn);
    T VisitFor(ForNode forNode);
    T VisitProcCall(ProcCallNode p);
    T VisitFuncCall(FuncCallNode f);
    T VisitFuncDef(FuncDefNode f);
    T VisitReturn(ReturnNode r);
    T VisitDefinitionsAndStatements(DefinitionsAndStatements fdandStmts);
    T VisitVariableDeclarationNode(VariableDeclarationNode varDecl);
    T VisitDefinitionsList(DefinitionsListNode defList);
}

public interface IVisitorP
{
    void VisitNode(Node bin);
    void VisitTypeNode(TypeNode tpn);
    void VisitIntTypeNode(IntTypeNode itn);
    void VisitBoolTypeNode(BoolTypeNode btn);
    void VisitDoubleTypeNode(DoubleTypeNode dtn);
    void VisitDefinitionNode(DefinitionNode defNode);
    void VisitExprNode(ExprNode bin);
    void VisitStatementNode(StatementNode bin);
    void VisitBinOp(BinOpNode bin);
    void VisitStatementList(StatementListNode stl);
    void VisitBlockNode(BlockNode bin);
    void VisitExprList(ExprListNode exlist);
    void VisitInt(IntNode n);
    void VisitDouble(DoubleNode d);
    void VisitId(IdNode id);
    void VisitAssign(AssignNode ass);
    void VisitAssignOp(AssignOpNode ass);
    void VisitVarAssign(VarAssignNode ass);
    void VisitIf(IfNode ifn);
    void VisitWhile(WhileNode whn);
    void VisitFor(ForNode forNode);
    void VisitProcCall(ProcCallNode p);
    void VisitFuncCall(FuncCallNode f);
    void VisitFuncDef(FuncDefNode f);
    void VisitDefinitionsAndStatements(DefinitionsAndStatements DefandStmts);
    void VisitDefinitionsList(DefinitionsListNode defList);
    void VisitVariableDeclarationNode(VariableDeclarationNode vardecl);
    void VisitReturn(ReturnNode r);
}

public abstract class Node : ICloneable
{
    public Position Pos { get; set; }

    public virtual T Visit<T>(IVisitor<T> v)
    {
        return v.VisitNode(this);
    }

    public virtual void VisitP(IVisitorP v)
    {
        v.VisitNode(this);
    }

    public abstract object Clone();
}

public abstract class DefinitionNode : Node
{
    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitDefinitionNode(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitDefinitionNode(this);
    }
}

public abstract class ExprNode : Node
{
    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitExprNode(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitExprNode(this);
    }
}

public abstract class StatementNode : Node
{
    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitStatementNode(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitStatementNode(this);
    }
}

public abstract class TypeNode : Node
{
    public SemanticType Type = SemanticType.NoType;
    public virtual T Visit<T>(IVisitor<T> v)
    {
        return v.VisitNode(this);
    }

    public virtual void VisitP(IVisitorP v)
    {
        v.VisitNode(this);
    }
}

public class IntTypeNode : TypeNode
{
    public IntTypeNode() => Type = SemanticType.IntType;
    public virtual T Visit<T>(IVisitor<T> v)
    {
        return v.VisitIntTypeNode(this);
    }

    public virtual void VisitP(IVisitorP v)
    {
        v.VisitIntTypeNode(this);
    }
    public override object Clone()
    {
        return this;
    }
}
public class BoolTypeNode : TypeNode
{
    public BoolTypeNode() => Type = SemanticType.BoolType;
    public virtual T Visit<T>(IVisitor<T> v)
    {
        return v.VisitBoolTypeNode(this);
    }

    public virtual void VisitP(IVisitorP v)
    {
        v.VisitBoolTypeNode(this);
    }
    public override object Clone()
    {
        return this;
    }
}

public class DoubleTypeNode : TypeNode
{
    public DoubleTypeNode() => Type = SemanticType.DoubleType;
    public virtual T Visit<T>(IVisitor<T> v)
    {
        return v.VisitDoubleTypeNode(this);
    }

    public virtual void VisitP(IVisitorP v)
    {
        v.VisitDoubleTypeNode(this);
    }
    public override object Clone()
    {
        return this;
    }
}
public class BinOpNode : ExprNode
{
    public BinOpNode(ExprNode Left, ExprNode Right, TokenType Op, Position pos = null)
    {
        this.Left = Left;
        this.Right = Right;
        this.Op = Op;
        Pos = pos;
    }

    public ExprNode Left { get; set; }
    public ExprNode Right { get; set; }
    public TokenType Op { get; set; }

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

    public override string ToString()
    {
        return $"({Left}{OpToStr()}{Right})";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitBinOp(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitBinOp(this);
    }

    public override object Clone()
    {
        return new BinOpNode(
            (ExprNode)Left.Clone(),
            (ExprNode)Right.Clone(),
            Op,
            Pos?.Clone() as Position
        );
    }
}

public class StatementListNode : StatementNode
{
    public List<StatementNode> lst = new();

    public void Add(StatementNode st)
    {
        lst.Add(st);
    }

    public override string ToString()
    {
        return string.Join("; ", lst);
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitStatementList(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitStatementList(this);
    }

    public override object Clone()
    {
        var clone = new StatementListNode();
        foreach (var stmt in lst)
        {
            clone.Add((StatementNode)stmt.Clone());
        }
        clone.Pos = Pos?.Clone() as Position;
        return clone;
    }
}

public class ExprListNode : Node
{
    public List<ExprNode> lst = new();

    public void Add(ExprNode ex)
    {
        lst.Add(ex);
    }

    public override string ToString()
    {
        return string.Join(",", lst);
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitExprList(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitExprList(this);
    }

    public override object Clone()
    {
        var clone = new ExprListNode();
        foreach (var expr in lst)
        {
            clone.Add((ExprNode)expr.Clone());
        }
        clone.Pos = Pos?.Clone() as Position;
        return clone;
    }
}

public class IntNode : ExprNode
{
    public IntNode(int Value, Position p = null)
    {
        Val = Value;
        Pos = p;
    }

    public int Val { get; set; }

    public override string ToString()
    {
        return Val.ToString();
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitInt(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitInt(this);
    }

    public override object Clone()
    {
        return new IntNode(Val, Pos?.Clone() as Position);
    }
}

public class DoubleNode : ExprNode
{
    public DoubleNode(double Value, Position p = null)
    {
        Val = Value;
        Pos = p;
    }

    public double Val { get; set; }

    public override string ToString()
    {
        return Val.ToString();
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitDouble(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitDouble(this);
    }

    public override object Clone()
    {
        return new DoubleNode(Val, Pos?.Clone() as Position);
    }
}

public class IdNode : ExprNode
{
    public IdNode(string name, Position p = null)
    {
        Name = name;
        Pos = p;
    }

    public string Name { get; set; }
    public SemanticType ValueType { get; set; } // тип значения для явно типизации

    public override string ToString()
    {
        return Name;
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitId(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitId(this);
    }

    public override object Clone()
    {
        var clone = new IdNode(Name, Pos?.Clone() as Position);
        clone.ValueType = ValueType; // Копируем тип, если он уже вычислен
        return clone;
    }
}

public class VariableDeclarationNode : DefinitionNode
{
    public VarAssignNode vass;

    public VariableDeclarationNode(VarAssignNode vass, Position p = null)
    {
        this.vass = vass;
        Pos = p;
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitVariableDeclarationNode(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitVariableDeclarationNode(this);
    }

    public override object Clone()
    {
        return new VariableDeclarationNode(
            (VarAssignNode)vass.Clone(),
            Pos?.Clone() as Position
        );
    }
}

public class VarAssignNode : StatementNode
{
    public VarAssignNode(IdNode Ident = null, ExprNode Expr = null, Position p = null)
    {
        this.Ident = Ident;
        this.Expr = Expr;
        Pos = p;
    }

    public IdNode Ident { get; set; }
    public ExprNode Expr { get; set; }

    public override string ToString()
    {
        var exstr = Expr.ToString();
        return $"var {Ident} = {exstr}";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitVarAssign(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitVarAssign(this);
    }

    public override object Clone()
    {
        return new VarAssignNode(
            (IdNode)Ident?.Clone(),
            (ExprNode)Expr?.Clone(),
            Pos?.Clone() as Position
        );
    }
}

public class AssignNode : StatementNode
{
    public AssignNode(IdNode Ident, ExprNode Expr, Position p = null)
    {
        this.Ident = Ident;
        this.Expr = Expr;
        Pos = p;
    }

    public IdNode Ident { get; set; }
    public ExprNode Expr { get; set; }

    public override string ToString()
    {
        var exstr = Expr.ToString();
        return $"{Ident} = {exstr}";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitAssign(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitAssign(this);
    }

    public override object Clone()
    {
        return new AssignNode(
            (IdNode)Ident?.Clone(),
            (ExprNode)Expr?.Clone(),
            Pos?.Clone() as Position
        );
    }
}

public class AssignOpNode : StatementNode
{
    public AssignOpNode(IdNode Ident, ExprNode Expr, char op, Position p = null)
    {
        this.Ident = Ident;
        this.Expr = Expr;
        Op = op;
        Pos = p;
    }

    public IdNode Ident { get; set; }
    public ExprNode Expr { get; set; }
    public char Op { get; set; } // + - * /

    public override string ToString()
    {
        var exstr = Expr.ToString();
        return $"{Ident} {Op}= {exstr}";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitAssignOp(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitAssignOp(this);
    }

    public override object Clone()
    {
        return new AssignOpNode(
            (IdNode)Ident?.Clone(),
            (ExprNode)Expr?.Clone(),
            Op,
            Pos?.Clone() as Position
        );
    }
}

public class BlockNode : StatementNode
{
    public NameSpace BlockNameSpace;
    public StatementListNode lst;

    public BlockNode(StatementListNode stl, Position p = null)
    {
        lst = stl;
        Pos = p;
    }

    public void Add(StatementNode st)
    {
        lst.Add(st);
    }

    public override string ToString()
    {
        return string.Join("; ", lst);
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitBlockNode(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitBlockNode(this);
    }

    public override object Clone()
    {
        var clone = new BlockNode(
            (StatementListNode)lst?.Clone(),
            Pos?.Clone() as Position
        );
        // BlockNameSpace не клонируется - будет создан заново при семантическом анализе
        return clone;
    }
}

public class IfNode : StatementNode
{
    public LightWeightNameSpace ElseNameSpace;
    public LightWeightNameSpace ThenNameSpaceSpace;

    public IfNode(ExprNode Condition, StatementNode ThenStat, StatementNode ElseStat, Position p = null)
    {
        this.Condition = Condition;
        this.ThenStat = ThenStat;
        this.ElseStat = ElseStat;
        Pos = p;
    }

    public ExprNode Condition { get; set; }
    public StatementNode ThenStat { get; set; }
    public StatementNode ElseStat { get; set; }

    public override string ToString()
    {
        var result = $"if {Condition} then {ThenStat}";
        if (ElseStat != null)
            result += $" else {ElseStat}";
        return result;
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitIf(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitIf(this);
    }

    public override object Clone()
    {
        var clone = new IfNode(
            (ExprNode)Condition?.Clone(),
            (StatementNode)ThenStat?.Clone(),
            (StatementNode)ElseStat?.Clone(),
            Pos?.Clone() as Position
        );
        // Пространства имен не клонируются - будут созданы заново
        return clone;
    }
}

public class WhileNode : StatementNode
{
    public LightWeightNameSpace WhileNameSpace;

    public WhileNode(ExprNode Condition, StatementNode Stat, Position p = null)
    {
        this.Condition = Condition;
        this.Stat = Stat;
        Pos = p;
    }

    public ExprNode Condition { get; set; }
    public StatementNode Stat { get; set; }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitWhile(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitWhile(this);
    }

    public override object Clone()
    {
        var clone = new WhileNode(
            (ExprNode)Condition?.Clone(),
            (StatementNode)Stat?.Clone(),
            Pos?.Clone() as Position
        );
        // WhileNameSpace не клонируется
        return clone;
    }
}

public class ForNode : StatementNode
{
    public LightWeightNameSpace ForNameSpace;

    public ForNode(VarAssignNode counter, ExprNode Condition, AssignOpNode increment, StatementNode Stat,
        Position p = null)
    {
        this.Condition = Condition;
        Counter = counter;
        Increment = increment;
        this.Stat = Stat;
        Pos = p;
    }

    public VarAssignNode Counter { get; set; }
    public ExprNode Condition { get; set; }
    public AssignOpNode Increment { get; set; }
    public StatementNode Stat { get; set; }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitFor(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitFor(this);
    }

    public override object Clone()
    {
        var clone = new ForNode(
            (VarAssignNode)Counter?.Clone(),
            (ExprNode)Condition?.Clone(),
            (AssignOpNode)Increment?.Clone(),
            (StatementNode)Stat?.Clone(),
            Pos?.Clone() as Position
        );
        // ForNameSpace не клонируется
        return clone;
    }
}

public class ProcCallNode : StatementNode
{
    public ProcCallNode(IdNode Name, ExprListNode Pars, Position p = null)
    {
        this.Name = Name;
        this.Pars = Pars;
        Pos = p;
    }

    public IdNode Name { get; set; }
    public ExprListNode Pars { get; set; }

    public override string ToString()
    {
        return $"{Name}({string.Join(",", Pars.lst)})";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitProcCall(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitProcCall(this);
    }

    public override object Clone()
    {
        return new ProcCallNode(
            (IdNode)Name?.Clone(),
            (ExprListNode)Pars?.Clone(),
            Pos?.Clone() as Position
        );
    }
}

public class DefinitionsListNode : DefinitionNode
{
    public List<DefinitionNode> lst = new();

    public void Add(DefinitionNode def)
    {
        lst.Add(def);
    }

    public override string ToString()
    {
        return string.Join(";\n", lst);
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitDefinitionsList(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitDefinitionsList(this);
    }

    public override object Clone()
    {
        var clone = new DefinitionsListNode();
        foreach (var def in lst)
        {
            clone.Add((DefinitionNode)def.Clone());
        }
        clone.Pos = Pos?.Clone() as Position;
        return clone;
    }
}

public class DefinitionsAndStatements : Node
{
    public DefinitionsAndStatements(DefinitionsListNode DefList, StatementNode statementList, Position p = null)
    {
        MainProgram = statementList;
        DefinitionsList = DefList;
        Pos = p;
    }

    public StatementNode MainProgram { get; set; }
    public DefinitionsListNode DefinitionsList { get; set; }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitDefinitionsAndStatements(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitDefinitionsAndStatements(this);
    }

    public override object Clone()
    {
        return new DefinitionsAndStatements(
            (DefinitionsListNode)DefinitionsList?.Clone(),
            (StatementNode)MainProgram?.Clone(),
            Pos?.Clone() as Position
        );
    }
}

public class FuncDefNode : DefinitionNode
{
    public FuncDefNode()
    {
    }

    public FuncDefNode(IdNode Name, List<IdNode> Params, StatementNode Body, bool isClearTyped = false, bool isReturnTypeDeclared = false,Position p = null)
    {
        this.Name = Name;
        this.Params = Params;
        this.Body = Body;
        Pos = p;
        IsClearTypes = isClearTyped;
        IsReturnTypeDeclared = isReturnTypeDeclared;
    }

    public IdNode Name { get; set; }
    public List<IdNode> Params { get; set; }
    public bool IsClearTypes = false;
    public bool IsReturnTypeDeclared = false;
    public StatementNode Body { get; set; }
    public SemanticType ReturnType { get; set; }

    public override string ToString()
    {
        // Форматируем параметры с типами
        var formattedParams = Params.Select(p => $"{p.ValueType}: {p.Name}");
        
        return !IsClearTypes 
            ? $"def {Name}({string.Join(", ", Params.Select(p => p.Name))}) {Body}"
            : $"def {Name}({string.Join(", ", formattedParams)}) {Body}";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitFuncDef(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitFuncDef(this);
    }

    public override object Clone()
    {
        var clonedParams = Params?.Select(p => (IdNode)p.Clone()).ToList();
        return new FuncDefNode(
            (IdNode)Name?.Clone(),
            clonedParams,
            (StatementNode)Body?.Clone(),
            IsClearTypes,
            IsReturnTypeDeclared,
            Pos?.Clone() as Position
        )
        {
            ReturnType = ReturnType
        };
    }
}

public class ReturnNode : StatementNode
{
    public ReturnNode(ExprNode expr, Position p = null)
    {
        Expr = expr;
        Pos = p;
    }

    public ExprNode Expr { get; set; }

    public override string ToString()
    {
        return $"return {Expr}";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitReturn(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitReturn(this);
    }

    public override object Clone()
    {
        return new ReturnNode(
            (ExprNode)Expr?.Clone(),
            Pos?.Clone() as Position
        );
    }
}

public class FuncCallNode : ExprNode
{
    public SemanticType ValueType;

    public FuncCallNode(IdNode Name, ExprListNode Pars, Position p = null)
    {
        this.Name = Name;
        this.Pars = Pars;
        Pos = p;
    }

    public IdNode Name { get; set; }
    public ExprListNode Pars { get; set; }
    public int SpecializationId { get; set; } = -1; // -1 означает отсутствие специализации

    public override string ToString()
    {
        return $"{Name}({string.Join(",", Pars.lst)})";
    }

    public override T Visit<T>(IVisitor<T> v)
    {
        return v.VisitFuncCall(this);
    }

    public override void VisitP(IVisitorP v)
    {
        v.VisitFuncCall(this);
    }

    public override object Clone()
    {
        var clone = new FuncCallNode(
            (IdNode)Name?.Clone(),
            (ExprListNode)Pars?.Clone(),
            Pos?.Clone() as Position
        );
        clone.ValueType = ValueType;
        clone.SpecializationId = SpecializationId;
        return clone;
    }
}

