namespace plamp.Alternative.Tokenization.Enums;

public static class KeywordTranslator
{
    public static Keywords ToKeyword(this string word)
    {
        return word switch
        {
            "use" => Keywords.Use,
            "fn" => Keywords.Fn,
            "as" => Keywords.As,
            "false" => Keywords.False,
            "true" => Keywords.True,
            "while" => Keywords.While,
            "if" => Keywords.If,
            "else" => Keywords.Else,
            "null" => Keywords.Null,
            "return" => Keywords.Return,
            "break" => Keywords.Break,
            "continue" => Keywords.Continue,
            "model" => Keywords.Model,
            "module" => Keywords.Module,
            _ => Keywords.Unknown
        };
    }
}