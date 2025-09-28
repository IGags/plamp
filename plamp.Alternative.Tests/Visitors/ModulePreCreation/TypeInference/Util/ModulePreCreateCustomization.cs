using System;
using System.Collections.Generic;
using AutoFixture;
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
        var symbolTableMock = new Mock<ISymbolTable>();
        var filePosition = new KeyValuePair<FilePosition, FilePosition>();
        symbolTableMock.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTableMock.Setup(x =>
            x.AddSymbol(It.IsAny<NodeBase>(), It.IsAny<FilePosition>(), It.IsAny<FilePosition>()));
        symbolTableMock.Setup(x =>
                x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>(), It.IsAny<string>()))
            .Returns((Func<NodeBase, PlampExceptionRecord, string, PlampException>)((_, rec, fn) => new PlampException(rec, new(), new(), fn)));
        
        return new PreCreationContext(context.Create<string>(), symbolTableMock.Object);
    }
}