using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Kernel;
using Moq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Intrinsics;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;

public class SymbolTableBuildingContextCustomization(List<ISymbolTable> explicitDependencies) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(SymbolTableBuildingContext)) return new NoSpecimen();
        var translationTableMock = new Mock<ITranslationTable>();
        var filePosition = new FilePosition();
        translationTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        translationTableMock.Setup(x =>
            x.AddSymbol(It.IsAny<NodeBase>(), It.IsAny<FilePosition>()));
        translationTableMock.Setup(x =>
                x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns((Func<NodeBase, PlampExceptionRecord, PlampException>)((_, rec) => new PlampException(rec, new())));

        var symbolTables = SymbolTableInitHelper.CreateDefaultTables();
        var currentModule = (SymbolTable)symbolTables.First(x => x != RuntimeSymbols.SymbolTable);
        symbolTables.AddRange(explicitDependencies);
        return new SymbolTableBuildingContext(translationTableMock.Object, symbolTables, currentModule);
    }
}