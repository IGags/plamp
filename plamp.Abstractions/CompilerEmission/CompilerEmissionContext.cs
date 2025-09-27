using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;

namespace plamp.Abstractions.CompilerEmission;

/// <summary>
/// Объект, применяющийся при эмиссии исходного кода в IL сборки.
/// </summary>
/// <param name="MethodBody">Тело функции, которую следует создать в IL</param>
/// <param name="MethodBuilder">Объект <see cref="T:System.Reflection.Emit.MethodBuilder"/></param>
/// <param name="Parameters">Список входных параметров для функции</param>
/// <param name="SymbolTable">Таблица сиволов(задел на будущее для дебага и прочей мешуры)</param>
//TODO: Следует перенести этот объект в ILCodeEmitters так как в общей библиотеке он не нужен вообще. А хранение его здесь сужает возможности по его модификации.
public record CompilerEmissionContext(
    BodyNode MethodBody,
    MethodBuilder MethodBuilder,
    ParameterInfo[] Parameters, 
    ISymbolTable? SymbolTable);