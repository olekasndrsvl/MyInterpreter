using System.Diagnostics.Metrics;

namespace MyInterpreter;

public class InterpreterTree
{
    // Базовые классы узлов
    public abstract class NodeI
    {
    }

    public abstract class ExprNodeI : NodeI
    {
        public virtual int EvalInt() => 0;
        public virtual double EvalReal() => 0.0;
        public virtual bool EvalBool() => false;
    }

    public abstract class StatementNodeI : NodeI
    {
        public virtual void Execute() { }
    }

    // Вспомогательные классы для работы с ссылками
    public class Ref<T>
    {
        public virtual T Value { get; set; }
        public Ref(T value = default(T)) => Value = value;
    }

    // Бинарные операции
    public abstract class BinOpNodeI : ExprNodeI
    {
        public ExprNodeI Left { get; set; }
        public ExprNodeI Right { get; set; }

        protected BinOpNodeI(ExprNodeI left, ExprNodeI right)
        {
            Left = left;
            Right = right;
        }
    }

    // Арифметические операции
    public class PlusII : BinOpNodeI
    {
        public PlusII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override int EvalInt() => Left.EvalInt() + Right.EvalInt();
    }

    public class PlusIR : BinOpNodeI
    {
        public PlusIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalInt() + Right.EvalReal();
    }

    public class PlusRI : BinOpNodeI
    {
        public PlusRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() + Right.EvalInt();
    }

    public class PlusRR : BinOpNodeI
    {
        public PlusRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() + Right.EvalReal();
    }

    public class PlusIC : BinOpNodeI
    {
        public int Value { get; }
        public PlusIC(ExprNodeI left, int value) : base(left, null) => Value = value;
        public override int EvalInt() => Left.EvalInt() + Value;
    }

    public class PlusRC : BinOpNodeI
    {
        public double Value { get; }
        public PlusRC(ExprNodeI left, double value) : base(left, null) => Value = value;
        public override double EvalReal() => Left.EvalReal() + Value;
    }

    public class MinusII : BinOpNodeI
    {
        public MinusII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override int EvalInt() => Left.EvalInt() - Right.EvalInt();
    }

    public class MinusIR : BinOpNodeI
    {
        public MinusIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalInt() - Right.EvalReal();
    }

    public class MinusRI : BinOpNodeI
    {
        public MinusRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() - Right.EvalInt();
    }

    public class MinusRR : BinOpNodeI
    {
        public MinusRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() - Right.EvalReal();
    }

    public class MinusIC : BinOpNodeI
    {
        public int Value { get; }
        public MinusIC(ExprNodeI left, int value) : base(left, null) => Value = value;
        public override int EvalInt() => Left.EvalInt() - Value;
    }

    public class MinusRC : BinOpNodeI
    {
        public double Value { get; }
        public MinusRC(ExprNodeI left, double value) : base(left, null) => Value = value;
        public override double EvalReal() => Left.EvalReal() - Value;
    }

    public class MultII : BinOpNodeI
    {
        public MultII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override int EvalInt() => Left.EvalInt() * Right.EvalInt();
    }

    public class MultIR : BinOpNodeI
    {
        public MultIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalInt() * Right.EvalReal();
    }

    public class MultRI : BinOpNodeI
    {
        public MultRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() * Right.EvalInt();
    }

    public class MultRR : BinOpNodeI
    {
        public MultRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() * Right.EvalReal();
    }

    public class MultIC : BinOpNodeI
    {
        public int Value { get; }
        public MultIC(ExprNodeI left, int value) : base(left, null) => Value = value;
        public override int EvalInt() => Left.EvalInt() * Value;
    }

    public class MultRC : BinOpNodeI
    {
        public double Value { get; }
        public MultRC(ExprNodeI left, double value) : base(left, null) => Value = value;
        public override double EvalReal() => Left.EvalReal() * Value;
    }

    public class DivII : BinOpNodeI
    {
        public DivII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalInt() / Right.EvalInt();
    }

    public class DivIR : BinOpNodeI
    {
        public DivIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalInt() / Right.EvalReal();
    }

