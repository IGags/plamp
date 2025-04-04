using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Impl.BuiltRecords;
using plamp.Assembly.Impl.Models;
using plamp.Ast.Assemblies;

namespace plamp.Assembly.Impl.Builders;

public class PlampStaticAssemblyContainerBuilder : IStaticAssemblyContainerBuilder
{
    private readonly List<AssemblyBuilderInfo> _builders = [];

    public IStaticAssemblyContainer Build()
    {
        //TODO: shitty code
        var prepared = new List<PreparedAssembly>();
        foreach (var builder in _builders)
        {
            var types = new List<PreparedType>();
            foreach (var typeInfo in builder.AssemblyBuilder.Types)
            {
                var members = new List<PreparedMember>();
                foreach (var member in typeInfo.TypeBuilder.Members)
                {
                    var preparedMember = new PreparedMember(member.Member, member.Alias);
                    members.Add(preparedMember);
                }

                types.Add(new PreparedType(typeInfo.TypeBuilder.Type, typeInfo.NamespaceOverride, typeInfo.Alias,
                    members.ToArray()));
            }

            foreach (var enumInfo in builder.AssemblyBuilder.Enums)
            {
                var fields = new List<PreparedMember>();
                foreach (var field in enumInfo.TypeBuilder.Fields)
                {
                    var preparedField = new PreparedMember(field.Field, field.Alias);
                    fields.Add(preparedField);
                }

                types.Add(new PreparedType(enumInfo.TypeBuilder.EnumType, enumInfo.NamespaceOverride, enumInfo.Alias,
                    fields.ToArray()));
            }
            
            prepared.Add(new PreparedAssembly(builder.AssemblyBuilder.Assembly, builder.Alias, types.ToArray()));
        }
        
        return new DefaultStaticAssemblyContainer(prepared.ToArray());
    }

    public StaticAssemblyBuilder AddAssembly(System.Reflection.Assembly assembly, string alias = null)
    {
        alias ??= assembly.GetName().Name;
        if (assembly == null || assembly.IsDynamic) throw new ArgumentNullException(nameof(assembly));
        var builder = _builders.FirstOrDefault(x => x.Alias.Equals(alias));
        if (builder != default)
        {
            if (builder.AssemblyBuilder.Assembly != assembly)
                throw new InvalidOperationException($"The assembly with alias '{alias}' already exists.");
            return builder.AssemblyBuilder;
        }
        builder = _builders.FirstOrDefault(x => x.AssemblyBuilder.Assembly == assembly);
        if (builder != default)
        {
            if (!builder.Alias.Equals(alias))
            {
                throw new InvalidOperationException($"Assembly already exists with alias '{builder.Alias}'");
            }
            return builder.AssemblyBuilder;
        }
        
        var staticBuilder = new StaticAssemblyBuilder(assembly);
        var wrapper = new AssemblyBuilderInfo(staticBuilder, alias);
        _builders.Add(wrapper);
        return staticBuilder;
    }
}