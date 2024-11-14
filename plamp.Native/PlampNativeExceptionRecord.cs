namespace plamp.Native;

/// <summary>
/// Exception template
/// </summary>
public record PlampNativeExceptionRecord(string Message, int Code, ExceptionLevel Level)
{
    public PlampNativeExceptionFinalRecord Format(params string[] inlines) => new()
    {
        Message = string.Format(Message, inlines),
        Code = Code,
        Level = Level
    };
}

/// <summary>
/// Inlined exception
/// </summary>
public record PlampNativeExceptionFinalRecord()
{
    public required string Message { get; init; }

    public required int Code { get; init; }
    
    public required ExceptionLevel Level { get; init; }
}