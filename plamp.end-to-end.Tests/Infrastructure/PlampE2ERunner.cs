using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Alternative;
using plamp.ILCodeEmitters;
using plamp.ILCodeEmitters.EmissionDebug;

namespace plamp.EndToEnd.Tests.Infrastructure;

/// <summary>
/// Фасад e2e тестов над полным пайплайном сборки: чтение .plp файла, компиляция, IL эмиссия и подготовка методов к вызову
/// </summary>
public static class PlampE2ERunner
{
    /// <summary>
    /// Компилирует .plp файл и возвращает объект для вызова сгенерированных методов
    /// </summary>
    /// <param name="relativePath">Путь к .plp файлу относительно папки CodeForTests</param>
    public static async Task<CompiledPlampProgram> CompileFromCodeForTestsAsync(string relativePath)
    {
        var filePath = ResolveCodeForTestsPath(relativePath);
        return await CompileAsync(filePath);
    }

    /// <summary>
    /// Запускает только сборку
    /// </summary>
    /// <param name="relativePath">Путь к .plp файлу относительно папки CodeForTests</param>
    /// <returns>Ошибки токенизации, парсинга, построения таблицы символов</returns>
    public static async Task<IReadOnlyList<PlampException>> GetDiagnosticsAsync(string relativePath)
    {
        var filePath = ResolveCodeForTestsPath(relativePath);
        await using var file = File.OpenRead(filePath);
        using var reader = new StreamReader(file, Encoding.UTF8, leaveOpen: true);

        var (exceptions, _) = await CompilationPipeline.RunFrontendSteps(reader, filePath);
        return exceptions;
    }

    /// <summary>
    /// Компилирует .plp файл
    /// </summary>
    /// <param name="filePath">Полный путь до файла</param>
    /// <returns>Скомпилированная программа</returns>
    private static async Task<CompiledPlampProgram> CompileAsync(string filePath)
    {
        await using var file = File.OpenRead(filePath);
        using var reader = new StreamReader(file, Encoding.UTF8, leaveOpen: true);

        var (exceptions, symTable) = await CompilationPipeline.RunFrontendSteps(reader, filePath);
        if (exceptions.Count > 0)
        {
            throw new E2ECompilationException(filePath, exceptions);
        }

        var assemblyName = new AssemblyName(Guid.NewGuid().ToString("N"));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName.Name!);
        var moduleBuilder = new IlDumpModuleBuilder(module);

        SymTableEmitter.EmitModule(symTable, moduleBuilder);
        moduleBuilder.CreateGlobalFunctions();

        return new CompiledPlampProgram(filePath, module, moduleBuilder.GetIlDump());
    }

    /// <summary>
    /// Сборка полного пути по относительному пути
    /// </summary>
    /// <param name="relativePath">Путь до файла относительно CodeForTests</param>
    private static string ResolveCodeForTestsPath(string relativePath)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "CodeForTests", relativePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Не найден e2e .plp файл: {filePath}", filePath);
        }

        return filePath;
    }
}
