using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.ModulePreCreation.FillReferenceArray;

public class FillReferenceArrayContext : PreCreationContext
{
    private int _counter;
    
    public List<ArrayFillingContext> ToFill { get; } = [];

    public Stack<List<NodeBase>> NewInstructions { get; } = [];

    public string GetVariableName()
    {
        return $"<{_counter++}_fillReferenceArray>";
    }

    public FillReferenceArrayContext(PreCreationContext other) : base(other)
    {
    }

    public FillReferenceArrayContext(ITranslationTable translationTable, List<ISymTable> dependencies) : base(translationTable, dependencies)
    {
    }
}

public record struct ArrayFillingContext(NodeBase FillTarget, ITypeInfo ItemType);