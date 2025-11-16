using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Alternative.Visitors.ModulePreCreation;

namespace plamp.Cli.Diagnostics;

public class PrintAstVisitor : BaseValidator<PreCreationContext, AstPrintingContext>
{
    protected override AstPrintingContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(PreCreationContext outerContext, AstPrintingContext innerContext) => innerContext;

    protected override VisitResult VisitNodeBase(NodeBase node, AstPrintingContext context, NodeBase? parent)
    {
        if (context.Depth != 0)
        {
            if(context.Depth > 1) Console.Write(string.Concat(Enumerable.Repeat("┃   ", context.Depth - 1)));
            Console.Write("┣━━━");
        }
        Console.WriteLine(node.GetType().Name);
        context.Depth++;
        var result = base.VisitNodeBase(node, context, parent);
        context.Depth--;
        return result;
    }
}

public class AstPrintingContext(PreCreationContext context) : PreCreationContext(context)
{
    public int Depth { get; set; }
}