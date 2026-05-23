using System;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Позиция чего-либо в кодовом файле
/// </summary>
public readonly record struct FilePosition(long ByteOffset, long ByteLength, string FileName) : IComparable<FilePosition>
{
    /// <summary>
    /// Сравнение смещения относительно начала файла. Имя файла не учитывается.
    /// </summary>
    /// <param name="other">Другая позиция, с которой следует сравнивать</param>
    public int CompareTo(FilePosition other) => ByteOffset.CompareTo(other.ByteOffset);

    public static bool operator <(FilePosition left, FilePosition right) => left.CompareTo(right) < 0;

    public static bool operator >(FilePosition left, FilePosition right) => left.CompareTo(right) > 0;

    public override string ToString() => $"{FileName}: {ByteOffset} byte offset, {ByteLength} byte length";
}