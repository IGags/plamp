using plamp.Abstractions.Ast;

namespace plamp.Cli;

public static class Program
{
    private const string File = 
"""
module playground;

type Point { X, Y: int, Z: int }

fn array_init() {
    arr, i := [100]int, 0;
    while(i < Length(arr)) arr[i++] := i * i;
    
    res := arr.binary_search(144);
    
    if(res >= 0){
        Print("The index of an element is: ");
        Println(res);
    }
    else Print("Element not found");
}

fn binary_search(array: []int, target: int) int {
    if(Length(array) = 0) return -1;
    
    left, right := 0, Length(array) - 1;
    
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
        var res = await CompilationDriver.CompileModuleAsync("aaa.plp", File, false);
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
                var ptrStart = start - rowStart;
                var len = Math.Min(ex.FilePosition.CharacterLength, rowEnd - ptrStart + 1) - 1;
                var str = $"{ex.Message}" + '\n' + row + '\n' + new string(' ',  ptrStart - 1) + '^' + new string('~', len) + '\n';
                Console.WriteLine(str);
            }
        }
    }
}