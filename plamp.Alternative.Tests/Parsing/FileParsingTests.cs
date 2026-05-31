using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class FileParsingTests
{
    [Fact]
    public void ParseFibonacci_Correct()
    {
        const string code = """
                            module math.fibonacci_counter;
                            
                            fn fib(n:int) int {
                                if(n < 0) return n;
                                if(n = 0 || n = 1) return n;
                                if(n = 2) return 1;
                                return fib(n - 1) + fib(n - 2);
                            }
                            """;
        var (ast, context) = CompilationPipelineBuilder.RunParsingPipeline(code);
        context.Exceptions.ShouldBeEmpty();
        ast.ShouldNotBeNull();
        context.Sequence.MoveNext().ShouldBe(false);
    }

    [Fact]
    public void SimpleAlgorithms_Correct()
    {
        const string code = """
                            module math;
                            
                            fn euclid_algorithm(first, second :int) int {
                                while (second != 0) {
                                    c := a;
                                    a := b;
                                    b := c % b;
                                }
                                return a;
                            }
                            
                            fn get_line_center(x1, y1, x2, y2:float) Point {
                                return Point((x1 + x2) / 2, (y1 + y2) / 2);
                            }
                            """;
        var (ast, context) = CompilationPipelineBuilder.RunParsingPipeline(code);
        context.Exceptions.ShouldBeEmpty();
        ast.ShouldNotBeNull();
        context.Sequence.MoveNext().ShouldBe(false);
    }

    [Fact]
    public void FileStartsFromWhiteSpacesAndBreaks_Correct()
    {
        const string code = """
                                
                             
                            module test;
                            """;

        var (ast, context) = CompilationPipelineBuilder.RunParsingPipeline(code);
        context.Exceptions.ShouldBeEmpty();
        ast.ModuleName.ShouldNotBeNull().ModuleName.ShouldBe("test");
    }

    [Fact]
    public void ManyIncorrectTokensBetweenTopLevel_ReturnsOnlyOneException()
    {
        const string code = """
                            module test;
                            
                            vbbab
                            
                            sfkk
                            
                            if
                            else
                            while
                            1 4 "224"
                            
                            fn main() {}
                            
                            """;
        var (ast, context) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.TopLevelExpressionExpected().Code);
        
        ast.ModuleName.ShouldNotBeNull().ModuleName.ShouldBe("test");
        ast.Functions.ShouldHaveSingleItem();
    }

    [Fact]
    public void IncorrectTokensBeforeFirstTopLevel_ReturnsSingleException()
    {
        const string code = """
                            
                            babbu
                            '3' 5
                            aaaaaaaa
                            "fafa"
                            
                            module test;
                            """;
        
        var (ast, context) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.TopLevelExpressionExpected().Code);
        ast.ModuleName.ShouldNotBeNull().ModuleName.ShouldBe("test");
    }

    [Fact]
    public void IncorrectTokensAfterLastTopLevel_ReturnsSingleException()
    {
        const string code = """

                            module test;
                            
                            africa privaetn
                            alflf
                            gasman
                            """;
        
        var (ast, context) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.TopLevelExpressionExpected().Code);
        ast.ModuleName.ShouldNotBeNull().ModuleName.ShouldBe("test");
    }
}