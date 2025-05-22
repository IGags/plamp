using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.AssemblySignature;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.NodeComparers;

namespace plamp.Assembly.SignatureBuilding.Impl;

public class DefaultSignatureCreator : IAssemblySignatureCreator
{
    private static readonly RecursiveComparer RecursiveComparer = new();
    
    public async Task<SignatureBuildingResult> CreateAssemblySignatureAsync(
        List<NodeBase> rootNodes, 
        SignatureBuildingContext context,
        CancellationToken cancellationToken = default)
    {
    //     if (rootNode is not RootNode root)
    //     {
    //         throw new Exception("Node must be a RootNode");
    //     }
    //     
    //     var typeDefinitions = root.Nodes.OfType<TypeDefinitionNode>().ToList();
    //     var errorList = ValidateDuplicateSymbolNames(typeDefinitions, context);
    //
    //     if (errorList.Any())
    //     {
    //         return new (null, errorList);
    //     }
    //     
    //     var assembly = await context.AssemblyBuilderCreator.CreateAssemblyBuilderAsync(
    //         context.AssemblyName,
    //         context.ModuleName,
    //         cancellationToken);
    //
    //     var moduleName = assembly.GetModules().First().Name;
    //     var moduleBuilder = assembly.GetDynamicModule(moduleName)!;
    //     Debug.Assert(moduleBuilder == null);
    //     
    //     foreach (var typeDefinition in typeDefinitions)
    //     {
    //         cancellationToken.ThrowIfCancellationRequested();
    //         var signatureCreationRes =
    //             await CreateTypeSignatureAsync(typeDefinition, moduleBuilder!, context, cancellationToken);
    //         errorList.AddRange(signatureCreationRes);
    //     }
    //
    //     return new(assembly, errorList);
    // }
    //
    // private async Task<List<PlampException>> CreateSingleRootSignature()
    // {
    //     
    // }
    //
    // #region Validation
    //
    // private List<PlampException> ValidateDuplicateSymbolNames(
    //     List<TypeDefinitionNode> typeDefinitions,
    //     SignatureBuildingContext context)
    // {
    //     var exceptions = new List<PlampException>();
    //     var nameGroups = typeDefinitions.GroupBy(x => x.Name, RecursiveComparer);
    //     var duplicateNames = nameGroups.Where(x => x.Count() > 1).ToList();
    //     
    //     foreach (var duplicateName in duplicateNames.SelectMany(x => x))
    //     {
    //         var exceptionRecord = SignatureCreationExceptionInfo.DuplicateTypeNameRecord;
    //         
    //         var exception = context.SymbolTable.SetExceptionToNodeWithoutChildren(
    //             exceptionRecord, 
    //             duplicateName, 
    //             context.FileName, 
    //             context.AssemblyName);
    //         
    //         exceptions.Add(exception);
    //     }
    //
    //     foreach (var typeDefinition in typeDefinitions)
    //     {
    //         exceptions.AddRange(ValidateDuplicateMemberName(typeDefinition, context));
    //     }
    //     
    //     return exceptions;
    // }
    //
    // private List<PlampException> ValidateDuplicateMemberName(
    //     TypeDefinitionNode typeDefinition, 
    //     SignatureBuildingContext context)
    // {
    //     var members = typeDefinition.Members;
    //     if (members == null) return [];
    //
    //     var memberDict = new Dictionary<string, List<NodeBase>>();
    //     
    //     foreach (var member in members)
    //     {
    //         if(member == null) continue;
    //         var name = GetMemberName(member);
    //         if(name == null) continue;
    //
    //         if (memberDict.ContainsKey(name))
    //         {
    //             memberDict[name].Add(member);
    //         }
    //         else
    //         {
    //             memberDict[name] = [member];
    //         }
    //     }
    //
    //     var exceptions = new List<PlampException>();
    //     
    //     foreach (var member in memberDict.Where(x => x.Value.Count > 1).SelectMany(x => x.Value))
    //     {
    //         var exceptionRecord = SignatureCreationExceptionInfo.DuplicateMemberNameRecord;
    //
    //         var exception = context.SymbolTable.SetExceptionToNodeWithoutChildren(
    //             exceptionRecord,
    //             member,
    //             context.FileName,
    //             context.AssemblyName);
    //         
    //         exceptions.Add(exception);
    //     }
    //     
    //     return exceptions;
    // }
    //
    // private string? GetMemberName(NodeBase member)
    // {
    //     switch (member)
    //     {
    //         case DefNode defNode:
    //             var memberName = defNode.Name as MemberNode;
    //             var name = memberName?.MemberName;
    //             return name;
    //         default: throw new Exception("Unknown member type");
    //     }
    // }
    //
    // #endregion
    //
    // private async Task<List<PlampException>> CreateTypeSignatureAsync(
    //     TypeDefinitionNode typeDefinition,
    //     ModuleBuilder moduleBuilder,
    //     SignatureBuildingContext context, 
    //     CancellationToken cancellationToken = default)
    // {
    //     var typName = typeDefinition.Name;
    //     if (typName is not MemberNode memberName)
    //     {
    //         var exRecord = SignatureCreationExceptionInfo.InvalidTypeName;
    //         var ex = context.SymbolTable.SetExceptionToNodeWithoutChildren(
    //             exRecord, 
    //             typName, 
    //             context.FileName, 
    //             context.AssemblyName);
    //         return [ex];
    //     }
    //
    //     moduleBuilder.DefineType(memberName.MemberName,
    //         TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
    //     
    //     var exceptionList = new List<PlampException>();
    //     foreach (var member in typeDefinition.Members)
    //     {
    //         
    //     }
    // }
    //
    // private async Task<List<PlampException>> CreateMemberSignature()
    // {
    //     
    // }
        return new(default, default);
    }
}