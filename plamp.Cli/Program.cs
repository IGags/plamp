using plamp.Abstractions.Ast;

namespace plamp.Cli;

public static class Program
{
    private const string File = """
                                module playground;
                                fn implicit_conv() {
                                    println(1 + 0.5);
                                    return;
                                }
                                """;
    
    public static void Main()
    {
        var rows = File.Split('\n');
        var res = CompilationDriver.CompileModule("aaa.plp", File);
        if (res.Exceptions.Count > 0)
        {
            PrintRes(res.Exceptions);
            return;
        }
        var method = res.Compiled!.Modules.First().GetMethod("implicit_conv");
        method!.Invoke(null, []);

        void PrintRes(List<PlampException> exList)
        {
            foreach (var ex in exList)
            {
                var row = rows[ex.StartPosition.Row];
                var str = $"@@ {ex.StartPosition.Row}, {ex.StartPosition.Column} @@ {ex.Message}" + '\n' + row + '\n';
                str += new string(' ', ex.StartPosition.Column) + $"^\n@@ {ex.EndPosition.Row}, {ex.EndPosition.Column} @@ {ex.FileName}\n===================================";
                Console.WriteLine(str);
            }
        }
    }
}