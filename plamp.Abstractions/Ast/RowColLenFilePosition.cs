namespace plamp.Abstractions.Ast;

/// <summary>
/// Позиция в файле через указание строки, столбца и длины с учётом кодировки файла
/// </summary>
/// <param name="Row">Строка</param>
/// <param name="Col">Столбец</param>
/// <param name="Len">Длина</param>
public readonly record struct RowColLenFilePosition(long Row, long Col, long Len);