using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Base;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Member;

public interface IMemberRuleBuilder
{
    ITokenizationRuleBuilder ByPattern(string pattern);

    ITokenizationRuleBuilder UseDefaultRule();
}