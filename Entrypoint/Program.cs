using System;
using System.Linq;
using Parser;
using Parser.Assembly;

namespace entrypoint;

class Program
{
    static void Main(string[] args)
    {
        var parser = new PlampNativeParser("""
                                         def void main(int a)
                                             if(a < 5)
                                                 std.WriteLine("Переменная меньше 5")
                                             elif(a < 10)
                                                 std.WriteLine("Переменная меньше 10")
                                             else
                                                 std.WriteLine("Дальше я считать не умею")
                                         """);
        var ast = parser.Parse([new StdLib()]);
        var func = ast.First().Compile<Action<int>>();
        func(3);
        func(9);
        func(87);
    }
}