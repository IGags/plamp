using System.Linq;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class TypedefParsingTests
{
    [Fact]
    //Парсинг пустой строки - возвращает ничего
    public void ParseEmptyCode_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(string.Empty);
        var res = Parser.TryParseTypedef(context, out var node);
        context.Exceptions.ShouldBeEmpty();
        res.ShouldBeFalse();
        node.ShouldBeNull();
    }

    [Fact]
    //Парсинг некорректного кейворда - возвращает ничего
    public void ParseIncorrectKeyword_ReturnsEmpty()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("module");
        var res = Parser.TryParseTypedef(context, out var node);
        context.Exceptions.ShouldBeEmpty();
        res.ShouldBeFalse();
        node.ShouldBeNull();
    }

    [Fact]
    //Парсинг кейворда + некорректное далее - ошибка
    public void ParseOnlyKeyword_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type +");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedTypeName().Code);        
    }

    [Fact]
    //Парсинг кейворда + имени типа - ошибка
    public void ParseKeywordAndName_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType +");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code);
    }

    [Fact]
    //Парсинг кейворда + имени типа + ; - корректно
    public void ParseEmptyType_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType;");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
    }

    [Fact]
    //Парсинг кейворда + имени типа + открывающая скорбка некорректно
    public void ParseTypeMissingCloseBracket_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType {");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedClosingCurlyBracket().Code);
    }

    [Fact]
    //Парсинг кейворда + имени типа(далее префикс типа) + {} - корректно
    public void ParseAnotherEmptyType_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType {}");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
    }

    [Fact]
    //Префикс типа плюс одно поле - корректно
    public void ParseTypeWithField_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType { y: int }");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
        var fld = node.Fields.ShouldHaveSingleItem();
        fld.Name.Value.ShouldBe("y");
        fld.FieldType.TypeName.Name.ShouldBe("int");
    }

    [Fact]
    //Префикс типа плюс несколько полей одного типа - корректно
    public void ParseTypeWithManySameTypedFields_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType { x, y: int }");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
        node.Fields.Count.ShouldBe(2);
        node.Fields.Select(x => x.FieldType.TypeName.Name).All(x => x == "int").ShouldBeTrue();
        var names = new[] { "x", "y" };
        node.Fields.Select(x => names.Contains(x.Name.Value)).All(x => x).ShouldBeTrue();
    }

    [Fact]
    //Префикс типа плюс несколько полей разных типов - корректно
    public void ParseTypeWithManyFieldsDifferentTypes_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType { x: int; y: float }");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
        node.Fields.Count.ShouldBe(2);
        
        var xField = node.Fields.First(x => x.Name.Value == "x");
        xField.FieldType.TypeName.Name.ShouldBe("int");

        var yField = node.Fields.First(x => x.Name.Value == "y");
        yField.FieldType.TypeName.Name.ShouldBe("float");
    }

    [Fact]
    //Префикс типа плюс несколько сложных полей - корректно
    public void ParseTypeComplexCase_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType {x, y: string; z: MyType }");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
        node.Fields.Count.ShouldBe(3);
        
        var xField = node.Fields.First(x => x.Name.Value == "x");
        xField.FieldType.TypeName.Name.ShouldBe("string");

        var yField = node.Fields.First(x => x.Name.Value == "y");
        yField.FieldType.TypeName.Name.ShouldBe("string");
        
        var zField = node.Fields.First(x => x.Name.Value == "z");
        zField.FieldType.TypeName.Name.ShouldBe("MyType");
    }

    [Fact]
    //Незаконченное объявление поля - ошибка
    public void ParseTypeNotCompleteField_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType { x: }");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
        node.Fields.ShouldBeEmpty();

        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedTypeName().Code);
    }

    [Fact]
    //Объявлено имя поля, но не объявлено : с типом после - ошибка
    public void ParseTypeFieldQualifierExpected_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType { x }");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.Name.Value.ShouldBe("MyType");
        node.Fields.ShouldBeEmpty();
        
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedFieldTypeQualifier().Code);
    }

    [Fact]
    //Незакрытое тело типа - ошибка
    public void ParseNotClosedBody_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type MyType { x: float ");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedClosingCurlyBracket().Code);
    }

    [Fact]
    //Тип у которого открыт, но не закрыт дженерик без аргументов
    public void ParseTypeWithNotClosedEmptyGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[;");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.GenericParameters.ShouldBeEmpty();
        
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedGenericTypeArgumentAlias().Code);
    }

    [Fact]
    //Парсинг пустого дженерика - ошибка
    public void ParseTypeWithEmptyGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[];");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.GenericParameters.ShouldBeEmpty();

        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.EmptyGenericDefinition().Code);
    }

    [Fact]
    //Парсинг корректного типа с одним дженериком
    public void ParseTypeWithSingleGeneric_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[T1] {}");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.GenericParameters.ShouldHaveSingleItem().Name.Value.ShouldBe("T1");
        context.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Парсинг корректного типа с двумя дженериками
    public void ParseTypeWithTwoGenerics_Correct()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[T1, T2];");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        node.GenericParameters.Count.ShouldBe(2);
        node.GenericParameters[0].Name.Value.ShouldBe("T1");
        node.GenericParameters[1].Name.Value.ShouldBe("T2");
        context.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Парсин типа с не закрытым заполненным дженериком
    public void ParseTypeWithNotClosedFilledGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen [T {}");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();
        var generic = node.GenericParameters.ShouldHaveSingleItem();
        generic.Name.Value.ShouldBe("T");
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.GenericDefinitionIsNotClosed().Code);
    }

    [Fact]
    //Парсинг типа, где пропущен дженерик
    public void ParseTypeWithSkippedGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[T,];");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeTrue();
        node.ShouldNotBeNull();

        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedGenericTypeArgumentAlias().Code);
    }

    [Fact]
    //Дженерик параметр имеет формат похожий не на объявление, а на тип реализации
    public void PassIncorrectGenericParameterFormat_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[[]T];");
        var res = Parser.TryParseTypedef(context, out _);
        res.ShouldBeFalse();
        
        var exceptionCodes = new[]
        {
            PlampExceptionInfo.ExpectedGenericTypeArgumentAlias().Code,
            PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code
        };
        
        context.Exceptions.Count.ShouldBe(2);
        var actualCodes = context.Exceptions.Select(x => x.Code).ToHashSet();
        exceptionCodes.ShouldAllBe(x => actualCodes.Contains(x));
    }

    [Fact]
    //В объявление дженерик типа передали дженерик тип, ошибка
    public void PassGenericTypeInsideGeneric_ReturnsError()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen[T[int]];");
        var res = Parser.TryParseTypedef(context, out _);
        res.ShouldBeFalse();

        var exceptionCodes = new[]
        {
            PlampExceptionInfo.GenericDefinitionIsNotClosed().Code,
            PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code
        };
        
        context.Exceptions.Count.ShouldBe(2);
        var actualCodes = context.Exceptions.Select(x => x.Code).ToHashSet();
        exceptionCodes.ShouldAllBe(x => actualCodes.Contains(x));
    }

    [Fact] 
    //При парсинге типа с дженериком без открывающей скобки парсер думает, что перед ним тип без тела и не захватывет хвост выражения
    public void ParseTypeWithoutOpenGeneric_ReturnsException()
    {
        var context = CompilationPipelineBuilder.CreateParsingContext("type Gen T];");
        var res = Parser.TryParseTypedef(context, out var node);
        res.ShouldBeFalse();
        node.ShouldBeNull();
        
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code);
    }
    
}