using System;
using System.Linq;
using Parser;
using Parser.Assembly;

namespace mplg;

class Program
{
    static void Main(string[] args)
    {
        var parser = new PlampNativeTokenizer("""
                                         def int main(int a)
                                             return a + 2 * 2
                                         """);
        var ast = parser.Parse([new StdLib()]);
        var func = ast.First().Compile<Func<int, int>>();
        Console.WriteLine(func(6));
    }
}