using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Compiler.Model;
using plamp.Compiler.Util;

namespace plamp.Compiler;

internal class AssemblyTreeBuilder
{
    private static readonly AssemblyNameEqualityComparer AssemblyNameEqualityComparer = new();
    private readonly Dictionary<AssemblyName, AssemblyTreeNode> _ready = new(AssemblyNameEqualityComparer);
    private readonly Dictionary<AssemblyName, AssemblyTreeNode> _notComplete = new(AssemblyNameEqualityComparer);
    private readonly Queue<ReadOnlyAssemblyTreeNode> _jobQueue = [];
    
    public bool TryBuildAssemblyTree(
        List<CompilationAssemblyModel> assemblies,
        IAssemblyContainer assemblyContainer,
        out Queue<ReadOnlyAssemblyTreeNode> jobQueue,
        out List<PlampException> errors)
    {
        jobQueue = _jobQueue;
        errors = [];
        
        var error = false;
        var previousCount = -1;
        
        while (assemblies.Any())
        {
            if (assemblies.Count == previousCount)
            {
                error = true;
                ExcludeDependencyErrorAssemblies(assemblies, errors, assemblyContainer);
            }
            var toRemove = FindAssembliesWithAllDependencies(assemblies, assemblyContainer);
            foreach (var assemblyModel in toRemove)
            {
                assemblies.Remove(assemblyModel);
            }
            previousCount = assemblies.Count;
        }

        return !error;
    }

    private List<CompilationAssemblyModel> FindAssembliesWithAllDependencies(
        List<CompilationAssemblyModel> assemblies,
        IAssemblyContainer assemblyContainer)
    {
        var complete = new List<CompilationAssemblyModel>();
        foreach (var assembly in assemblies)
        {
            GetAssemblyTreeNode(
                assembly, 
                out var remainReferences, 
                out var model);
            
            var allFound = TryFindAllRemainAssemblyReferences(
                assemblyContainer,
                remainReferences,
                out var foundRemainReferenceResult);
            
            model.StaticReferences.AddRange(foundRemainReferenceResult.StaticReferences);
            model.DynamicReferences.AddRange(foundRemainReferenceResult.DynamicReferences);
            model.SourceFiles.UnionWith(assembly.SourceFiles);
                
            if (allFound)
            {
                _ready[assembly.Name] = model;
                _jobQueue.Enqueue(model);
                _notComplete.Remove(assembly.Name);
                complete.Add(assembly);
            }
            else
            {
                model.RemainReferences.UnionWith(foundRemainReferenceResult.NotFound);
                _notComplete[assembly.Name] = model;
            }
        }

        return complete;
    }

    private void GetAssemblyTreeNode(
        CompilationAssemblyModel assembly,
        out HashSet<AssemblyName> remainReferences,
        out AssemblyTreeNode model)
    {
        if (_notComplete.TryGetValue(assembly.Name, out model))
        {
            remainReferences = model.RemainReferences;
            model.RemainReferences.Clear();
        }
        else
        {
            model = new AssemblyTreeNode()
            {
                Name = assembly.Name,
                RemainReferences = []
            };
                    
            remainReferences = assembly.References;
        }
    }

    private bool TryFindAllRemainAssemblyReferences(
        IAssemblyContainer assemblyContainer, 
        HashSet<AssemblyName> remainReferences,
        out ReferenceFindResult result)
    {
        result = default;
        var allFound = true;
                
        foreach (var reference in remainReferences)
        {
            Assembly staticAssembly;
            // if ((staticAssembly = assemblyContainer.GetAssembly(reference)) != null)
            // {
            //     result.StaticReferences.Add(staticAssembly);
            //     continue;
            // }

            if (_ready.TryGetValue(reference, out var dynamicAssembly))
            {
                result.DynamicReferences.Add(dynamicAssembly);    
                continue;
            }
                    
            allFound = false;
            result.NotFound.Add(reference);
        }

        return allFound;
    }
    
    private void ExcludeDependencyErrorAssemblies(
        List<CompilationAssemblyModel> assemblies,
        List<PlampException> errors,
        IAssemblyContainer container)
    {
        var toRemove = new List<CompilationAssemblyModel>();
        var cycles = DetectCycles(assemblies);
        var missingDeps = FindMissingDependencies(container, assemblies);
                
        foreach (var cycle in cycles)
        {
            foreach (var assembly in cycle.Cycle)
            {
                toRemove.Add(assembly);
                var model = new AssemblyTreeNode()
                {
                    Name = assembly.Name
                };
                _ready[assembly.Name] = model;
            }
            errors.AddRange(cycle.Errors);
        }

        foreach (var missingDependency in missingDeps)
        {
            toRemove.Add(missingDependency.Assembly);
            var model = new AssemblyTreeNode()
            {
                Name = missingDependency.Assembly.Name
            };
            _ready[missingDependency.Assembly.Name] = model;
            errors.AddRange(missingDependency.Errors);
        }

        foreach (var assembly in toRemove)
        {
            assemblies.Remove(assembly);
        }
    }

