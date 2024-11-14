using System.Collections.Generic;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;
using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.String;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal;

public class LiteralRuleBuilder : ILiteralRuleBuilder
{
    private string _trueKeyword;
    private string _falseKeyword;
    
    private readonly ITokenizationRuleBuilder _builder;

    private List<NumberType> _numberTypePrecedence = [
        NumberType.Int, 
        NumberType.Uint, 
        NumberType.Long, 
        NumberType.Ulong,
        NumberType.Double,
        NumberType.Float,
        NumberType.Short,
        NumberType.Ushort,
        NumberType.Byte
    ];
    
    private StringRuleBuilder _stringRuleBuilder;
    
    private INumberRuleBuilder<double> _doubleRuleBuilder;
    private INumberRuleBuilder<float> _floatRuleBuilder;
    private INumberRuleBuilder<int> _intRuleBuilder;
    private INumberRuleBuilder<byte> _byteRuleBuilder;
    private INumberRuleBuilder<uint> _uintRuleBuilder;
    private INumberRuleBuilder<ulong> _ulongRuleBuilder;
    private INumberRuleBuilder<long> _longRuleBuilder;
    private INumberRuleBuilder<short> _shortRuleBuilder;
    private INumberRuleBuilder<ushort> _ushortRuleBuilder;

    public LiteralRuleBuilder(ITokenizationRuleBuilder builder)
    {
        _builder = builder;
    }

    public ITokenizationRuleBuilder CompleteRule() => _builder;
    
    public void Validate()
    {
        _stringRuleBuilder?.Validate();
    }

    public IStringRuleBuilder ForString()
    {
        _stringRuleBuilder ??= new StringRuleBuilder(this);
        return _stringRuleBuilder;
    }

    public INumberRuleBuilder<double> ForDouble()
    {
        _doubleRuleBuilder ??= new NumberRuleBuilder<double>(this);
        return _doubleRuleBuilder;
    }

    public INumberRuleBuilder<float> ForFloat()
    {
        _floatRuleBuilder ??= new NumberRuleBuilder<float>(this);
        return _floatRuleBuilder;
    }

    public INumberRuleBuilder<int> ForInt()
    {
        _intRuleBuilder ??= new NumberRuleBuilder<int>(this);
        return _intRuleBuilder;
    }

    public INumberRuleBuilder<byte> ForByte()
    {
        _byteRuleBuilder ??= new NumberRuleBuilder<byte>(this);
        return _byteRuleBuilder;
    }

    public INumberRuleBuilder<uint> ForUint()
    {
        _uintRuleBuilder ??= new NumberRuleBuilder<uint>(this);
        return _uintRuleBuilder;
    }

    public INumberRuleBuilder<ulong> ForUlong()
    {
        _ulongRuleBuilder ??= new NumberRuleBuilder<ulong>(this);
        return _ulongRuleBuilder;
    }

    public INumberRuleBuilder<short> ForShort()
    {
        _shortRuleBuilder ??= new NumberRuleBuilder<short>(this);
        return _shortRuleBuilder;
    }

    public INumberRuleBuilder<ushort> ForUshort()
    {
        _ushortRuleBuilder ??= new NumberRuleBuilder<ushort>(this);
        return _ushortRuleBuilder;
    }

    public INumberRuleBuilder<long> ForLong()
    {
        _longRuleBuilder ??= new NumberRuleBuilder<long>(this);
        return _longRuleBuilder;
    }

    public ILiteralRuleBuilder WithNumberTypePrecedence(List<NumberType> typeOrder)
    {
        typeOrder ??= [];
        _numberTypePrecedence = typeOrder;
        return this;
    }

    public ILiteralRuleBuilder WithDefaultNumberTypePrecedence()
    {
        _numberTypePrecedence = [
            NumberType.Int, 
            NumberType.Uint, 
            NumberType.Long, 
            NumberType.Ulong,
            NumberType.Double,
            NumberType.Float,
            NumberType.Short,
            NumberType.Ushort,
            NumberType.Byte
        ];
        return this;
    }

    public ILiteralRuleBuilder WithBool(string @true, string @false)
    {
        if (string.IsNullOrWhiteSpace(@true))
        {
            throw new RuleValidationException("Empty true keyword");
        }

        if (string.IsNullOrWhiteSpace(@false))
        {
            throw new RuleValidationException("Empty false keyword");
        }

        if (@true.Equals(@false))
        {
            throw new RuleValidationException("True and false keyword is equals");
        }

        _trueKeyword = @true;
        _falseKeyword = @false;
        return this;
    }
}