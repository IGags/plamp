using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization;

/// <summary>
/// Inherit for generate tokenization grammatic
/// </summary>
public abstract class TokenizerBase<T> where T : TokenBase
{
    /// <summary>
    /// Rule definition
    /// </summary>
    protected ITokenizationRuleBuilder Rule { get; } = new TokenizationRuleBuilder();

    public IRule MakeTokenizer()
    {
        return Rule
            .ForKeywords().AddKeywords("Privet", "Poka", "Zdravstvuite").CompleteRule()
            .ForLiterals()
                .ForByte().UsePattern("[0-9]+b", byte.TryParse)
                .ForInt().UseBody(null, "0-9").AddOptionalPostfix("i").CompleteRule(int.TryParse)
                .ForString().AddLimiter("\"").AddEscapeSequencePrefix("\\")
                    .AddNewLineSequence("n").CompleteRule()
                .WithBool("yes", "no")
            .CompleteRule()
            .ForMembers().ByPattern("[a-zA-Z]+[a-zA-Z0-9]")
            .Build();
    }
}