using System;
using AutoFixture.Kernel;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Visitors.ModulePreCreation;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;

public class ModulePreCreateCustomization : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(PreCreationContext)) return new NoSpecimen();
        var translationTableMock = new Mock<ITranslationTable>();
        var filePosition = new FilePosition();
        translationTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        translationTableMock.Setup(x =>
            x.AddSymbol(It.IsAny<NodeBase>(), It.IsAny<FilePosition>()));
        translationTableMock.Setup(x =>
                x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns((Func<NodeBase, PlampExceptionRecord, PlampException>)((_, rec) => new PlampException(rec, new())));

        var symbolTables = SymbolTableInitHelper.CreateDefaultTables();
        
        return new PreCreationContext(translationTableMock.Object, symbolTables);
    }
}