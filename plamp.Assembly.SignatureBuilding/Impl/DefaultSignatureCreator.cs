using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.AssemblySignature;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Assembly.SignatureBuilding.Impl;

public class DefaultSignatureCreator : IAssemblySignatureCreator
{
    public async Task<SignatureBuildingResult> CreateAssemblySignatureAsync(
        NodeBase rootNode, 
        SignatureBuildingContext context,
        CancellationToken cancellationToken = default)
    {
        if (rootNode is not RootNode root)
        {
            //TODO: add warning
            throw new NotImplementedException();
        }
        
        var typeDefinitions = root.Nodes.OfType<TypeDefinitionNode>().ToList();
        var errorList = ValidateDuplicateSymbolNames(typeDefinitions, context);

        if (errorList.Any())
        {
            return new (null, errorList);
        }
        
        var assembly = await context.AssemblyBuilderCreator.CreateAssemblyBuilderAsync(
            context.AssemblyName,
            context.ModuleName,
            cancellationToken);

        var moduleName = assembly.GetModules().First().Name;
        var moduleBuilder = assembly.GetDynamicModule(moduleName)!;
        Debug.Assert(moduleBuilder == null);
        
        foreach (var typeDefinition in typeDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signatureCreationRes =
                await CreateTypeSignatureAsync(typeDefinition, moduleBuilder!, context, cancellationToken);
            errorList.AddRange(signatureCreationRes);
        }

        return new(assembly, errorList);
    }

    private List<PlampException> ValidateDuplicateSymbolNames(
        List<TypeDefinitionNode> typeDefinitions,
        SignatureBuildingContext context)
    {
        var typeNames = new HashSet<string>();

        foreach (var typeDefinition in typeDefinitions)
        {
            
        }
    }

    private Task<List<PlampException>> CreateTypeSignatureAsync(
        TypeDefinitionNode typeDefinition,
        ModuleBuilder moduleBuilder,
        SignatureBuildingContext context, 
        CancellationToken cancellationToken = default)
    {
        
    }
}