    public class DivRI : BinOpNodeI
    {
        public DivRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() / Right.EvalInt();
    }

    public class DivRR : BinOpNodeI
    {
        public DivRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override double EvalReal() => Left.EvalReal() / Right.EvalReal();
    }

    public class DivIC : BinOpNodeI
    {
        public int Value { get; }
        public DivIC(ExprNodeI left, int value) : base(left, null) => Value = value;
        public override double EvalReal() => Left.EvalInt() / Value;
    }

    public class DivRC : BinOpNodeI
    {
        public double Value { get; }
        public DivRC(ExprNodeI left, double value) : base(left, null) => Value = value;
        public override double EvalReal() => Left.EvalReal() / Value;
    }

    // Операции сравнения
    public class LessII : BinOpNodeI
    {
        public LessII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() < Right.EvalInt();
    }

    public class LessIR : BinOpNodeI
    {
        public LessIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() < Right.EvalReal();
    }

    public class LessRI : BinOpNodeI
    {
        public LessRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() < Right.EvalInt();
    }

    public class LessRR : BinOpNodeI
    {
        public LessRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() < Right.EvalReal();
    }

    public class GreaterII : BinOpNodeI
    {
        public GreaterII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() > Right.EvalInt();
    }

    public class GreaterIR : BinOpNodeI
    {
        public GreaterIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() > Right.EvalReal();
    }

    public class GreaterRI : BinOpNodeI
    {
        public GreaterRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() > Right.EvalInt();
    }

    public class GreaterRR : BinOpNodeI
    {
        public GreaterRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() > Right.EvalReal();
    }

    public class LessEqII : BinOpNodeI
    {
        public LessEqII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() <= Right.EvalInt();
    }

    public class LessEqIR : BinOpNodeI
    {
        public LessEqIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() <= Right.EvalReal();
    }

    public class LessEqRI : BinOpNodeI
    {
        public LessEqRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() <= Right.EvalInt();
    }

    public class LessEqRR : BinOpNodeI
    {
        public LessEqRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() <= Right.EvalReal();
    }

    public class GreaterEqII : BinOpNodeI
    {
        public GreaterEqII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() >= Right.EvalInt();
    }

    public class GreaterEqIR : BinOpNodeI
    {
        public GreaterEqIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() >= Right.EvalReal();
    }

    public class GreaterEqRI : BinOpNodeI
    {
        public GreaterEqRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() >= Right.EvalInt();
    }

    public class GreaterEqRR : BinOpNodeI
    {
        public GreaterEqRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalReal() >= Right.EvalReal();
    }

    public class EqII : BinOpNodeI
    {
        public EqII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() == Right.EvalInt();
    }

    public class EqIR : BinOpNodeI
    {
        public EqIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Math.Abs(Left.EvalInt() - Right.EvalReal()) < double.Epsilon;
    }

    public class EqRI : BinOpNodeI
    {
        public EqRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Math.Abs(Left.EvalReal() - Right.EvalInt()) < double.Epsilon;
    }

    public class EqRR : BinOpNodeI
    {
        public EqRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Math.Abs(Left.EvalReal() - Right.EvalReal()) < double.Epsilon;
    }

    public class EqBB : BinOpNodeI
    {
        public EqBB(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalBool() == Right.EvalBool();
    }

    public class NotEqII : BinOpNodeI
    {
        public NotEqII(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalInt() != Right.EvalInt();
    }

    public class NotEqIR : BinOpNodeI
    {
        public NotEqIR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Math.Abs(Left.EvalInt() - Right.EvalReal()) > double.Epsilon;
    }

    public class NotEqRI : BinOpNodeI
    {
        public NotEqRI(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Math.Abs(Left.EvalReal() - Right.EvalInt()) > double.Epsilon;
    }

    public class NotEqRR : BinOpNodeI
    {
        public NotEqRR(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Math.Abs(Left.EvalReal() - Right.EvalReal()) > double.Epsilon;
    }

