using System;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Общий класс для любой ошибки компилляции языка
/// </summary>
public class PlampException : Exception
{
    /// <summary>
    /// Имя исходного файла, в котором произошла ошибка
    /// </summary>
    public string FileName { get; }
    
    /// <summary>
    /// Позиция в файле, с которой начинается ошибка. (включительно)
    /// </summary>
    public FilePosition StartPosition { get; }
    
    /// <summary>
    /// Позиция в файле, на котором ошибка заканчивается. (включительно)
    /// </summary>
    public FilePosition EndPosition { get; }
    
    /// <summary>
    /// Код ошибки.
    /// </summary>
    public string Code { get; }
    
    /// <summary>
    /// Уровень серьёзности ошибки
    /// </summary>
    public ExceptionLevel Level { get; }

    /// <summary>
    /// Создание экземпляра ошибки
    /// </summary>
    /// <param name="exceptionFinalRecord">Шаблон, по которому форматируется ошибка</param>
    /// <param name="startPosition">Позиция начала ошибки в файле</param>
    /// <param name="endPosition">Позиция конца ошибки в файле</param>
    /// <param name="fileName">Имя исходного файла, где произошла ошибка</param>
    /// <exception cref="ArgumentException">Позиция начала меньше позиции конца в файле</exception>
    public PlampException(
        PlampExceptionRecord exceptionFinalRecord, 
        FilePosition startPosition, 
        FilePosition endPosition, 
        string fileName) 
        : base(exceptionFinalRecord.Message)
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
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not PlampException other) return false;

        return
            StartPosition.Equals(other.StartPosition)
            && EndPosition.Equals(other.EndPosition)
            && Code == other.Code
            && Level == other.Level
            && Message.Equals(other.Message)
            && FileName.Equals(other.FileName);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, StartPosition, EndPosition, Code, (int)Level);
    }

    public override string ToString()
    {
        return $"{Level} {Code}: {Message} From: {StartPosition} To: {EndPosition})";
    }
}