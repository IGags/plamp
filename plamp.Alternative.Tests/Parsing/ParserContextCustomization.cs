using System;
using System.IO;
using System.Text;
using AutoFixture;
using AutoFixture.Kernel;
using plamp.Abstractions.Ast;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tests.Parsing;

public class ParserContextCustomization(string toParse) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(ParsingContext)) return new NoSpecimen();
        var fileName = context.Create<string>();
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(toParse));
        using var reader = new StreamReader(stream, Encoding.Unicode);
        var tokenizationResult = Tokenizer.TokenizeAsync(reader, fileName).Result;
        var translationTable = new TranslationTable();
        AddComments(tokenizationResult.Sequence, translationTable);
        if (tokenizationResult.Sequence.Current() is WhiteSpace)
        {
            tokenizationResult.Sequence.MoveNextNonWhiteSpace();
        }
        var result = new ParsingContext(tokenizationResult.Sequence, tokenizationResult.Exceptions, translationTable);
        return result;
    }

    /// <summary>
    /// Переносит комментарии из токенов в таблицу трансляции тестового контекста.
    /// </summary>
    /// <param name="sequence">Последовательность токенов входного кода.</param>
    /// <param name="translationTable">Таблица трансляции тестового контекста.</param>
    private static void AddComments(TokenSequence sequence, TranslationTable translationTable)
    {
        foreach (var token in sequence)
        {
            if (token is not WhiteSpace whiteSpace)
            {
                continue;
            }

            var kind = whiteSpace.Kind switch
            {
                WhiteSpaceKind.SingleLineComment => CommentKind.SingleLine,
                WhiteSpaceKind.MultiLineComment => CommentKind.MultiLine,
                _ => (CommentKind?)null
            };

            if (kind.HasValue)
            {
                translationTable.AddComment(new SourceComment(whiteSpace.GetStringRepresentation(), whiteSpace.Position, kind.Value));
            }
        }
    }
}