namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.String;

public interface ILimiterBuilder : ICompleteStringLiteralBuilder
{
    IEscapeSequenceBuilder AddEscapeSequencePrefix(string prefix);
}