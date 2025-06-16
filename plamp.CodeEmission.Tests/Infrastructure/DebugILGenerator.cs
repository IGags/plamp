using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class DebugILGenerator : ILGenerator
{
    private readonly ILGenerator _inner;
    private int _varScopeCounter;

    //Не билдер так как нужно менять последнюю строку.
    private readonly StringBuilder _code = new();
    private readonly List<LocalBuilder> _locals = [];
    private readonly List<Label> _labels = [];
    private readonly List<string> _unwrittenLabels = [];

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
        _code.AppendLine($"BEGIN LEXICAL SCOPE {_varScopeCounter++}");
        _inner.BeginScope();
    }

    public override LocalBuilder DeclareLocal(Type localType, bool pinned)
    {
        _code.AppendLine($"DECLARE LOCAL {_locals.Count} {localType.Name} PINNED: {pinned}");
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
        WritePrefix();
        _code.AppendLine(opcode.ToString() ?? throw new InvalidOperationException());
        _inner.Emit(opcode);
    }

    public override void Emit(OpCode opcode, byte arg)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, double arg)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, short arg)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, int arg)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, long arg)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, ConstructorInfo con)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {con.Name}({string.Join(", ", con.GetParameters().Select(x => x.ParameterType.Name))})");
        _inner.Emit(opcode, con);
    }

    public override void Emit(OpCode opcode, Label label)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} lab_{_labels.IndexOf(label)}");
        _inner.Emit(opcode, label);
    }

    public override void Emit(OpCode opcode, Label[] labels)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var labConcat = string.Join(' ', labels.Select(x => $"lab_{_labels.IndexOf(x)}"));
        _code.AppendLine($"{code} {labConcat}");
        _inner.Emit(opcode, labels);
    }

    public override void Emit(OpCode opcode, LocalBuilder local)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} loc_{_locals.IndexOf(local)}");
        _inner.Emit(opcode, local);
    }

    public override void Emit(OpCode opcode, SignatureHelper signature)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {signature}");
        _inner.Emit(opcode, signature);
    }

    public override void Emit(OpCode opcode, FieldInfo field)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {field.DeclaringType}.{field.Name}");
        _inner.Emit(opcode, field);
    }

    public override void Emit(OpCode opcode, MethodInfo meth)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var infoStr = GetMethodInfoStringDesc(meth);
        _code.AppendLine($"{code} {infoStr}");
        _inner.Emit(opcode, meth);
    }

    public override void Emit(OpCode opcode, float arg)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
    }

    public override void Emit(OpCode opcode, string str)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} \"{str}\"");
        _inner.Emit(opcode, str);
    }

    public override void Emit(OpCode opcode, Type cls)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {cls.Name}");
        _inner.Emit(opcode, cls);
    }

    public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var opt = optionalParameterTypes == null ? string.Empty : string.Join(' ', optionalParameterTypes.Select(x => x.Name));
        var infoString = GetMethodInfoStringDesc(methodInfo);
        _code.AppendLine($"{code} {infoString}{(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCall(opcode, methodInfo, optionalParameterTypes);
    }

    public override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
        Type[]? optionalParameterTypes)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var args = parameterTypes?.Concat(optionalParameterTypes ?? []) ?? optionalParameterTypes;
        var opt = args == null ? string.Empty : string.Join(' ', args.Select(x => x.Name));
        var ret = returnType == null ? string.Empty : returnType.Name;
        _code.AppendLine($"{code} {callingConvention} RET: {ret} ARGS: {(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
    }

    public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
    {
        WritePrefix();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var opt = parameterTypes == null ? string.Empty : string.Join(' ', parameterTypes.Select(x => x.Name));
        var ret = returnType == null ? string.Empty : returnType.Name;
        _code.AppendLine($"{code} {unmanagedCallConv} RET: {ret} ARGS: {(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
    }

    public override void EndExceptionBlock() => throw new NotImplementedException();

    public override void EndScope()
    {
        _code.AppendLine($"END LEXICAL SCOPE {--_varScopeCounter}");
        _inner.EndScope();
    }

    public override void MarkLabel(Label loc)
    {
        _inner.MarkLabel(loc);
        _unwrittenLabels.Add($"lab_{_labels.IndexOf(loc)}:");
    }

    public override void UsingNamespace(string usingNamespace) => throw new NotImplementedException();

    public override int ILOffset => _inner.ILOffset;

    public override string ToString() => _code.ToString();

    private void WritePrefix()
    {
        if (_unwrittenLabels.Count != 0)
        {
            foreach (var label in _unwrittenLabels)
            {
                _code.AppendLine(label);
            }
            _unwrittenLabels.Clear();
        }
        
        _code.Append(' ', 6);
    }

    private string GetMethodInfoStringDesc(MethodInfo meth)
    {
        if (meth is MethodBuilder bd)
        {
            return $"{bd.ReturnType} {bd.Name} DYNAMIC BUILDER METHOD";
        }
        return $"{meth.ReturnType} {meth.DeclaringType}::{meth.Name}({string.Join(", ", meth.GetParameters().Select(x => x.ParameterType.Name))})";
    }
}