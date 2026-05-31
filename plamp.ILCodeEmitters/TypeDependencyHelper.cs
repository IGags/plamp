using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.ILCodeEmitters;

/// <summary>
/// Сортирует типы в древо зависимостей внутри одного модуля
/// </summary>
public static class TypeDependencyHelper
{
    public static IReadOnlyList<ITypeBuilderInfo> OrderTypes(IReadOnlyList<ITypeBuilderInfo> types)
    {
        var depsDict = FindSameModuleDependencyForEachType(types);
        return GetTypeOrder(depsDict);
    }

    #region Order types

    public static IReadOnlyList<ITypeBuilderInfo> GetTypeOrder(Dictionary<ITypeBuilderInfo, HashSet<ITypeBuilderInfo>> depsDict)
    {
        if (depsDict.Count == 0) return [];
        var order = new List<ITypeBuilderInfo>();

        int prevOrder;
        do
        {
            prevOrder = order.Count;
            
            var keys = depsDict.Keys.ToList();
            //Знаю, что по словарю можно итерироваться и удалять и так, но это сделано для безопасности.
            foreach (var type in keys)
            {
                var deps = depsDict[type];
                deps.ExceptWith(order);
                if(deps.Count != 0) continue;

                depsDict.Remove(type);
                order.Add(type);
            }
        } while (prevOrder != order.Count);

        if (depsDict.Count > 0) throw new Exception("Не удалось создать все типы в сборке, скорее всего сборка имеет циклические зависимости.");
        return order;
    }

    #endregion

    #region Build dependencies

    public static Dictionary<ITypeBuilderInfo, HashSet<ITypeBuilderInfo>> FindSameModuleDependencyForEachType(
        IReadOnlyList<ITypeBuilderInfo> types)
    {
        var typeDependencies = new Dictionary<ITypeBuilderInfo, HashSet<ITypeBuilderInfo>>();

        foreach (var type in types)
        {
            var deps = new HashSet<ITypeBuilderInfo>();
            foreach (var fld in type.FieldBuilders)
            {
                var fldType = fld.FieldType;
                var fldDeps = GetFieldDeps(fldType, type);
                deps.UnionWith(fldDeps);
            }

            typeDependencies[type] = deps;
        }

        return typeDependencies;
    }

    public static IReadOnlyList<ITypeBuilderInfo> GetFieldDeps(ITypeInfo fieldType, ITypeInfo definingType)
    {
        if (fieldType.IsGenericTypeParameter) return [];
        if (fieldType.IsGenericTypeDefinition) throw new Exception("У поля не может быть тип равный объявлению дженерик типа");
        
        var elem = fieldType;
        while (elem.IsArrayType)
        {
            elem = elem.ElementType();
            if (elem == null) throw new Exception();
        }
        
        var deps = new List<ITypeBuilderInfo>();
        
        if (elem.IsGenericType)
        {
            var def = elem.GetGenericTypeDefinition();
            if (def == null) throw new Exception();
            var args = elem.GetGenericArguments();
            var otherDeps = args.SelectMany(x => GetFieldDeps(x, definingType));
            deps.AddRange(otherDeps);

            elem = def;
        }
        
        if (!definingType.Equals(elem)
            && elem is ITypeBuilderInfo elemBd
            && elemBd.ModuleName == definingType.ModuleName)
        {
            deps.Add(elemBd);
        }
        
        return deps;
    }

    #endregion
}