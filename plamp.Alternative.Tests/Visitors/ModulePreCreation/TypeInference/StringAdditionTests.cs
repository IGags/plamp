using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Alternative.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class StringAdditionTests
{
    [Theory]
    [InlineData("\"1\" + 2")]
    [InlineData("2 + \"1\"")]
    public void AddStringToNonString_ReturnsException(string code)
    {
        var (ast, translationTable) = ParseBinary(code);
        var ctx = new PreCreationContext(translationTable, [Builtins.SymTable]);
        var validator = new TypeInferenceWeaver();
        var res = validator.WeaveDiffs(ast, ctx);
        var ex = res.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.CannotApplyOperator().Code);
    }

    [Theory]
    [InlineData("a := \"a\" + \"b\"", "ab")]
    [InlineData("a := \"a\" + \"b\" + \"c\"", "abc")]
    [InlineData("a := \"a\" + \"b\" + \"c\" + \"dddd\"", "abcdddd")]
    public void AddLiteralToLiteral_MergeLiteralsCorrect(string code, string resultLteral)
    {
        var (ast, translationTable) = ParseBinary(code);
        var ctx = new PreCreationContext(translationTable, [Builtins.SymTable]);
        var validator = new TypeInferenceWeaver();
        var res = validator.WeaveDiffs(ast, ctx);
        res.Exceptions.ShouldBeEmpty();
        var assignNode = ast.ShouldBeOfType<AssignNode>();
        var lit = assignNode.Sources.ShouldHaveSingleItem().ShouldBeOfType<LiteralNode>();
        lit.Value.ShouldBe(resultLteral);
        lit.Type.ShouldBe(Builtins.String);
    }

    [Fact]
    public void AddLiteralToVariable_ShouldReplaceToConcatCorrect()
    {
        var code = "{a: string; b := a + \"123\"}";
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        Parser.TryParseBody(context, out var body);
        body.ShouldNotBeNull();
        var ctx = new PreCreationContext(context.TranslationTable, [Builtins.SymTable]);
        var validator = new TypeInferenceWeaver();
        var res = validator.WeaveDiffs(body, ctx);
        res.Exceptions.ShouldBeEmpty();
        var concat = body.ExpressionList[1].ShouldBeOfType<AssignNode>().Sources.ShouldHaveSingleItem().ShouldBeOfType<CallNode>();
        concat.FnInfo.ShouldNotBeNull().AsFunc().Invoke(null, ["12", "ab"]).ShouldBe("12ab");
        concat.Args.Count.ShouldBe(2);
        concat.Args[0].ShouldBeOfType<MemberNode>().MemberName.ShouldBe("a");
        concat.Args[1].ShouldBeOfType<LiteralNode>().Value.ShouldBe("123");
    }
    
    private (NodeBase, ITranslationTable) ParseBinary(string code)
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        Parser.TryParseExpression(context, out var body);
        body.ShouldNotBeNull();
        return (body, context.TranslationTable);
    }
}