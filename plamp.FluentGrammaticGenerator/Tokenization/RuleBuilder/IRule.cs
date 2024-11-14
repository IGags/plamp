using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder;

public interface IRule
{
    /// <summary>
    /// Make token sequence for parsing from source code
    /// </summary>
    public TokenSequence Tokenize(string sourceCode);
}