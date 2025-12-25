using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class TypeInitTests
{
    [Fact]
    //Пустая строка - корректно
    public void EmptyCode_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        context.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Не имя типа - ничего не возвращает
    public void NotTypeName_Nothing()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("+");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        context.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Имя типа далее некорректно - возврат ошибки
    public void TypeNameAndNextIncorrect_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType +");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code);
    }

    [Fact]
    //Имя типа и одна открытая скобка - возврат ошибки
    public void TypeNameAndOpenBracket_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType{");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedClosingCurlyBracket().Code);
    }

    [Fact]
    //Имя типа и пустое тело - корректно
    public void EmptyTypeInit_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType {}");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        node.Type.TypeName.Name.ShouldBe("MyType");
        node.FieldInitializers.ShouldBeEmpty();
    }

    [Fact]
    //Имя типа и заполнение поля - корректно
    public void InitTypeWithField_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType {x: 10}");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        node.Type.TypeName.Name.ShouldBe("MyType");
        var initField = node.FieldInitializers.ShouldHaveSingleItem();
        initField.FieldName.Value.ShouldBe("x");
        initField.Value.ShouldBeEquivalentTo(new LiteralNode(10, Builtins.Int));
    }

    [Fact]
    //Имя типа и заполнение нескольких полей корректно
    public void InitTypeWithManyFields_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType {x: 10; y: \"bib\"}");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        context.Exceptions.ShouldBeEmpty();
        node.Type.TypeName.Name.ShouldBe("MyType");
        
        node.FieldInitializers.Count.ShouldBe(2);

        var xField = node.FieldInitializers.First(x => x.FieldName.Value == "x");
        xField.Value.ShouldBeEquivalentTo(new LiteralNode(10, Builtins.Int));
        
        var yField = node.FieldInitializers.First(x => x.FieldName.Value == "y");
        yField.Value.ShouldBeEquivalentTo(new LiteralNode("bib", Builtins.String));
    }

    [Fact]
    //Имя типа и незаполненное поле - возврат ошибки
    public void InitTypeWithNotFilledField_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType {x: }");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedFieldValue().Code);
        
        node.Type.TypeName.Name.ShouldBe("MyType");
        node.FieldInitializers.ShouldBeEmpty();
    }

    [Fact]
    //Имя типа и незакрытое тело инициализации - возврат ошибки
    public void InitTypeNotClosed_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType {x: 10");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedClosingCurlyBracket().Code);
    }

    [Fact]
    //Инициализация нескольких полей - некорректно
    public void InitMany_Incorrect()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("MyType {x, y: 10}");
        var res = Parser.TryParseTypeInit(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        context.Exceptions.Count.ShouldBe(2);
        var codes = context.Exceptions.Select(x => x.Code).ToHashSet();
        codes.ExceptWith([PlampExceptionInfo.ExpectedColon().Code, PlampExceptionInfo.ExpectedClosingCurlyBracket().Code]);
        codes.ShouldBeEmpty();
    }
}