namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;

public interface INumberPrefixBuilder<T>
{
    INumberBodyBuilder<T> UseBody(string allowedCharacters, string ranges = null);
}