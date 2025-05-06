using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.AssemblySignature;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Compilation.Models.ApiGeneration;
using plamp.Abstractions.CompilerEmission;
using plamp.ILCodeEmitters.Helper;

namespace plamp.ILCodeEmitters;

public class DefaultAssemblySignatureCreator : IAssemblySignatureCreator
{
    public Task<AssemblyApiGenerators> CreateAssemblySignatureAsync(
        List<NodeBase> topLevelNodes,
        ICompiledAssemblyContainer compiledAssemblyContainer, 
        ISymbolTable symbolTable, 
        AssemblyName assemblyName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetApiGenerators(topLevelNodes, assemblyName, compiledAssemblyContainer));
    }
    
    private AssemblyApiGenerators GetApiGenerators(
        List<NodeBase> topLevelNodes, 
        AssemblyName assemblyName,
        ICompiledAssemblyContainer compiledAssemblyContainer)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeList = MakeTypeDefinitions(moduleBuilder, topLevelNodes);
        
        var methodList = new List<MethodEmitterPair>();
        foreach (var type in typeList)
        {
            methodList.AddRange(EmitMemberSignatures(
                type, 
                compiledAssemblyContainer,
                typeList));
        }

        return new AssemblyApiGenerators(assemblyBuilder, moduleBuilder, typeList, methodList);
    }


    private List<MethodEmitterPair> EmitMemberSignatures(
        TypeEmitterPair typeEmitterPair, 
        ICompiledAssemblyContainer compiledAssemblyContainer,
        List<TypeEmitterPair> typeEmitters)
    {
        var emitters = new List<MethodEmitterPair>();
        var members = typeEmitterPair.TypeDefinition.TypeMembers;
        foreach (var member in members)
        {
            switch (member)
            {
                case DefNode defNode:
                    emitters.Add(EmitMethodSignature(typeEmitterPair, defNode, compiledAssemblyContainer, typeEmitters));
                    break;
                default:
                    throw new NotSupportedException("Cannot emit other members");
            }
        }

        return emitters;
    }
    
    private List<TypeEmitterPair> MakeTypeDefinitions(ModuleBuilder moduleBuilder, List<NodeBase> topLevelNodes)
    {
        var typeList = new List<TypeEmitterPair>();
        foreach (var topLevelNode in topLevelNodes)
        {
            var typ = MakeTypeDefinition(topLevelNode);
            var fullName = typ.Name + '.' + typ.Namespace;
            var typeEmitter = moduleBuilder.DefineType(fullName,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            GenerateDefaultCtor(typeEmitter);
            typeList.Add(new TypeEmitterPair(typeEmitter, typ));
        }

        return typeList;
    }
    
    private void GenerateDefaultCtor(TypeBuilder typeBuilder)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public, 
            CallingConventions.HasThis, 
            Type.EmptyTypes);
        
        ctorBuilder.SetImplementationFlags(MethodImplAttributes.IL);
        var generator = ctorBuilder.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ret);
    }

    private TypeDefinition MakeTypeDefinition(NodeBase node)
    {
        if(node is not TypeDefinitionNode typeDef) throw new ArgumentException("topLevelNode is not type definition");
        if(typeDef.Name is not MemberNode name) throw new ArgumentException("typeDef.Name is not member");
        if(typeDef.Namespace is not MemberNode @namespace) throw new ArgumentException("typeDef.Namespace is not member");
        if(!typeDef.Generics.All(x => x is TypeNode)) throw new ArgumentException("typeDef.Generics.Any(x => x is not type)");
        var generics = typeDef.Generics.Cast<TypeNode>().ToList();
        if(generics.Any()) throw new NotSupportedException("Generics is not supported yet");
        return new TypeDefinition(name.MemberName, @namespace.MemberName, generics, typeDef.Members);
    }
    
    private MethodEmitterPair EmitMethodSignature(
        TypeEmitterPair typeEmitter,
        DefNode defNode,
        ICompiledAssemblyContainer compiledAssemblyContainer,
        List<TypeEmitterPair> typeEmitters)
    {
        var typeName = MemberNameResolveHelper.ParseTypeName(defNode.ReturnType);
        var returnType = MemberNameResolveHelper.FindType(typeName, compiledAssemblyContainer, typeEmitters);
        var args = new List<ArgDefinition>(defNode.ParameterList.Count);
        foreach (var arg in defNode.ParameterList)
        {
            args.Add(ParseArg(arg, compiledAssemblyContainer, typeEmitters));
        }

        if (defNode.Name is not MemberNode methodName)
            throw new NotSupportedException("Method name should be a member");
        var name = MemberNameResolveHelper.ParseMember(methodName);
        var emitter = typeEmitter.TypeBuilder.DefineMethod(
            name,
            MethodAttributes.Final | MethodAttributes.Public,
            CallingConventions.Any,
            returnType,
            args.Select(x => x.TypeDef).ToArray());
        var result = new MethodDefinition(name, returnType, args, [], defNode.Body);
        return new MethodEmitterPair(emitter, result);
    }

    private ArgDefinition ParseArg(
        NodeBase arg,
        ICompiledAssemblyContainer compiledAssemblyContainer,
        List<TypeEmitterPair> currentAssembly)
    {
        if(arg is not ParameterNode parameter) throw new NotSupportedException("Parameter type should be a parameter node");
        if(parameter.Name is not MemberNode paramName) throw new NotSupportedException("Parameter name should be a member");
        var paramTypeName = MemberNameResolveHelper.ParseTypeName(parameter.Type);
        var name = MemberNameResolveHelper.ParseMember(paramName);
        var type = MemberNameResolveHelper.FindType(paramTypeName, compiledAssemblyContainer, currentAssembly);
        return new ArgDefinition(type, name);
    }
}