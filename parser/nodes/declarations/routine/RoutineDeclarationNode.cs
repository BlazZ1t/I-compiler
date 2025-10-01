namespace ImperativeLang.SyntaxAnalyzer
{
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
    class ParameterNode : Node
    {
        public string Name { get; set; }
        public TypeNode Type { get; set; }

        public ParameterNode(string name, TypeNode type)
        {
            Name = name;
            Type = type;
        }
    }

    
}