    private List<CycleDependencyErrorRecord> DetectCycles(List<CompilationAssemblyModel> assemblies)
    {
        var marked = new HashSet<AssemblyName>(AssemblyNameEqualityComparer);
        var detectionStack = new Stack<CompilationAssemblyModel>();
        var toDetect = new Stack<CompilationAssemblyModel>();
        var cycleRecords = new List<CycleDependencyErrorRecord>();
        foreach (var assembly in assemblies)
        {
            if (marked.Contains(assembly.Name)) continue;

            toDetect.Push(assembly);
            
            while (toDetect.Any())
            {
                var current = toDetect.Peek();
                var mark = TryCheckDependencyCycle(marked, current, detectionStack, assemblies, toDetect, cycleRecords);
                if (mark)
                {
                    marked.Add(current.Name);
                    toDetect.Pop();
                    if (!detectionStack.TryPeek(out var top) || top != current) continue;
                    
                    detectionStack.Pop();
                }
                else
                {
                    detectionStack.Push(current);
                }
            }
        }

        return cycleRecords;
    }

    private bool TryCheckDependencyCycle(
        HashSet<AssemblyName> marked,
        CompilationAssemblyModel currentModel,
        Stack<CompilationAssemblyModel> detectionStack,
        List<CompilationAssemblyModel> assemblies,
        Stack<CompilationAssemblyModel> toDetect,
        List<CycleDependencyErrorRecord> cycleRecords)
    {
        var mark = true;
        foreach (var reference in currentModel.References)
        {
            if (marked.Contains(reference)) continue;
            if (detectionStack.Any(x => AssemblyNameEqualityComparer.Equals(x.Name, reference)))
            {
                marked.Add(reference);
                var dependencyCycle = detectionStack
                    .TakeWhile(x => !AssemblyNameEqualityComparer.Equals(x.Name, reference));
                var currentAssembly =
                    assemblies.First(x => AssemblyNameEqualityComparer.Equals(x.Name, reference));
                var dependencyCycleList = dependencyCycle.Concat([currentAssembly]).ToList();

                var cycleNames = dependencyCycleList.Select(x => x.Name.Name);
                var errorRecord = PlampAssemblyExceptions.CircularDependency(cycleNames.ToArray());
                var error = new PlampException(errorRecord, default, default, null, null);
                var dependencyRecord = new CycleDependencyErrorRecord(dependencyCycleList, [error]);
                cycleRecords.Add(dependencyRecord);
            }

            var found = assemblies.FirstOrDefault(x => AssemblyNameEqualityComparer.Equals(x.Name, reference));

            if (found == null) continue;
            toDetect.Push(found);
            mark = false;
        }

        return mark;
    }

    private List<MissingDependencyAssemblyRecord> FindMissingDependencies(
        IAssemblyContainer container,
        List<CompilationAssemblyModel> assemblies)
    {
        var errors = new List<MissingDependencyAssemblyRecord>();
        // foreach (var assembly in assemblies)
        // {
        //     var missingRefs = new List<PlampException>();
        //     foreach (var reference in assembly.References)
        //     {
        //         if (container.GetAssembly(reference) != null 
        //             || _ready.ContainsKey(reference) 
        //             || assemblies.Any(x => AssemblyNameEqualityComparer.Equals(x.Name, reference))) 
        //             continue;
        //         
        //         var exceptionRecord = PlampAssemblyExceptions.MissingDependency(assembly.Name.Name, reference.Name);
        //         var exception = new PlampException(exceptionRecord, default, default, null, assembly.Name);
        //         missingRefs.Add(exception);
        //     }
        //
        //     if (missingRefs.Any())
        //     {
        //         errors.Add(new MissingDependencyAssemblyRecord(assembly, missingRefs));
        //     }
        // }

        return errors;
    }

    private record struct ReferenceFindResult(
        List<Assembly> StaticReferences,
        List<AssemblyTreeNode> DynamicReferences,
        List<AssemblyName> NotFound);
    
    private record struct MissingDependencyAssemblyRecord(CompilationAssemblyModel Assembly, List<PlampException> Errors);

    private record struct CycleDependencyErrorRecord(List<CompilationAssemblyModel> Cycle, List<PlampException> Errors);
}