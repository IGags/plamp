using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Alternative;
using plamp.ILCodeEmitters;

namespace plamp.Cli;

public static class Program
{
    public static async Task<int> Main(params string[] args)
    {
        if (args.Length != 1)
        {
            return -1;
        }

        var filepath = Path.GetFullPath(args[0]);
        await using var file = File.OpenRead(filepath);
        var sw = Stopwatch.StartNew();
        var (exceptions, symTable) = await CompilationPipeline.RunFrontendSteps(file, Encoding.UTF8, filepath);
     
        
        
        Console.WriteLine($"Compilation took {sw.Elapsed}");
        
        if (exceptions.Count > 0)
        {
            PrintResAsync(exceptions);
            return -1;
        }

        sw.Restart();
        var assemblyName = new AssemblyName(Guid.NewGuid().ToString("N"));
        var asm = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule(assemblyName.Name!);
        
        SymTableEmitter.EmitModule(symTable, module);
        
        module.CreateGlobalFunctions();        
        Console.WriteLine($"Emission took {sw.Elapsed}");
        
        var method = module.GetMethod("main");

        if (method == null)
        {
            throw new Exception("Method main is not defined in the module");
        }
        
        sw.Restart();
        method.Invoke(null, []);
        Console.WriteLine($"Execution took {sw.Elapsed}");
        
        return 0;

        async Task PrintResAsync(List<PlampException> exList)
        {
            var text = await File.ReadAllTextAsync(filepath, Encoding.UTF8);
            await using var fs = File.OpenRead(filepath);
            var lines = text.Split('\n');
            exList = exList.OrderBy(x => x.FilePosition).ToList();
            foreach (var ex in exList)
            {
                var rowColPos = await ToRowColLenFilePositionAsync(ex.FilePosition, fs, Encoding.UTF8);
                
                Console.WriteLine($"{rowColPos.Row + 1}:{rowColPos.Col + 1}:{rowColPos.Len} {ex.Message}");
                Console.WriteLine($"{rowColPos.Row,4} |");
                Console.WriteLine($"{rowColPos.Row + 1,4} | {lines[rowColPos.Row]}");
                var limit = Math.Min(rowColPos.Len, lines[rowColPos.Row].Length - rowColPos.Col);
                var prefixLen = rowColPos.Col;
                var prefix = prefixLen == 0 ? "" : new string(' ', (int)prefixLen);
                var postfix = limit <= 1 ? "" : new string('~', (int)limit - 1);
                Console.WriteLine($"{rowColPos.Row + 2,4} | " + prefix + "^" + postfix);
                Console.WriteLine();
            }
        }
    }
    
    private static async Task<RowColLenFilePosition> ToRowColLenFilePositionAsync(
        FilePosition position, 
        Stream stream, 
        Encoding encoding)
    {
        if(!stream.CanSeek) throw new ArgumentException("Stream must be seekable", nameof(stream));
        if(!stream.CanRead) throw new ArgumentException("Stream must be readable", nameof(stream));
        stream.Seek(0, SeekOrigin.Begin);
        var decoder = encoding.GetDecoder();

        var line = 0;
        var chr = 0;

        if (stream.Length < position.ByteLength + position.ByteOffset) throw new InvalidOperationException();
        var buffer = new byte[8192];
        var toRead = position.ByteOffset;

        while (toRead > 0)
        {
            var readAmt = (int)Math.Min(buffer.Length, toRead);
            var readCt = await stream.ReadAsync(buffer, 0, readAmt);
            toRead -= readCt;
            if(readCt == 0 && toRead != 0) throw new EndOfStreamException();
            var ct = encoding.GetCharCount(buffer, 0, readCt);
            var chars = new char[ct];
            var charCt = decoder.GetChars(buffer, 0, readCt, chars, 0);

            CountChars(ref line, ref chr, charCt, chars);
        }

        var finalCharCt = decoder.GetCharCount(Array.Empty<byte>(), true);
        if (finalCharCt > 0)
        {
            var chars = new char[finalCharCt];
            decoder.GetChars(Array.Empty<byte>(), 0, 0, chars, 0, true);
            CountChars(ref line, ref chr, finalCharCt, chars);
        }

        buffer = new byte[position.ByteLength];
        var lenReadCt = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (lenReadCt != buffer.Length) throw new EndOfStreamException();
        var len = encoding.GetCharCount(buffer, 0, lenReadCt);
        return new RowColLenFilePosition(line, chr, len);
    }

    private static void CountChars(ref int line, ref int chr, int charCt, char[] chars)
    {
        for (int i = 0; i < charCt; i++)
        {
            if (chars[i] == '\n')
            {
                line += 1;
                chr = 0;
            }
            else
            {
                chr += 1;
            }
        }
    }
}