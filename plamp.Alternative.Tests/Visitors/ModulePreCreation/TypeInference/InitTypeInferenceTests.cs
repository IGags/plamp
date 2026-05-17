using System.IO;
using System.Text;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class InitTypeInferenceTests
{
    [Theory]
    [InlineData("int")]
    [InlineData("array")]
    [InlineData("any")]
    public void InferenceBuiltinType_ReturnsError(string typeName)
    {
        var code = $$"""
                    fn a() {
                        c := {{typeName}}{};
                    }
                    """;
        var (ast, ctx) = SetupAndAct(code);

        var ex = ctx.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.CannotInitBuiltinType().Code);
        
        ast.ShouldBeOfType<RootNode>().Functions.ShouldHaveSingleItem();
    }

    [Fact]
    public void InferenceTypeNotExists_ReturnsError()
    {
        const string code = """
                            fn d() {
                                d := AMANOTHEHERE{};
                            }
                            """;

        var (ast, ctx) = SetupAndAct(code);
        var ex = ctx.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound("AMANOTHERE").Code);
        
        var fn = ast.ShouldBeOfType<RootNode>().Functions.ShouldHaveSingleItem();
        var assign = fn.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        var typeInit = assign.Sources.ShouldHaveSingleItem().ShouldBeOfType<InitTypeNode>();
        typeInit.Type.TypeInfo.ShouldBeNull();
    }

    [Fact]
    public void InferenceExistingType_Correct()
    {
        const string code = """
                            data C;
                            
                            fn a(){
                                v := C{};
                            }
                            """;
        
        var (ast, ctx) = SetupAndAct(code);
        ctx.Exceptions.ShouldBeEmpty();
        
        var fn = ast.ShouldBeOfType<RootNode>().Functions.ShouldHaveSingleItem();
        var assign = fn.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        var typeInit = assign.Sources.ShouldHaveSingleItem().ShouldBeOfType<InitTypeNode>();

        var info = typeInit.Type.TypeInfo.ShouldNotBeNull();
        info.Name.ShouldBe("C");
    }

    [Fact]
    public void InferenceGenericTypeInit_Correct()
    {
        const string code = """
                            data D[T1, T2];
                            
                            fn rn_11(){
                                c := D[int, string]{};
                            }
                            """;

        var (ast, ctx) = SetupAndAct(code);
        ctx.Exceptions.ShouldBeEmpty();
        
        var fn = ast.ShouldBeOfType<RootNode>().Functions.ShouldHaveSingleItem();
        var assign = fn.Body.ExpressionList.ShouldHaveSingleItem().ShouldBeOfType<AssignNode>();
        var typeInit = assign.Sources.ShouldHaveSingleItem().ShouldBeOfType<InitTypeNode>();
        
        var info = typeInit.Type.TypeInfo.ShouldNotBeNull();
        info.DefinitionName.ShouldBe("D");
        info.Name.ShouldBe("D[int, string]");
    }

    public (NodeBase, PreCreationContext) SetupAndAct(string code)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var parseRes = CompilationPipeline.RunParsingAsync(reader, "test.plp").Result;
        var builder = new SymTableBuilder() { ModuleName = "test" };
        var res = CompilationPipeline.RunSymTableBuilding(parseRes.Ast,
            new SymbolTableBuildingContext(parseRes.Context.TranslationTable, [Builtins.SymTable], builder));

        var ctx = new PreCreationContext(parseRes.Context.TranslationTable, [Builtins.SymTable, builder]);
        var visitor = new TypeInferenceWeaver();
        visitor.WeaveDiffs(res.Ast, ctx);
        return (res.Ast, ctx);
    }
}