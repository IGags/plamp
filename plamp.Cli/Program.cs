using plamp.Abstractions.Ast;

namespace plamp.Cli;

public static class Program
{
    private const string File = 
"""
module playground;

type Point { X, Y: int, Z: int }

fn array_init() {
    pt := Point{X, Y, Z: 11};
    arr, i := [100]int, 0;
    while(i < arr.length()) arr[i++] := i * i;
    
    res := arr.binary_search(144);
    
    if(res >= 0){
        print("The index of an element is: ");
        println(res);
    }
    else print("Element not found");
}

fn binary_search(array: []int, target: int) int {
    if(array.length() = 0) return -1;
    
    left, right := 0, array.length() - 1;
    
    while(left <= right){
        center := (left + right) / 2;
        
        if(array[center] = target)      return center;
        else if(array[center] < target) left  := center + 1;
        else                            right := center - 1;
    } 
    
    return -1;
}
""";
    
    public static async Task Main()
    {
        var res = await CompilationDriver.CompileModuleAsync("aaa.plp", File, true);
        if (res.Exceptions.Count > 0 || res.Compiled == null)
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
                var start = (int)ex.FilePosition.ByteOffset / 2;
                var rowStart = File.LastIndexOf('\n', start);
                var rowEnd = File.IndexOf('\n', start + 1);
                rowStart = rowStart == -1 ? 0 : rowStart;
                
                var row = File[rowStart..rowEnd];
                var str = $"{ex.Message}" + '\n' + row + '\n' + new string(' ', start - rowStart - 1) + '^' + '\n';
                Console.WriteLine(str);
            }
        }
    }
}