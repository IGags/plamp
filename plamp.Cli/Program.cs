using plamp.Abstractions.Ast;

namespace plamp.Cli;

public static class Program
{
    private const string File = """
                                module playground;
                                fn implicit_conv() {
                                    println("try guess number!");
                                    att_count := 3;
                                    number := 5;
                                    while(att_count > 0) {
                                        dec := readln();
                                        if(int(dec) = 5) {
                                            println("correct!");
                                            return;
                                        }
                                        if(int(dec) < 5) println("the number is greater");
                                         if(int(dec) > 5) println("the number is lesser");
                                        println("attempts left:");
                                        
                                        att_count--;
                                        println(att_count);
                                    }
                                    println("you lose");
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