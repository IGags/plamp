using System;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Позиция чего-либо в кодовом файле
/// </summary>
//TODO: Следует добавить байтовое смещение в файле. Также рассмотрел бы переход на другую структуру с хранением имени файла.
public readonly record struct FilePosition(int Row, int Column) : IComparable<FilePosition>
{
    /// <summary>
    /// Сравнение позиции.(при чтении строк сверху вниз, слева направо)
    /// </summary>
    /// <param name="other">Другая позиция, с которой следует сравнивать</param>
    public int CompareTo(FilePosition other)
    {
        if (Row == other.Row && Column == other.Column)
        {
            return 0;
        }

        if (Row < other.Row || (Row == other.Row && Column < other.Column))
        {
            return -1;
        }

        return 1;
    }
    
    public static FilePosition operator +(FilePosition left, FilePosition right)
    {
        return new(left.Row + right.Row, left.Column + right.Column);
    }

    public static bool operator <(FilePosition left, FilePosition right)
    {
        return left.CompareTo(right) < 0;
    }
    
    public static bool operator >(FilePosition left, FilePosition right)
    {
        return left.CompareTo(right) > 0;
    }

    public override string ToString()
    {
        return $"{Row}:{Column}";
    }
}