using System;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Позиция чего-либо в кодовом файле
/// </summary>
public readonly record struct FilePosition(long ByteOffset, int CharacterLength, string FileName) : IComparable<FilePosition>
{
    /// <summary>
    /// Сравнение смещения относительно начала файла. Имя файла не учитывается.
    /// </summary>
    /// <param name="other">Другая позиция, с которой следует сравнивать</param>
    public int CompareTo(FilePosition other) => ByteOffset.CompareTo(other.ByteOffset);

    
    public static FilePosition operator +(FilePosition left, FilePosition right)
    {
        if (!left.FileName.Equals(right.FileName)) throw new InvalidOperationException("Cannot sum offsets form the different files.");
        return left with { ByteOffset = left.ByteOffset + right.ByteOffset };
    }

    public static bool operator <(FilePosition left, FilePosition right) => left.CompareTo(right) < 0;

    public static bool operator >(FilePosition left, FilePosition right) => left.CompareTo(right) > 0;

    public override string ToString() => $"{FileName}: {ByteOffset} byte offset, {CharacterLength} character length";
}