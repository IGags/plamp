using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Cli.Diagnostics;

public class PrintAstVisitor : BaseValidator<BaseVisitorContext, AstPrintingContext>
{
    protected override AstPrintingContext CreateInnerContext(BaseVisitorContext context) => new(context);

    protected override BaseVisitorContext MapInnerToOuter(BaseVisitorContext outerContext, AstPrintingContext innerContext) => innerContext;

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

public class AstPrintingContext(BaseVisitorContext context) : BaseVisitorContext(context)
{
    public int Depth { get; set; }
}