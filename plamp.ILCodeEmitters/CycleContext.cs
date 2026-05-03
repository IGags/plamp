namespace plamp.ILCodeEmitters;

/// <summary>
/// Модель, которая хранит ассоциацию цикла с лейблами начала и конца в il коде.<br/>
/// Лейбл начала переносит на первую инструкцию принадлежащую циклу.<br/>
/// Лейбл конца переносит на первую инструкцию после цикла.
/// </summary>
/// <param name="StartLabel">Лейбл начала цикла(до условия)</param>
/// <param name="EndLabel">Лейбл первой инструкции после цикла</param>
internal record CycleContext(string StartLabel, string EndLabel);