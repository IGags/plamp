using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation;
using Shouldly;

namespace plamp.Abstractions.Tests.Ast;

public class BaseVisitorTests
{
    class UniversalContext() : BaseVisitorContext(null!, null!)
    {
        public int ModuleNameVisitCt { get; set; }

        public int FuncDefVisitCt { get; set; }

        public int TypeDefVisitCt { get; set; }

        public int BodyVisitCt { get; set; }
    }

    class GuardVisitor : BaseVisitor<UniversalContext>
    {
        protected override VisitResult PreVisitModuleDefinition(ModuleDefinitionNode definition, UniversalContext context, NodeBase? parent)
        {
            context.ModuleNameVisitCt++;
            return VisitResult.Continue;
        }

        protected override VisitResult PreVisitFunction(FuncNode node, UniversalContext context, NodeBase? parent)
        {
            context.FuncDefVisitCt++;
            return VisitResult.Continue;
        }

        protected override VisitResult PreVisitBody(BodyNode node, UniversalContext context, NodeBase? parent)
        {
            context.BodyVisitCt++;
            return VisitResult.Continue;
        }

        protected override VisitResult PreVisitTypedef(TypedefNode node, UniversalContext context, NodeBase? parent)
        {
            context.TypeDefVisitCt++;
            return VisitResult.Continue;
        }

        public void Visit(NodeBase node, UniversalContext context) => VisitNodeBase(node, context, null);
    }
    
    private readonly NodeBase _ast = new RootNode(
        moduleName: new ModuleDefinitionNode("124"),
        imports: [],
        functions:
        [
            new FuncNode(new TypeNode(new TypeNameNode("123")), new FuncNameNode("fsafsafsa"), [], [],
                new BodyNode([]))
        ],
        types:
        [
            new TypedefNode(new("Abc"), [], [])
        ]
    );
    
    [Fact]
    public void VisitAstWithAllGuard_Correct()
    {
        var ctx = new UniversalContext();
        var visitor = new GuardVisitor();

        visitor.Visit(_ast, ctx);
        
        ctx.BodyVisitCt.ShouldBe(1);
        ctx.ModuleNameVisitCt.ShouldBe(1);
        ctx.FuncDefVisitCt.ShouldBe(1);
        ctx.TypeDefVisitCt.ShouldBe(1);
    }

    class TopLevelVisitor : GuardVisitor
    {
        protected override VisitorGuard Guard => VisitorGuard.TopLevel;
    }
    
    [Fact]
    public void VisitAstWithTopLevelGuard_Correct()
    {
        var ctx = new UniversalContext();
        var visitor = new TopLevelVisitor();
        
        visitor.Visit(_ast, ctx);
        
        ctx.BodyVisitCt.ShouldBe(0);
        ctx.ModuleNameVisitCt.ShouldBe(1);
        ctx.FuncDefVisitCt.ShouldBe(1);
        ctx.TypeDefVisitCt.ShouldBe(1);
    }

    class FuncDefVisitor : GuardVisitor
    {
        protected override VisitorGuard Guard => VisitorGuard.FuncDef;
    }

    [Fact]
    public void VisitAstWithFuncDefGuard_Correct()
    {
        var ctx = new UniversalContext();
        var visitor = new FuncDefVisitor();
        
        visitor.Visit(_ast, ctx);
        
        ctx.BodyVisitCt.ShouldBe(0);
        ctx.ModuleNameVisitCt.ShouldBe(0);
        ctx.FuncDefVisitCt.ShouldBe(1);
        ctx.TypeDefVisitCt.ShouldBe(0);
    }

    class TypeDefVisitor : GuardVisitor
    {
        protected override VisitorGuard Guard => VisitorGuard.TypeDef;
    }

    [Fact]
    public void VisitAstWithTypeDefGuard_Correct()
    {
        var ctx = new UniversalContext();
        var visitor = new TypeDefVisitor();
        
        visitor.Visit(_ast, ctx);
        
        ctx.BodyVisitCt.ShouldBe(0);
        ctx.ModuleNameVisitCt.ShouldBe(0);
        ctx.FuncDefVisitCt.ShouldBe(0);
        ctx.TypeDefVisitCt.ShouldBe(1);
    }
    
    class FnWithBodyVisitor : GuardVisitor
    {
        protected override VisitorGuard Guard => VisitorGuard.FuncDefWithBody;
    }
    
    [Fact]
    public void VisitAstWithFuncDefWBodyGuard_Correct()
    {
        var ctx = new UniversalContext();
        var visitor = new FnWithBodyVisitor();
        
        visitor.Visit(_ast, ctx);
        
        ctx.BodyVisitCt.ShouldBe(1);
        ctx.ModuleNameVisitCt.ShouldBe(0);
        ctx.FuncDefVisitCt.ShouldBe(1);
        ctx.TypeDefVisitCt.ShouldBe(0);
    }

    class ModuleDefVisitor : GuardVisitor
    {
        protected override VisitorGuard Guard => VisitorGuard.ModuleDef;
    }
    
    [Fact]
    public void VisitAstWithModuleDefGuard_Correct()
    {
        var ctx = new UniversalContext();
        var visitor = new ModuleDefVisitor();
        
        visitor.Visit(_ast, ctx);
        
        ctx.BodyVisitCt.ShouldBe(0);
        ctx.ModuleNameVisitCt.ShouldBe(1);
        ctx.FuncDefVisitCt.ShouldBe(0);
        ctx.TypeDefVisitCt.ShouldBe(0);
    }
}