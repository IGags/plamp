using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Keyword;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Member;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Operator;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Other;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder;

/// <summary>
/// Builds the rules for tokenizer
/// </summary>
public interface ITokenizationRuleBuilder
{
    public IMemberRuleBuilder ForMembers();

    public IKeywordRuleBuilder ForKeywords();

    public ILiteralRuleBuilder ForLiterals();

    public IOperatorRuleBuilder ForOperators();

    public IOtherRuleBuilder ForOthers();

    public ITokenizationRuleBuilder AddTokenPair();

    public IRule Build();
}