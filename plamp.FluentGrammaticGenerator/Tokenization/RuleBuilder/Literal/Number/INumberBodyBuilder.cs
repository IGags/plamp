namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;

public interface INumberBodyBuilder<T> : INumberDelimiterBuilder<T>, IOptionalPostfixBuilder<T>
{
    public IOptionalPostfixBuilder<T> UsePostfix(string postfix);

    public INumberDelimiterBuilder<T> UseDelimiter(string delimiter);
}