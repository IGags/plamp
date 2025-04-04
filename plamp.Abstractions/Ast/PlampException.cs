using System;
using System.Reflection;

namespace plamp.Abstractions.Ast;

/// <summary>
/// A single class for every plamp error exclude runtime
/// </summary>
public class PlampException : Exception
{
    public string FileName { get; }
    public AssemblyName AssemblyName { get; }
    public FilePosition StartPosition { get; }
    public FilePosition EndPosition { get; }
    public int Code { get; }
    public ExceptionLevel Level { get; }

    public PlampException(PlampExceptionRecord exceptionFinalRecord, FilePosition startPosition, 
        FilePosition endPosition, string fileName = null, AssemblyName assemblyName = null) : base(exceptionFinalRecord.Message)
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
        FileName = fileName;
        AssemblyName = assemblyName;
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
            && Message.Equals(other.Message)
            && FileName.Equals(other.FileName);
    }

    public override string ToString()
    {
        return $"{Level} {Code}: {Message} From: {StartPosition} To: {EndPosition})";
    }
}