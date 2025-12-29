using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ParseFieldAccessSequenceTests
{
    [Fact]
    //Пустая строка - корректно
    public void ParseEmpty_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(string.Empty);
        var res = Parser.TryParseFieldAccessSequence(context, new MemberNode("a"), out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        context.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Первый токен не слово - корректно
    public void TryParseNotAccess_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("+");
        var res = Parser.TryParseFieldAccessSequence(context, new MemberNode("a"), out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        context.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Некорректное имя первого поля
    public void TryParseIncorrectName_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(".+");
        var res = Parser.TryParseFieldAccessSequence(context, new MemberNode("a"), out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedFieldName().Code);
    }

    [Fact]
    //Корректный доступ к полю
    public void TryParseFieldAccess_ReturnsCorrect()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(".b");
        var from = new MemberNode("a");
        var res = Parser.TryParseFieldAccessSequence(context, from, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        node.From.ShouldBe(from);
        node.Field.Name.ShouldBe("b");
    }

    [Fact]
    //Список полей - корректно
    public void TryParseMemberAccessSequence_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(".b.c");
        var from = new MemberNode("a");
        var res = Parser.TryParseFieldAccessSequence(context, from, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        node.Field.Name.ShouldBe("c");
        node = node.From.ShouldBeOfType<FieldAccessNode>();
        
        node.Field.Name.ShouldBe("b");
        node.From.ShouldBe(from);
    }
    
    [Fact]
    //Одно из полей недоступно возврат ноды с ошибкой
    public void IncorrectFieldName_CorrectWithException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(".b..");
        var from = new MemberNode("a");
        var res = Parser.TryParseFieldAccessSequence(context, from, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();

        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedFieldName().Code);
        
        node.From.ShouldBe(from);
        node.Field.Name.ShouldBe("b");
    }
}