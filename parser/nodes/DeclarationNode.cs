namespace ImperativeLang.Parser
{
    abstract class DeclarationNode : AstNode { }


    /// <summary>
    /// Any variable declaration
    /// </summary>
    class VariableDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public TypeNode? VarType { get; set; }
        public ExpressionNode? Initializer { get; set; }

        public VariableDeclarationNode(string name, TypeNode? varType = null, ExpressionNode? initializer = null)
        {
            Name = name;
            VarType = varType;
            Initializer = initializer;
        }
    }

    /// <summary>
    /// Any type declaration
    /// For example:
    /// type int is integer
    /// type Point is record etc.
    /// type IntegerArray is array etc.
    /// </summary>
    class TypeDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public TypeNode Type { get; set; }

        public TypeDeclarationNode(string name, TypeNode type)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// Any routine declaration
    /// </summary>
    class RoutineDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; set; } = new();
        public TypeNode? ReturnType { get; set; }
        public RoutineBodyNode? Body { get; set; }

        public RoutineDeclarationNode(string name, List<ParameterNode>? parameters = null, TypeNode? returnType = null, RoutineBodyNode? body = null)
        {
            Name = name;
            Parameters = parameters ?? new List<ParameterNode>();
            ReturnType = returnType;
            Body = body;
        }
    }

    // Everything routine declaration related
    class ParameterNode : AstNode
    {
        public string Name { get; set; }
        public TypeNode Type { get; set; }

        public ParameterNode(string name, TypeNode type)
        {
            Name = name;
            Type = type;
        }
    }

    abstract class RoutineBodyNode : AstNode { }

    class BlockRoutineBodyNode : RoutineBodyNode
    {
        public List<AstNode> Body { get; set; } = new();

        public BlockRoutineBodyNode(List<AstNode>? body = null)
        {
            Body = body ?? new List<AstNode>();
        }
    }
    
    class ExpressionRoutineBodyNode : RoutineBodyNode
    {
        public ExpressionNode Expression { get; set; }

        public ExpressionRoutineBodyNode(ExpressionNode expression)
        {
            Expression = expression;
        }
    }
}