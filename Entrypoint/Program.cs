using System;
using Parser;

namespace mplg;

class Program
{
    static void Main(string[] args)
    {
        var tokens = """
                     output.Set(input.Get("x"), "a")
                     output.Set("aaa" == input.Get("d").ToString(),"b")
                     output.Set(false, "c")
                     JsonArray x = new JsonArray
                     for i in input.Get("a").AsArray()
                         JsonObject value = new JsonObject
                         value.Set(i.Get("b"), "r")
                         x.Add(value)
                     output.Set(x, "x")
                     """.Tokenize();
        Console.WriteLine("123");
    }
}