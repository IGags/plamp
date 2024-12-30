using System;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;

namespace plamp.Native;

/// <summary>
/// A single class for every plamp error exclude runtime
/// </summary>
public class PlampException : Exception
{
    public TokenPosition StartPosition { get; }
    public TokenPosition EndPosition { get; }
    public int Code { get; }
    public ExceptionLevel Level { get; }

    public PlampException(PlampNativeExceptionFinalRecord exceptionFinalRecord, TokenPosition startPosition, 
        TokenPosition endPosition) : base(exceptionFinalRecord.Message)
    {
        if (startPosition.CompareTo(endPosition) == 1)
        {
            //Funny
            throw new ArgumentException("Start position cannot be lesser than end position");
        }

        StartPosition = startPosition;
        EndPosition = endPosition;
        Code = exceptionFinalRecord.Code;
        Level = exceptionFinalRecord.Level;
    }
    
    public PlampException(PlampNativeExceptionFinalRecord exceptionFinalRecord, TokenBase token) 
        : base(exceptionFinalRecord.Message)
    {
        StartPosition = token.Start;
        EndPosition = token.End;
        Code = exceptionFinalRecord.Code;
        Level = exceptionFinalRecord.Level;
    }
    
    public override bool Equals(object obj)
    {
        if (obj == null || obj is not PlampException other)
        {
            return false;
        }
        return 
            StartPosition.Equals(other.StartPosition) 
            && EndPosition.Equals(other.EndPosition) 
            && Code == other.Code 
            && Level == other.Level
            && Message.Equals(other.Message);
    }

    public override string ToString()
    {
        return $"{Level} {Code}: {Message} From: {StartPosition} To: {EndPosition})";
    }
}