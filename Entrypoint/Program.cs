using System;
using System.Linq;
using System.Linq.Expressions;
using Parser;
using Parser.Assembly;

namespace entrypoint;

class Program
{
    static void Main(string[] args)
    {
        var parser = new PlampNativeParser("""
                                         def void main(int a)
                                             if(a % 2 == 0)
                                                 std.WriteLine("Чёт!")
                                             else
                                                 std.WriteLine("Нечет")
                                         """);
        var ast = parser.Parse([new StdLib()]);
        var func = ast.First().Compile<Action<int>>();
        func(3);
        func(8);
        func(87);
    }
}