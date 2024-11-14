using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Base;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Keyword;

public interface IKeywordRuleBuilder : IRuleExtensions
{
    public IKeywordRuleBuilder AddKeywords(params string[] keywords);
}