using System;
using AutoFixture;
using AutoFixture.Kernel;
using plamp.Abstractions.Compilation.Models;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;

namespace plamp.Alternative.Tests.Parsing;

public class ParserContextCustomization(string toParse) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(ParsingContext)) return new NoSpecimen();
        var fileName = context.Create<string>();
        var source = new SourceFile(fileName, toParse);
        var tokenizationResult = Tokenizer.Tokenize(source);
        var result = new ParsingContext(tokenizationResult.Sequence, fileName, [], new SymbolTable());
        return result;
    }
}