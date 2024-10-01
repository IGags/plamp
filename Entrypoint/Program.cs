using System;
using System.Linq;
using Parser;
using Parser.Assembly;

namespace mplg;

class Program
{
    static void Main(string[] args)
    {
        var parser = new PlampNativeParser("""
                                         def int main(int a, int b)
                                             return (a + b) * (a - b)
                                         """);
        var ast = parser.Parse([new StdLib()]);
        var func = ast.First().Compile<Func<int, int, int>>();
        Console.WriteLine(func(5, 4));
    }
}