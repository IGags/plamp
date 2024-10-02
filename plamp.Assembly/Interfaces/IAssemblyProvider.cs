using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Assembly.Interfaces;

public interface IAssemblyProvider
{
    public List<string> GetAssemblyList();
    public Type GetTypeByName(string typeName, string assemblyName = null);
    public MethodInfo GetStaticMethod(string typeName, string methodName, List<Type> signature, string assembly = null);

    public MethodInfo GetDynamicMethodOrExtension(string typeName, string methodName, List<Type> signature,
        string assembly = null);

    public ConstructorInfo GetConstructor(string typeName, List<Type> signature, string assembly = null);

    //TODO: Определение оператора
    public MethodInfo GetOperator(string typeName);

    public PropertyInfo GetIndexer(string typeName, List<Type> signature);
}