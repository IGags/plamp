using System.Diagnostics;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Alternative;

namespace plamp.Cli;

public static class Program
{
    public static async Task<int> Main(params string[] args)
    {
        var sw = Stopwatch.StartNew();
        if (args.Length != 1)
        {
            return -1;
        }

        var filepath = Path.GetFullPath(args[0]);
        await using var file = File.OpenRead(filepath);
        using var reader = new StreamReader(file, Encoding.UTF8, leaveOpen:true);
        var res = await CompilationPipeline.RunEntirePipelineAsync(reader, filepath);

        var fileText = await File.ReadAllTextAsync(filepath, Encoding.UTF8);
        
        Console.WriteLine($"Compilation took {sw.Elapsed}");
        
        if (res.Exceptions.Count > 0 || res.Compiled == null)
        {
            PrintRes(res.Exceptions);
            return -1;
        }
        
        var method = res.Compiled!.Modules.First().GetMethod("main");
        sw.Restart();
        method!.Invoke(null, []);
        Console.WriteLine($"Execution took {sw.Elapsed}");
        
        return 0;

        void PrintRes(List<PlampException> exList)
        {
            var fileBytes = File.ReadAllBytes(filepath);
            foreach (var ex in exList)
            {
                var start = Encoding.UTF8.GetString(fileBytes, 0, (int)ex.FilePosition.ByteOffset).Length;
                var lookupStart = Math.Clamp(start, 0, Math.Max(fileText.Length - 1, 0));
                var rowStart = fileText.LastIndexOf('\n', lookupStart);
                var rowEnd = fileText.IndexOf('\n', lookupStart + 1);
                rowStart = rowStart == -1 ? 0 : rowStart;
                rowEnd = rowEnd == -1 ? fileText.Length : rowEnd;
                
                var row = fileText[rowStart..rowEnd];
                var ptrStart = start - rowStart;
                var len = Math.Min(ex.FilePosition.CharacterLength, rowEnd - ptrStart + 1) - 1;
                len = len < 0 ? 0 : len;
                ptrStart = ptrStart < 1 ? 0 : ptrStart - 1;
                var str = $"{ex.Message}" + '\n' + row + '\n' + new string(' ',  ptrStart) + '^' + new string('~', len) + '\n';
                Console.WriteLine(str);
            }
        }
    }
}