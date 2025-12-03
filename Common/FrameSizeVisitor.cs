using MyInterpreter.SemanticCheck;
using static MyInterpreter.SemanticCheck.SymbolTable;
using static MyInterpreter.TypeChecker;
namespace MyInterpreter.Common;

public class FrameSizeVisitor :AutoVisitor
{
    public static Dictionary<string,int> FrameSizes = new Dictionary<string, int>();
    private static Stack<FunctionSpecialization> _currentCheckingFunctionSpecialization = new Stack<FunctionSpecialization>();
    private static Dictionary<string,HashSet<string>> AlreadyDeclaredVariables = new Dictionary<string, HashSet<string>>();
    private string _currentGeneratingFunctionName ="Main";
    static FrameSizeVisitor()
    {
        FrameSizes.Add("Main",0);
        AlreadyDeclaredVariables.Add("Main", new HashSet<string>());
        if(SymbolTable.FunctionTable.TryGetValue("Main", out var functionTable))
            _currentCheckingFunctionSpecialization.Push(functionTable.Specializations.First());
        else
            throw new CompilerExceptions.UnExpectedException("Something went wrong! We have no Main function in function table!");
    }
    
    public Dictionary<string, int> GetFrameSizes()
    {
        return FrameSizes;
    }

  
    
    public static void Reset()
    {
        FrameSizes.Clear();
        AlreadyDeclaredVariables.Clear();
        FrameSizes.Add("Main",0);
        AlreadyDeclaredVariables.Add("Main", new HashSet<string>());
        if(SymbolTable.FunctionTable.TryGetValue("Main", out var functionTable))
            _currentCheckingFunctionSpecialization.Push(functionTable.Specializations.First());
        else
            throw new CompilerExceptions.UnExpectedException("Something went wrong! We have no Main function in function table!");
    }

    public override void VisitId(IdNode node)
    {
        if(!AlreadyDeclaredVariables[_currentGeneratingFunctionName].Add(node.Name))
        {
            FrameSizes[_currentGeneratingFunctionName] += 1;
        }
    }

    public override void VisitAssign(AssignNode ass)
    {
        if (!AlreadyDeclaredVariables[_currentGeneratingFunctionName].Contains(ass.Ident.Name))
            FrameSizes[_currentGeneratingFunctionName] += 1;
        if (!(ass.Expr is IntNode intNode || ass.Expr is DoubleNode doubleNode))
        {
            ass.Expr.VisitP(this);
        }
      
    }

    public override void VisitInt(IntNode n) => FrameSizes[_currentGeneratingFunctionName] += 1;
    
    public override void VisitDouble(DoubleNode n) => FrameSizes[_currentGeneratingFunctionName] += 1;
    public override void VisitAssignOp(AssignOpNode ass) => FrameSizes[_currentGeneratingFunctionName] += 3;

    public override void VisitProcCall(ProcCallNode p)
    {
        foreach (var param in p.Pars.lst)
        {
            param.VisitP(this);
        }
        
    }

    public override void VisitFuncCall(FuncCallNode f)
    {
       
    }

    public override void VisitFuncDef(FuncDefNode fd)
    {
        fd.Body.VisitP(this);
    }
    
}