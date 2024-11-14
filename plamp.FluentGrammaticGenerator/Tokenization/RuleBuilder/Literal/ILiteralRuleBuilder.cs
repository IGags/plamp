using System.Collections.Generic;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Base;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.String;
using plamp.FluentGrammaticGenerator.Tokenization.Token;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal;

public interface ILiteralRuleBuilder : IRuleExtensions
{
    public IStringRuleBuilder ForString();
    
    public INumberRuleBuilder<double> ForDouble();

    public INumberRuleBuilder<float> ForFloat();

    public INumberRuleBuilder<int> ForInt();

    public INumberRuleBuilder<byte> ForByte();

    public INumberRuleBuilder<uint> ForUint();

    public INumberRuleBuilder<ulong> ForUlong();

    public INumberRuleBuilder<short> ForShort();

    public INumberRuleBuilder<ushort> ForUshort();
    
    public INumberRuleBuilder<long> ForLong();

    public ILiteralRuleBuilder WithNumberTypePrecedence(List<NumberType> typeOrder);

    public ILiteralRuleBuilder WithDefaultNumberTypePrecedence();

    public ILiteralRuleBuilder WithBool(string @true, string @false);
}