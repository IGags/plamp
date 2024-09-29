using System;
using Parser.Token;

namespace Parser.Ast;

public static class KeywordTranslator
{
    public static Keywords ToKeyword(this Word word)
    {
        return word.GetString().ToKeyword();
    }
    
    public static Keywords ToKeyword(this string word)
    {
        return word switch
        {
            "use" => Keywords.Use,
            "def" => Keywords.Def,
            "new" => Keywords.New,
            "false" => Keywords.False,
            "true" => Keywords.True,
            "for" => Keywords.For,
            "while" => Keywords.While,
            "if" => Keywords.If,
            "elif" => Keywords.Elif,
            "else" => Keywords.Else,
            "in" => Keywords.In,
            "null" => Keywords.Null,
            "return" => Keywords.Return,
            "break" => Keywords.Break,
            "continue" => Keywords.Continue,
            "model" => Keywords.Model,
            "var" => Keywords.Var,
            _ => Keywords.Unknown
        };
    }
}