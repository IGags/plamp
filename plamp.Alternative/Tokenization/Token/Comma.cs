﻿using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization.Token;

public class Comma : TokenBase
{
    public Comma(FilePosition start, FilePosition end) : base(start, end, ",")
    {
    }
}