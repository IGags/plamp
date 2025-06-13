using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace plamp.CodeEmission.Tests.Infrastructure;

public class DebugILGenerator : ILGenerator
{
    private readonly ILGenerator _inner;
    private int _varScopeCounter;
    private int _pos;

    //Не билдер так как нужно менять последнюю строку.
    private readonly StringBuilder _code = new();
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
        _code.AppendLine($"BEGIN LEXICAL SCOPE {_varScopeCounter++}");
        _inner.BeginScope();
        FinishStatement();
    }

    public override LocalBuilder DeclareLocal(Type localType, bool pinned)
    {
        _code.AppendLine($"DECLARE LOCAL {_locals.Count} {localType.Name} PINNED: {pinned}");
        var local = _inner.DeclareLocal(localType, pinned);
        _locals.Add(local);
        FinishStatement();
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
        AddLeadingSpaces();
        _code.AppendLine(opcode.ToString() ?? throw new InvalidOperationException());
        _inner.Emit(opcode);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, byte arg)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, double arg)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, short arg)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, int arg)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, long arg)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, ConstructorInfo con)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {con.Name}({string.Join(", ", con.GetParameters().Select(x => x.ParameterType.Name))})");
        _inner.Emit(opcode, con);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, Label label)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} lab_{_labels.IndexOf(label)}");
        _inner.Emit(opcode, label);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, Label[] labels)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var labConcat = string.Join(' ', labels.Select(x => $"lab_{_labels.IndexOf(x)}"));
        _code.AppendLine($"{code} {labConcat}");
        _inner.Emit(opcode, labels);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, LocalBuilder local)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} loc_{_locals.IndexOf(local)}");
        _inner.Emit(opcode, local);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, SignatureHelper signature)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {signature}");
        _inner.Emit(opcode, signature);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, FieldInfo field)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {field.DeclaringType}.{field.Name}");
        _inner.Emit(opcode, field);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, MethodInfo meth)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var infoStr = GetMethodInfoStringDesc(meth);
        _code.AppendLine($"{code} {infoStr}");
        _inner.Emit(opcode, meth);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, float arg)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {arg}");
        _inner.Emit(opcode, arg);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, string str)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} \"{str}\"");
        _inner.Emit(opcode, str);
        FinishStatement();
    }

    public override void Emit(OpCode opcode, Type cls)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        _code.AppendLine($"{code} {cls.Name}");
        _inner.Emit(opcode, cls);
        FinishStatement();
    }

    public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var opt = optionalParameterTypes == null ? string.Empty : string.Join(' ', optionalParameterTypes.Select(x => x.Name));
        var infoString = GetMethodInfoStringDesc(methodInfo);
        _code.AppendLine($"{code} {infoString}{(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCall(opcode, methodInfo, optionalParameterTypes);
        FinishStatement();
    }

    public override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
        Type[]? optionalParameterTypes)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var args = parameterTypes?.Concat(optionalParameterTypes ?? []) ?? optionalParameterTypes;
        var opt = args == null ? string.Empty : string.Join(' ', args.Select(x => x.Name));
        var ret = returnType == null ? string.Empty : returnType.Name;
        _code.AppendLine($"{code} {callingConvention} RET: {ret} ARGS: {(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        FinishStatement();
    }

    public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
    {
        AddLeadingSpaces();
        var code = opcode.ToString() ?? throw new InvalidOperationException();
        var opt = parameterTypes == null ? string.Empty : string.Join(' ', parameterTypes.Select(x => x.Name));
        var ret = returnType == null ? string.Empty : returnType.Name;
        _code.AppendLine($"{code} {unmanagedCallConv} RET: {ret} ARGS: {(opt == string.Empty ? opt : " " + opt)}");
        _inner.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
        FinishStatement();
    }

    public override void EndExceptionBlock() => throw new NotImplementedException();

    public override void EndScope()
    {
        _code.AppendLine($"END LEXICAL SCOPE {--_varScopeCounter}");
        _inner.EndScope();
        FinishStatement();
    }

    public override void MarkLabel(Label loc)
    {
        var ix = _labels.IndexOf(loc);
        var labStr = $"lab_{ix}:";
        _pos += labStr.Length;
        _code.Append(labStr);
        _inner.MarkLabel(loc);
    }

    public override void UsingNamespace(string usingNamespace) => throw new NotImplementedException();

    public override int ILOffset => _inner.ILOffset;

    public override string ToString() => _code.ToString();

    private void AddLeadingSpaces()
    {
        var spaceCount = Math.Max(10 - _pos, 0);
        _code.Append(' ', spaceCount);
    }

    private void FinishStatement() => _pos = 0;

    private string GetMethodInfoStringDesc(MethodInfo meth)
    {
        if (meth is MethodBuilder bd)
        {
            return $"{bd.ReturnType} {bd.Name} DYNAMIC BUILDER METHOD";
        }
        return $"{meth.ReturnType} {meth.DeclaringType}::{meth.Name}({string.Join(", ", meth.GetParameters().Select(x => x.ParameterType.Name))})";
    }
}