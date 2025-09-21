using plamp.Abstractions.Ast;

namespace plamp.Cli;

public static class Program
{
    private const string File = 
"""
module playground;
fn array_init() {
    arr := [100]int;
    i := 0;
    while(i < arr.length()) arr[i++] := i * i;
    
    res := arr.binary_search(144);
    
    if(res >= 0){
        print("The index of an element is: ");
        println(res);
    }
    else print("Element not found");
}

fn binary_search([]int array, int num) int {
    if(array.length() = 0) return -1;
    
    left   := 0;
    right  := array.length() - 1;
    
    while(left <= right){
        center := (left + right) / 2;
        
        if(array[center] = num)      return center;
        else if(array[center] < num) left  := center + 1;
        else                         right := center - 1;
    } 
    
    return -1;
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
        var method = res.Compiled!.Modules.First().GetMethod("array_init");
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