using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Keyword;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Member;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Operator;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Other;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder;

internal class TokenizationRuleBuilder 
    : ITokenizationRuleBuilder
{
    
    
    public IMemberRuleBuilder ForMembers()
    {
        throw new System.NotImplementedException();
    }

    public IKeywordRuleBuilder ForKeywords()
    {
        throw new System.NotImplementedException();
    }

    public ILiteralRuleBuilder ForLiterals()
    {
        throw new System.NotImplementedException();
    }

    public IOperatorRuleBuilder ForOperators()
    {
        throw new System.NotImplementedException();
    }

    public IOtherRuleBuilder ForOthers()
    {
        throw new System.NotImplementedException();
    }

    public ITokenizationRuleBuilder AddTokenPair()
    {
        throw new System.NotImplementedException();
    }

    public IRule Build()
    {
        throw new System.NotImplementedException();
    }
}