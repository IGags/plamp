using System;
using System.Collections.Generic;
using AutoFixture.Kernel;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Visitors.SymbolTableBuilding;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;

public class SymbolTableBuildingContextCustomization(List<ISymTable> explicitDependencies, ITranslationTable table) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(SymbolTableBuildingContext)) return new NoSpecimen();
        explicitDependencies.Add(Builtins.SymTable);
        return new SymbolTableBuildingContext(table, explicitDependencies, new SymTableBuilder());
    }
}