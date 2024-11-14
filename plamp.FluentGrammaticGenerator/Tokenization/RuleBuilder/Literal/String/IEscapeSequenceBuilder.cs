namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder.Literal.String;

public interface IEscapeSequenceBuilder
{
    ICompleteStringLiteralBuilder AddNewLineSequence(string sequence);
    
    ICompleteStringLiteralBuilder AddReturnСarriageSequence(string sequence);
    
    ICompleteStringLiteralBuilder AddTabSequence(string sequence);
}