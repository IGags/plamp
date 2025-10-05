using System;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Общий класс для любой ошибки компилляции языка
/// </summary>
public class PlampException : Exception
{
    /// <summary>
    /// Позиция в файле, с которой начинается ошибка. (включительно)
    /// </summary>
    public FilePosition FilePosition { get; }
    
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
    /// <param name="filePosition">Позиция начала ошибки в файле</param>
    /// <exception cref="ArgumentException">Позиция начала меньше позиции конца в файле</exception>
    public PlampException(
        PlampExceptionRecord exceptionFinalRecord, 
        FilePosition filePosition) 
        : base(exceptionFinalRecord.Message)
    {
        FilePosition = filePosition;
        Code = exceptionFinalRecord.Code;
        Level = exceptionFinalRecord.Level;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not PlampException other) return false;

        return
            FilePosition.Equals(other.FilePosition)
            && Code == other.Code
            && Level == other.Level
            && Message.Equals(other.Message);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePosition, Code, (int)Level);
    }

    public override string ToString()
    {
        return $"{Level} {Code}: {Message} From: {FilePosition})";
    }
}