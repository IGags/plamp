using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;

internal partial class NumberRuleBuilder<T> : INumberRuleBuilder<T>
{
    [GeneratedRegex(@"\s")]
    private partial Regex WhiteSpaceRegex();

    public readonly List<KeyValuePair<Regex, NumberBuildDelegate<T>>> Conversions = [];
    
    private string _currentRuleBuilderString = "";
    private string _currentOptionalPostfix = "";
    
    private readonly ILiteralRuleBuilder _literalRuleBuilder;

    public NumberRuleBuilder(ILiteralRuleBuilder literalRuleBuilder)
    {
        _literalRuleBuilder = literalRuleBuilder;
    }

    public ILiteralRuleBuilder CompleteRule(NumberBuildDelegate<T> conversionFunc)
    {
        _currentRuleBuilderString = $"{_currentRuleBuilderString}(?:{_currentOptionalPostfix})?";
        var pattern = new Regex(_currentRuleBuilderString);
        Conversions.Add(new(pattern, conversionFunc));
        _currentRuleBuilderString = "";
        _currentOptionalPostfix = "";
        return _literalRuleBuilder;
    }

    public IOptionalPostfixBuilder<T> AddOptionalPostfix(string optionalPostfix)
    {
        if (string.IsNullOrWhiteSpace(optionalPostfix))
        {
            throw new RuleValidationException("Empty number optional postfix");
        }
        
        _currentOptionalPostfix += _currentOptionalPostfix.Length == 0
            ? $"|{Regex.Escape(optionalPostfix)}"
            : Regex.Escape(optionalPostfix);
        
        return this;
    }

    public IOptionalPostfixBuilder<T> UsePostfix(string postfix)
    {
        if (string.IsNullOrWhiteSpace(postfix))
        {
            throw new RuleValidationException("Empty number postfix");
        }
        
        _currentRuleBuilderString += $"(?:{Regex.Escape(postfix)})";
        return this;
    }

    public INumberDelimiterBuilder<T> UseDelimiter(string delimiter)
    {
        if (string.IsNullOrWhiteSpace(delimiter))
        {
            throw new RuleValidationException("Empty number delimiter");
        }
        
        _currentRuleBuilderString += Regex.Escape(delimiter);
        return this;
    }

    public ILiteralRuleBuilder UsePattern(string pattern, NumberBuildDelegate<T> conversionFunc)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new RuleValidationException("Empty number pattern");
        }

        Conversions.Add(new (new Regex(pattern), conversionFunc));
        return _literalRuleBuilder;
    }

    public INumberPrefixBuilder<T> UsePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new RuleValidationException("Empty number prefix");
        }

        _currentRuleBuilderString += $"(?:{Regex.Escape(prefix)})";
        return this;
    }

    public INumberBodyBuilder<T> UseBody(string allowedCharacters, string ranges = null)
    {
        if (string.IsNullOrWhiteSpace(allowedCharacters) || string.IsNullOrWhiteSpace(ranges))
        {
            throw new RuleValidationException("Empty number body characters");
        }

        if (WhiteSpaceRegex().IsMatch(allowedCharacters))
        {
            throw new RuleValidationException("Number body characters cannot contain white spaces");
        }

        _currentRuleBuilderString += $"[{Regex.Escape(allowedCharacters)}{ranges}]+";
        return this;
    }

    public void Validate() { }
}