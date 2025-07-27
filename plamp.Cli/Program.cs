using plamp.Abstractions.Ast;

namespace plamp.Cli;

class Program
{
    private const string File = """
                                module a2;
                                fn fib(int n) int {
                                    if(n < 0) return n;
                                    if(n = 0 || n = 1) return n;
                                    if(n = 2) return 1;
                                    return fib(n - 1) + fib(n - 2);
                                }
                                """;
    
    static void Main()
    {
        var rows = File.Split('\n');
        var res = CompilationDriver.CompileModule("aaa.plp", File);
        if (res.Exceptions.Count > 0)
        {
            PrintRes(res.Exceptions);
            return;
        }
        var method = res.Compiled!.Modules.First().GetMethod("fib");
        Console.WriteLine("PROGRAM OUTPUT: " + method!.Invoke(null, [14]));

        void PrintRes(List<PlampException> exList)
        {
            foreach (var ex in exList)
            {
                var row = rows[ex.StartPosition.Row];
                var str = $"@@ {ex.StartPosition.Row}, {ex.StartPosition.Column} @@ {ex.Message}" + '\n' + row + '\n';
                str += new string(' ', ex.StartPosition.Column) + $"^\n@@ {ex.EndPosition.Row}, {ex.EndPosition.Column} @@ {ex.FileName}";
                Console.WriteLine(str);
            }
        }
    }
}