using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.String;

internal class StringRuleBuilder : IStringRuleBuilder, ILimiterBuilder, IEscapeSequenceBuilder
{
    private readonly ILiteralRuleBuilder _literalRuleBuilder;

    
    
    public HashSet<string> LimiterSet { get; } = [];

    public HashSet<string> EscapeSequencePrefix { get; } = [];

    public HashSet<string> NewLineSequence { get; } = [];

    public HashSet<string> ReturnCarriageSequence { get; } = [];

    public HashSet<string> TabSequence { get; } = [];

    public StringRuleBuilder(ILiteralRuleBuilder literalRuleBuilder)
    {
        _literalRuleBuilder = literalRuleBuilder;
    }
    
    public ILimiterBuilder AddLimiter(string limiter)
    {
        LimiterSet.Add(limiter);
        return this;
    }

    public IEscapeSequenceBuilder AddEscapeSequencePrefix(string prefix)
    {
        EscapeSequencePrefix.Add(prefix);
        return this;
    }

    public ICompleteStringLiteralBuilder AddNewLineSequence(string sequence)
    {
        NewLineSequence.Add(sequence);
        return this;
    }

    public ICompleteStringLiteralBuilder AddReturnСarriageSequence(string sequence)
    {
        ReturnCarriageSequence.Add(sequence);
        return this;
    }

    public ICompleteStringLiteralBuilder AddTabSequence(string sequence)
    {
        TabSequence.Add(sequence);
        return this;
    }

    public ILiteralRuleBuilder CompleteRule()
    {
        Validate();
        return _literalRuleBuilder;
    }

    public void Validate()
    {
        foreach (var escapeSequence in EscapeSequencePrefix)
        {
            ValidateSequence(LimiterSet, escapeSequence, "Escape sequence prefix {0} equals to limiter");
            ValidateSequence(NewLineSequence, escapeSequence, "Escape sequence prefix {0} equals to new line sequence");
            ValidateSequence(ReturnCarriageSequence, escapeSequence, "Escape sequence prefix {0} equals to return carpet sequence");
            ValidateSequence(TabSequence, escapeSequence, "Escape sequence prefix {0} equals to tab sequence");
        }

        foreach (var limiter in LimiterSet)
        {
            ValidateSequence(NewLineSequence, limiter, "Limiter {0} equals to new line sequence");
            ValidateSequence(ReturnCarriageSequence, limiter, "Limiter sequence prefix {0} equals to return carpet sequence");
            ValidateSequence(TabSequence, limiter, "Limiter sequence prefix {0} equals to tab sequence");
        }
        
        foreach (var newLine in NewLineSequence)
        {
            ValidateSequence(ReturnCarriageSequence, newLine, "New line sequence {0} equals to return carpet sequence");
            ValidateSequence(TabSequence, newLine, "New line sequence {0} equals to tab sequence");
        }

        foreach (var carriage in ReturnCarriageSequence)
        {
            ValidateSequence(TabSequence, carriage, "Return carriage sequence {0} equals to tab sequence");
        }
    }

    private void ValidateSequence(HashSet<string> sequence, string validator, string message)
    {
        foreach (var item in sequence)
        {
            if (validator.Equals(item, StringComparison.InvariantCulture))
            {
                throw new RuleValidationException(string.Format(message, item));
            }
        }
    }
}