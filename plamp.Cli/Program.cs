using plamp.Abstractions.Ast;
using plamp.Abstractions.Compilation.Models;
using plamp.Alternative;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;

namespace plamp.Cli;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Target file does not set");
        }
        var file = File.ReadAllText("/home/roma/plamp-snippets/valid.plp");
        var rows = file.Split('\n');
        var tokens = Tokenizer.Tokenize(new SourceFile("/home/roma/plamp-snippets/valid.plp", file));
        if (args.Contains("-t"))
        {
            PrintRes(tokens.Exceptions);
            foreach (var token in tokens.Sequence)
            {
                Console.WriteLine($"@@ {token.Start}, {token.End} @@ {token.GetStringRepresentation()}");
            }
            return;
        }

        var context = new ParsingContext(tokens.Sequence, file, tokens.Exceptions, new SymbolTable());
        var parsed = Parser.ParseFile(context);
        rows = rows.Select(x => x.Replace("\t", "    ")).ToArray();
        PrintRes(context.Exceptions);

        void PrintRes(List<PlampException> exList)
        {
            foreach (var ex in exList)
            {
                var row = rows[ex.StartPosition.Row];
                var str = $"@@ {ex.StartPosition.Row}, {ex.StartPosition.Column} @@ {ex.Message}" + '\n' + row + '\n';
                str += new string(' ', ex.StartPosition.Column) + $"^\n@@ {ex.EndPosition.Row}, {ex.EndPosition.Column} @@";
                Console.WriteLine(str);
            }
        }
    }
}