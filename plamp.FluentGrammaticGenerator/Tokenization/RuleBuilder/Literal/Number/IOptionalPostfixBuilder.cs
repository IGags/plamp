using plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.String;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.Number;

public interface IOptionalPostfixBuilder<T> : ICompleteNumberLiteralRuleBuilder<T>
{
    public IOptionalPostfixBuilder<T> AddOptionalPostfix(string optionalPostfix);
}