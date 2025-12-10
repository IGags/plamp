using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Kernel;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Intrinsics;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;

public class SymbolTableBuildingContextCustomization(List<ISymbolTable> explicitDependencies, ITranslationTable table) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(SymbolTableBuildingContext)) return new NoSpecimen();
        var symbolTables = SymbolTableInitHelper.CreateDefaultTables();
        var currentModule = (SymbolTable)symbolTables.First(x => x != RuntimeSymbols.SymbolTable);
        symbolTables.AddRange(explicitDependencies);
        return new SymbolTableBuildingContext(table, symbolTables, currentModule);
    }
}