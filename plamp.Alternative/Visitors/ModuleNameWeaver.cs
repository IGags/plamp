using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.AstExtensions;
using RootNode = plamp.Abstractions.Ast.Node.RootNode;

namespace plamp.Alternative.Visitors;

public class ModuleNameWeaver : BaseExtendedWeaver<ModuleNameWeaverContext, ModuleNameInnerWeaverContext, ModuleNameResult>
{
    protected override ModuleNameInnerWeaverContext CreateInnerContext(ModuleNameWeaverContext context)
    {
        return new ModuleNameInnerWeaverContext(context.Exceptions, context.SymbolTable, context.FileName);
    }

    protected override ModuleNameResult CreateWeaveResult(ModuleNameInnerWeaverContext innerContext, ModuleNameWeaverContext outerContext)
    {
        return new ModuleNameResult(innerContext.Exceptions, innerContext.ModuleName);
    }

    protected override VisitResult VisitRoot(RootNode node, ModuleNameInnerWeaverContext context)
    {
        base.VisitRoot(node, context);
        if (context.ModuleName == null) return VisitResult.Break;
        if (context.ModuleNames.Count > 1)
        {
            var record = PlampNativeExceptionInfo.DuplicateModuleDefinition();
            foreach (var module in context.ModuleNames)
            {
                context.Exceptions.Add(context.SymbolTable.CreateExceptionForSymbol(module, record, context.FileName));
            }
        }

        var exceptionRecord = PlampNativeExceptionInfo.MemberCannotHaveSameNameAsDeclaringModule();
        foreach (var module in context.ModuleNames.Select(x => x.ModuleName).Distinct())
        {
            if(!context.Members.TryGetValue(module, out var members)) continue;
            foreach (var member in members)
            {
                context.Exceptions.Add(context.SymbolTable.CreateExceptionForSymbol(member, exceptionRecord, context.FileName));
            }
        }

        return VisitResult.Break;
    }

    protected override VisitResult VisitModuleDefinition(ModuleDefinitionNode definition, ModuleNameInnerWeaverContext context)
    {
        context.ModuleNames.Add(definition);
        return VisitResult.Continue;
    }

    protected override VisitResult VisitDef(DefNode node, ModuleNameInnerWeaverContext context)
    {
        if (!context.Members.TryGetValue(node.Name.MemberName, out var members))
        {
            members = [];
            context.Members.Add(node.Name.MemberName, members);
        }
        members.Add(node);
        return VisitResult.SkipChildren;
    }
}

public record ModuleNameWeaverContext(List<PlampException> Exceptions, SymbolTable SymbolTable, string FileName)
{
    
}

public record ModuleNameInnerWeaverContext(List<PlampException> Exceptions, SymbolTable SymbolTable, string FileName)
{
    public string? ModuleName => ModuleNames.Count > 1 ? null : ModuleNames.First().ModuleName;
    
    public List<ModuleDefinitionNode> ModuleNames { get; } = [];

    public Dictionary<string, List<NodeBase>> Members { get; } = [];
}

public record ModuleNameResult(List<PlampException> Exceptions, string? ModuleName);