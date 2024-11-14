using System.Collections.Generic;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Keyword;

internal class KeywordRuleBuilder : IKeywordRuleBuilder
{
    private readonly ITokenizationRuleBuilder _builder;

    public HashSet<string> KeywordTokens { get; } = new();

    public KeywordRuleBuilder(ITokenizationRuleBuilder builder)
    {
        _builder = builder;
    }

    public ITokenizationRuleBuilder CompleteRule() => _builder;
    
    public void Validate() { }

    public IKeywordRuleBuilder AddKeywords(params string[] keywords)
    {
        KeywordTokens.UnionWith(keywords);
        return this;
    }
}