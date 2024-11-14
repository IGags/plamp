using System;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;

public interface ICompleteNumberLiteralRuleBuilder<T>
{
    ILiteralRuleBuilder CompleteRule(NumberBuildDelegate<T> conversionFunc);
}