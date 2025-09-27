using System;

namespace plamp.Abstractions.Compilation.Models;

/// <summary>
/// Модель исходного файла, которая попадает на вход при компиляции
/// </summary>
/// <param name="FileName">Имя файла</param>
/// <param name="SourceCode">Содержимое файла</param>
public record struct SourceFile(string FileName, string SourceCode)
{
    private int? _stringHashCode;

    private int StringHashCode
    {
        get
        {
            _stringHashCode ??= SourceCode.GetHashCode();
            return _stringHashCode.Value;
        }
    }
    
    public override int GetHashCode() => HashCode.Combine(FileName, StringHashCode, GetType());
}