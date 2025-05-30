using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class DebugILGenerator : ILGenerator
{
    private readonly ILGenerator _inner;
    private int _varScopeCounter;

    //Не билдер так как нужно менять последнюю строку.
    private readonly List<string> _code = new();
    private readonly List<LocalBuilder> _locals = [];
    private readonly List<Label> _labels = [];

    internal DebugILGenerator(ILGenerator inner)
    {
        _inner = inner;
    }
    
    public override void BeginCatchBlock(Type? exceptionType) => throw new NotImplementedException();

    public override void BeginExceptFilterBlock() => throw new NotImplementedException();

    public override Label BeginExceptionBlock() => throw new NotImplementedException();

    public override void BeginFaultBlock() => throw new NotImplementedException();

    public override void BeginFinallyBlock() => throw new NotImplementedException();

    public override void BeginScope()
    {
        _code.Add($"BEGIN LEXICAL SCOPE {_varScopeCounter++}");
        _inner.BeginScope();
    }

    public override LocalBuilder DeclareLocal(Type localType, bool pinned)
    {
        _code.Add($"DECLARE LOCAL {_locals.Count} {localType.Name} PINNED: {pinned}");
        var local = _inner.DeclareLocal(localType, pinned);
        _locals.Add(local);
        return local;
    }

    public override Label DefineLabel()
    {
        var label = _inner.DefineLabel();
        _labels.Add(label);
        return label;
    }

    public override void Emit(OpCode opcode)
    {
        _code.Add(opcode.ToString() ?? throw new InvalidOperationException());
        _inner.Emit(opcode);
    }

    public override void Emit(OpCode opcode, byte arg)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, double arg)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, short arg)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, int arg)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, long arg)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, ConstructorInfo con)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {con}");
        _inner.Emit(opcode, con);
    }

    public override void Emit(OpCode opcode, Label label)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} lab_{_labels.IndexOf(label)}");
        _inner.Emit(opcode, label);
    }

    public override void Emit(OpCode opcode, Label[] labels)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var labConcat = string.Join(' ', labels.Select(x => $"lab_{_labels.IndexOf(x)}"));
        _code.Add($"{code} {labConcat}");
        _inner.Emit(opcode, labels);
    }

    public override void Emit(OpCode opcode, LocalBuilder local)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} loc_{_locals.IndexOf(local)}");
        _inner.Emit(opcode, local);
    }

    public override void Emit(OpCode opcode, SignatureHelper signature) => throw new NotImplementedException();

    public override void Emit(OpCode opcode, FieldInfo field)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {field}");
        _inner.Emit(opcode, field);
    }

    public override void Emit(OpCode opcode, MethodInfo meth)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {meth}");
        _inner.Emit(opcode, meth);
    }

    public override void Emit(OpCode opcode, float arg)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, string str)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {str}");
        _inner.Emit(opcode, str);
    }

    public override void Emit(OpCode opcode, Type cls)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.Add($"{code} {cls}");
        _inner.Emit(opcode, cls);
    }

    public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var opt = optionalParameterTypes == null ? string.Empty : string.Join(' ', optionalParameterTypes.Select(x => x.Name));
        _code.Add($"{code} {methodInfo}{(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCall(opcode, methodInfo, optionalParameterTypes);
    }

    public override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
        Type[]? optionalParameterTypes)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var args = parameterTypes?.Concat(optionalParameterTypes ?? []) ?? optionalParameterTypes;
        var opt = args == null ? string.Empty : string.Join(' ', args.Select(x => x.Name));
        var ret = returnType == null ? string.Empty : returnType.Name;
        _code.Add($"{code} {callingConvention} RET: {ret} ARGS: {(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
    }

    public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
    {
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var opt = parameterTypes == null ? string.Empty : string.Join(' ', parameterTypes.Select(x => x.Name));
        var ret = returnType == null ? string.Empty : returnType.Name;
        _code.Add($"{code} {unmanagedCallConv} RET: {ret} ARGS: {(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
    }

    public override void EndExceptionBlock() => throw new NotImplementedException();

    public override void EndScope()
    {
        _code.Add($"END LEXICAL SCOPE {--_varScopeCounter}");
        _inner.EndScope();
    }

    public override void MarkLabel(Label loc)
    {
        if (_code.Count != 0)
        {
            _code[^1] = $"lab_{_labels.IndexOf(loc)}: {_code[^1]}";
        }
        _inner.MarkLabel(loc);
    }

    public override void UsingNamespace(string usingNamespace) => throw new NotImplementedException();

    public override int ILOffset => _inner.ILOffset;

    public override string ToString() => string.Join('\n', _code);
}