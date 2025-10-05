using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Отвечает за связь узлов AST и позиционирования исходном тексте программы.
/// </summary>
//TODO: Ассоциировать узел AST с файлом при вставке, а не при генерации ошибки
public interface ISymbolTable
{
    /// <summary>
    /// Создаёт объект ошибки с перезаписанной позицией в исходном файле на основании узла AST. Если узла AST нет - поднимает исключение 
    /// </summary>
    /// <param name="node">Узел AST, для которого следует сгенерировать ошибку</param>
    /// <param name="exceptionRecord">Шаблон, из которого требуется собрать объект ошибки</param>
    /// <remarks>Имя файла избыточно. При генерации таблицы символа можно ассоциировать узел с именем файла</remarks>
    PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord);
    
    ///<summary>
    /// Попытка получения позиции в файле по узлу из таблицы.
    /// </summary>
    /// <param name="symbol">Узел AST для которого требуется найти позицию</param>
    /// <param name="position">Позиция узла в кодовом файле.</param>
    /// <returns>True - если удалось найти узел и его позицию, иначе false</returns>
    bool TryGetSymbol(NodeBase symbol, out FilePosition position);

    /// <summary>
    /// Добавление узла AST в таблицу символов.
    /// </summary>
    /// <param name="symbol">Узел ast.</param>
    /// <param name="position">Начальная позиция в файле</param>
    /// <exception cref="T:System.ArgumentException">Ошибка, если стартовая узла позиция больше конечной.</exception>
    /// <exception cref="T:System.ArgumentException">Узел уже присутствует в таблице.</exception>
    void AddSymbol(NodeBase symbol, FilePosition position);
}