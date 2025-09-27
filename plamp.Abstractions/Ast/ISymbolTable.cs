using System.Collections.Generic;
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
    /// <param name="fileName">Имя файла, в котором находится узел.</param>
    /// <remarks>Имя файла избыточно. При генерации таблицы символа можно ассоциировать узел с именем файла</remarks>
    PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord, string fileName);

    /// <summary>
    /// Создаёт единый объект ошибки для списка узлов AST. При этом максимум ошибки - максимальная позиция в файле среди узлов AST, минимум - минимальная.
    /// </summary>
    /// <param name="nodes">Список узлов, на который надо наложить ошибку</param>
    /// <param name="exceptionRecord">Шаблон, из которого требуется собрать объект ошибки</param>
    /// <param name="fileName">Имя файла, в котором находится узел.</param>
    /// <remarks>Имя файла избыточно. При генерации таблицы символа можно ассоциировать узел с именем файла</remarks>
    //TODO: Возвращать null, если узлы отсутствуют, а не бросать ошибку. Это позволит вызывающему коду действовать более гибко.
    PlampException SetExceptionToNodeRange(List<NodeBase> nodes, PlampExceptionRecord exceptionRecord, string fileName);
    
    ///<summary>
    /// Попытка получения позиции в файле по узлу из таблицы.
    /// </summary>
    /// <param name="symbol">Узел AST для которого требуется найти позицию</param>
    /// <param name="pair">Пара позиций. Позиция начала и позиция конца в файле</param>
    /// <returns>True - если удалось найти узел и его позицию, иначе false</returns>
    bool TryGetSymbol(NodeBase symbol, out KeyValuePair<FilePosition, FilePosition> pair);

    /// <summary>
    /// Добавление узла AST в таблицу символов.
    /// </summary>
    /// <param name="symbol">Узел ast.</param>
    /// <param name="start">Начальная позиция в файле</param>
    /// <param name="end">Конечная позиция в файле</param>
    /// <exception cref="T:System.ArgumentException">Ошибка, если стартовая узла позиция больше конечной.</exception>
    /// <exception cref="T:System.ArgumentException">Узел уже присутствует в таблице.</exception>
    void AddSymbol(NodeBase symbol, FilePosition start, FilePosition end);
}