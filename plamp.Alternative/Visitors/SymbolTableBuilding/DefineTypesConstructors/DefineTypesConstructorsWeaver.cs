using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.DefineTypesConstructors;

/// <summary>
/// Цель этого вивера создать AST дефолтные конструкторы для типов объявленных в модуле.
/// </summary>
public class DefineTypesConstructorsWeaver : BaseWeaver<SymbolTableBuildingContext, SymbolTableBuildingContext>
{
    protected override SymbolTableBuildingContext CreateInnerContext(SymbolTableBuildingContext context) 
        => context;

    protected override SymbolTableBuildingContext MapInnerToOuter(
        SymbolTableBuildingContext innerContext, 
        SymbolTableBuildingContext outerContext) 
        => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, SymbolTableBuildingContext context, NodeBase? parent)
    {
        //Если тип не находится в контексте компиляции модуля, то этот визитор не применяется.
        if (parent is not RootNode) return VisitResult.SkipChildren;
        
        var type = context.SymTableBuilder.ListTypes().FirstOrDefault(x => x.Name == node.Name.Value);
        
        // Если тип не найден в таблице символов (это происходит в результате ошибки валидации типа), то он пропускается.
        if (type == null)
        {
            return VisitResult.SkipChildren;
        }
        
        var typeName = new TypeNameNode(type.Name);
        //TODO: Дженерики
        //Узел обязан иметь ссылку на себя в таблице символов так как этап построения типов в таблице символов уже прошёл.
        var typeUnderConstruction = new TypeNode(typeName) { TypeInfo = type };
        var ctorName = $"<{type.Name}_ctor>";

        var paramName = "t";
        var parameter = new ParameterNode(typeUnderConstruction, new ParameterNameNode(paramName));
        var body = CreateConstructorBodyAst(node, paramName);
        
        /*
         * fn <MyType_ctor>(t: MyType) MyType {
         *   ...
         * }
         */
        var function = new FuncNode(typeUnderConstruction, new FuncNameNode(ctorName), [parameter], body);

        var funcInfo = context.SymTableBuilder.DefineFunc(function);
        type.Constructor = funcInfo;
        
        return VisitResult.SkipChildren;
    }
    
    private BodyNode CreateConstructorBodyAst(TypedefNode type, string instanceParamName)
    {
        var expressions = new List<NodeBase>();

        foreach (var field in type.Fields)
        {
            var fieldTypeInfo = field.FieldType.TypeInfo;
            
            //Если у этого поля нет информации о типе, значит при его валидации нашлась ошибка. (Либо тип не известен, либо у поля некорректное имя)
            if(fieldTypeInfo == null) continue;

            var fieldAccess = new FieldAccessNode(new MemberNode(instanceParamName), new FieldNode(field.Name.Value));

            if (TryInitAsArray(fieldAccess, fieldTypeInfo, out var expression)) {}
            else if (TryInitAsTypeInstance(fieldAccess, fieldTypeInfo, out expression)) {}

            //Если не нашлось способа инициализировать поле, то это ошибка компилятора
            if (expression == null)
            {
                throw new Exception($"Не удалось создать инициализатор для поля {field.Name.Value} типа {type.Name.Value}, проверьте корректность кода компилятора");
            }
            
            expressions.Add(expression);
        }

        return new BodyNode(expressions);
    }

    private bool TryInitAsArray(
        FieldAccessNode target,
        ITypeInfo fieldTypeInfo, 
        [NotNullWhen(true)] out AssignNode? fieldInitExpression)
    {
        fieldInitExpression = null;
        if (!fieldTypeInfo.IsArrayType) return false;

        var itemType = fieldTypeInfo.ElementType();
        if (itemType == null) throw new Exception("Тип представился типом массива, но его элемент невозможно получить");

        var typeNode = MakeTypeNodeFromInfo(itemType);       

        var initArray = new InitArrayNode(typeNode, new LiteralNode(0, Builtins.Int));
        fieldInitExpression = new AssignNode([target], [initArray]);
        return true;
    }

    private bool TryInitAsTypeInstance(
        FieldAccessNode target,
        ITypeInfo fieldTypeInfo, 
        [NotNullWhen(true)] out AssignNode? fieldInitExpression)
    {
        var typeNode = MakeTypeNodeFromInfo(fieldTypeInfo);
        var initType = new InitTypeNode(typeNode, []);

        fieldInitExpression = new AssignNode([target], [initType]);
        return true;
    }

    private TypeNode MakeTypeNodeFromInfo(ITypeInfo info)
    {
        var typeName = new TypeNameNode(info.Name);
        var typeNode = new TypeNode(typeName) { TypeInfo = info };
        return typeNode;
    }
}