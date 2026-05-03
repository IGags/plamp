using System;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

public interface IGenericParameterBuilder : ITypeInfo
{
    /// <summary>
    /// Тип дженерик параметра у объявления типа.
    /// После его установки любые попытки модификации состяния этого класса должны завершаться с <see cref="InvalidOperationException"/>
    /// </summary>
    public Type? GenericParameterType { get; set; }
    
    /// <summary>
    /// Тип-билдер параметра, служит для построения полей типа, после установки <see cref="GenericParameterType"/> становится null
    /// А обращение к нему запрещается через <see cref="InvalidOperationException"/>
    /// </summary>
    public GenericTypeParameterBuilder? ParameterBuilder { get; set; }
}