    public class NotEqBB : BinOpNodeI
    {
        public NotEqBB(ExprNodeI left, ExprNodeI right) : base(left, right) { }
        public override bool EvalBool() => Left.EvalBool() != Right.EvalBool();
    }

    // Узлы для констант
    public class IntNodeI : ExprNodeI
    {
        public int Val { get; }
        public IntNodeI(int value) => Val = value;
        public override int EvalInt() => Val;
    }

    public class DoubleNodeI : ExprNodeI
    {
        public double Val { get; }
        public DoubleNodeI(double value) => Val = value;
        public override double EvalReal() => Val;
    }

    // Узлы для идентификаторов
    public class IdNodeI : ExprNodeI
    {
        private readonly Ref<int> _ref;
        public IdNodeI(Ref<int> pi) => _ref = pi;
        public override int EvalInt() => _ref.Value;
    }

    public class IdNodeR : ExprNodeI
    {
        private readonly Ref<double> _ref;
        public IdNodeR(Ref<double> pr) => _ref = pr;
        public override double EvalReal() => _ref.Value;
    }

    public class IdNodeB : ExprNodeI
    {
        private readonly Ref<bool> _ref;
        public IdNodeB(Ref<bool> pb) => _ref = pb;
        public override bool EvalBool() => _ref.Value;
    }

    public class IdNodeFun : ExprNodeI
    {
        public string Name { get; }
        public IdNodeFun(string name) => Name = name;
    }

    // Узлы для списков
    public class StatementListNodeI : StatementNodeI
    {
        public List<StatementNodeI> List { get; } = new List<StatementNodeI>();
        public void Add(StatementNodeI st) => List.Add(st);

        public override void Execute()
        {
            foreach (var statement in List)
                statement.Execute();
        }
    }

    public class ExprListNodeI : NodeI
    {
        public List<ExprNodeI> List { get; } = new List<ExprNodeI>();
        public void Add(ExprNodeI ex) => List.Add(ex);
    }

    // Узлы для присваивания
    public class AssignIntNodeI : StatementNodeI
    {
        protected readonly Ref<int> _ref;
        protected readonly ExprNodeI _expr;

        public AssignIntNodeI(Ref<int> pi, ExprNodeI expr)
        {
            _ref = pi;
            _expr = expr;
        }

        public override void Execute() => _ref.Value = _expr.EvalInt();
    }

    public class AssignIntCNodeI : StatementNodeI
    {
        protected readonly Ref<int> _ref;
        protected readonly int _val;

        public AssignIntCNodeI(Ref<int> pi, int val)
        {
            _ref = pi;
            _val = val;
        }

        public override void Execute() => _ref.Value = _val;
    }

    public class AssignRealNodeI : StatementNodeI
    {
        protected readonly Ref<double> _ref;
        protected readonly ExprNodeI _expr;

        public AssignRealNodeI(Ref<double> pr, ExprNodeI expr)
        {
            _ref = pr;
            _expr = expr;
        }

        public override void Execute() => _ref.Value = _expr.EvalReal();
    }

    public class AssignRealCNodeI : StatementNodeI
    {
        protected readonly Ref<double> _ref;
        protected readonly double _val;

        public AssignRealCNodeI(Ref<double> pr, double val)
        {
            _ref = pr;
            _val = val;
        }

        public override void Execute() => _ref.Value = _val;
    }

    public class AssignRealIntCNodeI : StatementNodeI
    {
        protected readonly Ref<double> _ref;
        protected readonly int _val;

        public AssignRealIntCNodeI(Ref<double> pr, int val)
        {
            _ref = pr;
            _val = val;
        }

        public override void Execute() => _ref.Value = _val;
    }

    public class AssignBoolNodeI : StatementNodeI
    {
        private readonly Ref<bool> _ref;
        private readonly ExprNodeI _expr;

        public AssignBoolNodeI(Ref<bool> pb, ExprNodeI expr)
        {
            _ref = pb;
            _expr = expr;
        }

        public override void Execute() => _ref.Value = _expr.EvalBool();
    }

