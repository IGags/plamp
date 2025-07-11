using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.AstManipulation.Validation.Models;
using plamp.Alternative.AstExtensions;
using RootNode = plamp.Abstractions.Ast.Node.RootNode;

namespace plamp.Alternative.Visitors;

public class MemberNameUniquenessValidator : BaseExtendedValidator<MemberNameUniquenessValidatorContext, MemberNameUniquenessValidatorInnerContext>
{
    protected override MemberNameUniquenessValidatorInnerContext MapContext(MemberNameUniquenessValidatorContext context)
    {
        return new(context.Exceptions, context.SymbolTable, context.FileName);
    }

    protected override ValidationResult CreateResult(MemberNameUniquenessValidatorContext outerContext,
        MemberNameUniquenessValidatorInnerContext innerContext) =>
        new() { Exceptions = innerContext.Exceptions };

    protected override VisitResult VisitRoot(RootNode node, MemberNameUniquenessValidatorInnerContext context)
    {
        foreach (var child in node.Visit())
        {
            switch (child)
            {
                case DefNode defNode:
                    if (!context.Members.TryGetValue(defNode.Name.MemberName, out var members))
                    {
                        members = [];
                        context.Members.Add(defNode.Name.MemberName, members);
                    }
                    members.Add(defNode);
                    break;
            }
        }

        var record = PlampNativeExceptionInfo.DuplicateMemberNameInModule();
        foreach (var member in context.Members.Values.Where(x => x.Count > 1).SelectMany(x => x))
        {
            context.Exceptions.Add(context.SymbolTable.CreateExceptionForSymbol(member, record, context.FileName));
        }

        return VisitResult.Break;
    }
}



public record MemberNameUniquenessValidatorContext(
    List<PlampException> Exceptions,
    SymbolTable SymbolTable,
    string FileName);

public record MemberNameUniquenessValidatorInnerContext(
    List<PlampException> Exceptions,
    SymbolTable SymbolTable,
    string FileName)
{
    public Dictionary<string, List<NodeBase>> Members { get; } = [];
}