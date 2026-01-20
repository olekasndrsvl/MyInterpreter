using MyInterpreter;

public static class ASTPrinter
{
    public static void PrintAST(DefinitionsAndStatements program)
    {
        Console.WriteLine("AST Tree:");
        Console.WriteLine("=========");
        PrintNode(program, 0);
    }

    private static void PrintNode(object node, int indent)
    {
        string indentation = new string(' ', indent * 2);
        
        if (node == null)
        {
            Console.WriteLine($"{indentation}null");
            return;
        }

        switch (node)
        {
            case DefinitionsAndStatements das:
                Console.WriteLine($"{indentation}Program");
                PrintNode(das.DefinitionsList, indent + 1);
                PrintNode(das.MainProgram, indent + 1);
                break;

            case DefinitionsListNode dln:
                Console.WriteLine($"{indentation}DefinitionsList [{dln.lst.Count}]");
                foreach (var def in dln.lst)
                {
                    PrintNode(def, indent + 1);
                }
                break;
            
            case FuncDefNode fdn:
                Console.WriteLine($"{indentation}Function: {fdn.Name.Name}");
                Console.WriteLine($"{indentation}  Parameters:");
                foreach (var param in fdn.Params)
                {
                    PrintNode(param, indent + 2);
                }
                Console.WriteLine($"{indentation}  Body:");
                PrintNode(fdn.Body, indent + 2);
                break;

            case VariableDeclarationNode vdn:
                Console.WriteLine($"{indentation}Global Variable Declaration");
                PrintNode(vdn.vass, indent + 1);
                break;

            case VarAssignNode van:
                Console.WriteLine($"{indentation}Variable: {van.Ident.Name} =");
                PrintNode(van.Expr, indent + 2);
                break;

            case BlockNode bn:
                Console.WriteLine($"{indentation}Block");
                PrintNode(bn.lst, indent + 1);
                break;

            case StatementListNode sln:
                Console.WriteLine($"{indentation}StatementList [{sln.lst.Count}]");
                foreach (var stmt in sln.lst)
                {
                    PrintNode(stmt, indent + 1);
                }
                break;

            case AssignNode an:
                Console.WriteLine($"{indentation}Assignment: {an.Ident.Name} =");
                PrintNode(an.Expr, indent + 2);
                break;

            case AssignOpNode aon:
                Console.WriteLine($"{indentation}Compound Assignment: {aon.Ident.Name} {aon.Op}= ");
                PrintNode(aon.Expr, indent + 2);
                break;

            case ProcCallNode pcn:
                Console.WriteLine($"{indentation}Procedure Call: {pcn.Name.Name}");
                Console.WriteLine($"{indentation}  Arguments:");
                foreach (var arg in pcn.Pars.lst)
                {
                    PrintNode(arg, indent + 2);
                }
                break;

            case FuncCallNode fcn:
                Console.WriteLine($"{indentation}Function Call: {fcn.Name.Name}");
                Console.WriteLine($"{indentation}  Arguments:");
                foreach (var arg in fcn.Pars.lst)
                {
                    PrintNode(arg, indent + 2);
                }
                break;

            case IfNode inode:
                Console.WriteLine($"{indentation}If");
                Console.WriteLine($"{indentation}  Condition:");
                PrintNode(inode.Condition, indent + 2);
                Console.WriteLine($"{indentation}  Then:");
                PrintNode(inode.ThenStat, indent + 2);
                if (inode.ElseStat != null)
                {
                    Console.WriteLine($"{indentation}  Else:");
                    PrintNode(inode.ElseStat, indent + 2);
                }
                break;

            case WhileNode wn:
                Console.WriteLine($"{indentation}While");
                Console.WriteLine($"{indentation}  Condition:");
                PrintNode(wn.Condition, indent + 2);
                Console.WriteLine($"{indentation}  Body:");
                PrintNode(wn.Stat, indent + 2);
                break;

            case ForNode fn:
                Console.WriteLine($"{indentation}For");
                Console.WriteLine($"{indentation}  Initialization:");
                PrintNode(fn.Counter, indent + 2);
                Console.WriteLine($"{indentation}  Condition:");
                PrintNode(fn.Condition, indent + 2);
                Console.WriteLine($"{indentation}  Increment:");
                PrintNode(fn.Increment, indent + 2);
                Console.WriteLine($"{indentation}  Body:");
                PrintNode(fn.Stat, indent + 2);
                break;

            case ReturnNode rn:
                Console.WriteLine($"{indentation}Return");
                PrintNode(rn.Expr, indent + 1);
                break;

            // Expression nodes
            case BinOpNode bon:
                Console.WriteLine($"{indentation}Binary Operation: {TokenToString(bon.Op)}");
                Console.WriteLine($"{indentation}  Left:");
                PrintNode(bon.Left, indent + 2);
                Console.WriteLine($"{indentation}  Right:");
                PrintNode(bon.Right, indent + 2);
                break;

            case IdNode idn:
                Console.WriteLine($"{indentation}Identifier: {idn.Name}");
                break;

            case IntNode inn:
                Console.WriteLine($"{indentation}Integer: {inn.Val}");
                break;

            case DoubleNode dbl:
                Console.WriteLine($"{indentation}Double: {dbl.Val}");
                break;

            case ExprListNode eln:
                Console.WriteLine($"{indentation}ExpressionList [{eln.lst.Count}]");
                foreach (var expr in eln.lst)
                {
                    PrintNode(expr, indent + 1);
                }
                break;

            default:
                Console.WriteLine($"{indentation}[Unknown node type: {node.GetType().Name}]");
                break;
        }
    }

    private static string TokenToString(TokenType token)
    {
        return token.ToString();
    }

}