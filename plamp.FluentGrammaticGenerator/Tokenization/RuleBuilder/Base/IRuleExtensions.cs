using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Base;

public interface IRuleExtensions
{
    public ITokenizationRuleBuilder CompleteRule();

    public void Validate();
}