    public class AssignPlusIntNodeI : AssignIntNodeI
    {
        public AssignPlusIntNodeI(Ref<int> pi, ExprNodeI expr) : base(pi, expr) { }
        public override void Execute() => _ref.Value += _expr.EvalInt();
    }

    public class AssignPlusRealNodeI : AssignRealNodeI
    {
        public AssignPlusRealNodeI(Ref<double> pr, ExprNodeI expr) : base(pr, expr) { }
        public override void Execute() => _ref.Value += _expr.EvalReal();
    }

    public class AssignPlusIntCNodeI : AssignIntCNodeI
    {
        public AssignPlusIntCNodeI(Ref<int> pi, int val) : base(pi, val) { }
        public override void Execute() => _ref.Value += _val;
    }

    public class AssignPlusRealCNodeI : AssignRealCNodeI
    {
        public AssignPlusRealCNodeI(Ref<double> pr, double val) : base(pr, val) { }
        public override void Execute() => _ref.Value += _val;
    }

    public class AssignPlusRealIntCNodeI : AssignRealIntCNodeI
    {
        public AssignPlusRealIntCNodeI(Ref<double> pr, int val) : base(pr, val) { }
        public override void Execute() => _ref.Value += _val;
    }

    // Узлы для управляющих конструкций
    public class IfNodeI : StatementNodeI
    {
        public ExprNodeI Condition { get; }
        public StatementNodeI ThenStat { get; }
        public StatementNodeI ElseStat { get; }

        public IfNodeI(ExprNodeI condition, StatementNodeI thenStat, StatementNodeI elseStat = null)
        {
            Condition = condition;
            ThenStat = thenStat;
            ElseStat = elseStat;
        }

        public override void Execute()
        {
            if (Condition.EvalBool())
                ThenStat.Execute();
            else
                ElseStat?.Execute();
        }
    }

    public class WhileNodeI : StatementNodeI
    {
        public ExprNodeI Condition { get; }
        public StatementNodeI Stat { get; }

        public WhileNodeI(ExprNodeI condition, StatementNodeI stat)
        {
            Condition = condition;
            Stat = stat;
        }

        public override void Execute()
        {
            while (Condition.EvalBool())
                Stat.Execute();
        }
    }
    
    public class ForNodeI : StatementNodeI
    {
        public AssignIntNodeI Counter { get; set; }
        public ExprNodeI Condition { get; set; }
    
        public AssignIntCNodeI Increment { get; set; }
        public StatementNodeI Stat { get; set; }
    
        public ForNodeI(AssignIntNodeI counter,ExprNodeI Condition, AssignIntCNodeI increment, StatementNodeI Stat)
        {
            this.Condition = Condition;
            this.Counter = counter;
            this.Increment = increment;
            this.Stat = Stat;
           
        }
        public override void Execute()
        {
            Counter.Execute();
            while (Condition.EvalBool())
            {
                Stat.Execute();
                Increment.Execute();
            }
            
        }
    }

    // Узел для вызова процедур
    public class ProcCallNodeI : StatementNodeI
    {
        public string Name { get; }
        public ExprListNodeI Pars { get; }

        public ProcCallNodeI(string name, ExprListNodeI pars)
        {
            Name = name;
            Pars = pars;
        }

        public override void Execute()
        {
            if (Name == "print" && Pars.List.Count > 0)
            {
          
                var value = Pars.List[0];
                if (value is IntNodeI intNode)
                    CompilerForm.Instance.ChangeOutputBoxText(intNode.EvalInt()+"\n");
                else if (value is DoubleNodeI doubleNode)
                    CompilerForm.Instance.ChangeOutputBoxText(doubleNode.EvalInt() + "\n");
                else if (value is IdNodeI idNodeInt)
                    CompilerForm.Instance.ChangeOutputBoxText(idNodeInt.EvalInt()+"\n");
                else if (value is IdNodeR idNodeReal)
                    CompilerForm.Instance.ChangeOutputBoxText(idNodeReal.EvalInt()+"\n");
                else
                    Console.WriteLine("Unknown type to print");
            }
        }
    }
}