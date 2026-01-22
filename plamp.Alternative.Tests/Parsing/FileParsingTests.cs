using AutoFixture;
using plamp.Alternative.Parsing;
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
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.ParseFile(context);
        context.Exceptions.ShouldBeEmpty();
        result.ShouldNotBeNull();
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
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.ParseFile(context);
        context.Exceptions.ShouldBeEmpty();
        result.ShouldNotBeNull();
        context.Sequence.MoveNext().ShouldBe(false);
    }
}