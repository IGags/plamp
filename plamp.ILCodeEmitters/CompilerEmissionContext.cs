using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.ILCodeEmitters;

/// <summary>
/// Объект, применяющийся при эмиссии исходного кода в IL сборки.
/// </summary>
/// <param name="MethodBody">Тело функции, которую следует создать в IL</param>
/// <param name="MethodBuilder">Объект <see cref="T:System.Reflection.Emit.MethodBuilder"/></param>
/// <param name="Parameters">Список входных параметров для функции</param>
/// <param name="SymbolTable">Таблица сиволов(задел на будущее для дебага и прочей мешуры)</param>
public record CompilerEmissionContext(
    BodyNode MethodBody,
    MethodBuilder MethodBuilder,
    ParameterInfo[] Parameters, 
    ITranslationTable? SymbolTable);