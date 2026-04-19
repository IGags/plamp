using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Symbols.SymTable;

/// <summary>
/// Информация об объявлении функции в рамках модуля.
/// </summary>
public interface IFnInfo : IModuleMember, IEquatable<IFnInfo>
{
    /// <summary>
    /// Имя функции, должно быть уникально с точностью до перегрузки(две перегрузки могут иметь одно имя).<br/>
    /// В модуле не может быть функций с одинаковым числом дженериков и одинаковой сигнатурой с точностью до совпадения позиции дженерик параметра одинакового порядка.
    /// <code>
    /// Не скомпилируется<br/>
    /// fn A[b](f: any, s: b) {}<br/>
    /// fn A[c](f: any, s: c) {}<br/>
    /// Скомпилируется<br/>
    /// fn A[b](f: any, s: b) {}<br/>
    /// fn A[c](f: c, s: any) {}
    /// </code>
    /// </summary>
    public string Name { get; }
    
    public IReadOnlyList<IArgInfo> Arguments { get; }
    
    public ITypeInfo ReturnType { get; }

    public IReadOnlyList<ITypeInfo> GenericParams { get; }
    
    public MethodInfo AsFunc();
}