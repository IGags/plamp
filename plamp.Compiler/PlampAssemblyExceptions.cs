using plamp.Abstractions.Ast;

namespace plamp.Compiler;

public static class PlampAssemblyExceptions
{
    //Assembly exceptions 3000-3499
    public static PlampExceptionRecord DuplicateAssemblyName(string assemblyName) =>
        new()
        {
            Message = $"Duplicate assembly name: {assemblyName}",
            Code = "CMP3000",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CircularDependency(params string[] assemblies) =>
        new()
        {
            Message = $"A list of assemblies: {string.Join(", ", assemblies)} creates circular dependency.",
            Code = "CMP3001",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingDependency(string assemblyName, string dependencyName) =>
        new()
        {
            Message = $"Missing dependency: {dependencyName} for {assemblyName}",
            Code = "CMP3002",
            Level = ExceptionLevel.Error
        };
}