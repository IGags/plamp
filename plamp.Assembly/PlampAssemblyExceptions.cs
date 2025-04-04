using plamp.Ast;

namespace plamp.Assembly;

public static class PlampAssemblyExceptions
{
    //Assembly exceptions 3000-3499
    public static PlampExceptionRecord DuplicateAssemblyName(string assemblyName) =>
        new()
        {
            Message = $"Duplicate module name: {assemblyName}",
            Code = 3000,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CircularDependency(params string[] assemblies) =>
        new()
        {
            Message = $"A list of modules: {string.Join(", ", assemblies)} creates circular dependency.",
            Code = 3001,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord DuplicateMethodDefinition(string methodName) =>
        new()
        {
            Message = $"Duplicate method signature with name: {methodName}",
            Code = 3002,
            Level = ExceptionLevel.Error
        };
}