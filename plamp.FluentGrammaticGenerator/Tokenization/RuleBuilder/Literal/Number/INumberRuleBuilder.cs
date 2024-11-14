using System;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;

public interface INumberRuleBuilder<T> : INumberBodyBuilder<T>, INumberPrefixBuilder<T>
{
    ILiteralRuleBuilder UsePattern(string pattern, NumberBuildDelegate<T> conversionFunc);

    INumberPrefixBuilder<T> UsePrefix(string prefix);
}