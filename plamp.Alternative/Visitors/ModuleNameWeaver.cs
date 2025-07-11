using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Alternative.AstExtensions;
using RootNode = plamp.Alternative.AstExtensions.RootNode;

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
        if (node.ModuleName == null)
        {
            var record = PlampNativeExceptionInfo.ModuleMustHaveName();
            context.Exceptions.Add(context.SymbolTable.CreateExceptionForSymbol(node, record, context.FileName));
            return VisitResult.Break;
        }
        context.ModuleName = node.ModuleName?.ModuleName;
        if(node.ModuleName == null 
           || !context.Members.TryGetValue(node.ModuleName.ModuleName, out var members)) return VisitResult.Break;
        
        var exceptionRecord = PlampNativeExceptionInfo.MemberCannotHaveSameNameAsDeclaringModule();
        foreach (var member in members)
        {
            context.Exceptions.Add(context.SymbolTable.CreateExceptionForSymbol(member, exceptionRecord, context.FileName));
        }

        return VisitResult.Break;
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
    public Dictionary<string, List<NodeBase>> Members { get; } = [];
    
    public string? ModuleName { get; set; }
}

public record ModuleNameResult(List<PlampException> Exceptions, string? ModuleName);