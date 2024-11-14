using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Member;

public class MemberRuleBuilder : IMemberRuleBuilder
{
    private readonly List<Regex> _patterns = [];
    
    private readonly ITokenizationRuleBuilder _tokenizationRuleBuilder;

    public MemberRuleBuilder(ITokenizationRuleBuilder tokenizationRuleBuilder)
    {
        _tokenizationRuleBuilder = tokenizationRuleBuilder;
    }
    
    public void Validate()
    {
        throw new System.NotImplementedException();
    }

    public ITokenizationRuleBuilder ByPattern(string pattern)
    {
        var regex = new Regex(pattern);
        _patterns.Add(regex);
        return _tokenizationRuleBuilder;
    }

    public ITokenizationRuleBuilder UseDefaultRule()
    {
        var regex = new Regex("[a-zA-Z]{1}[a-zA-Z0-9]*");
        _patterns.Add(regex);
        return _tokenizationRuleBuilder;
    }
}