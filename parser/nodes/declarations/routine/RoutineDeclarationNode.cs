using ImperativeLang.SemanticalAnalyzerNS;

namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any routine declaration
    /// </summary>
    class RoutineDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public RoutineSymbol? RoutineSymbol { get; set; }
        public List<VariableDeclarationNode> Parameters { get; set; } = new();
        public TypeNode? ReturnType { get; set; }
        public RoutineBodyNode? Body { get; set; }

        public RoutineDeclarationNode(string name, List<VariableDeclarationNode>? parameters = null, TypeNode? returnType = null, RoutineBodyNode? body = null, int line = 0, int column = 0) : base(line, column)
        {
            Name = name;
            Parameters = parameters ?? new List<VariableDeclarationNode>();
            ReturnType = returnType;
            Body = body;
        }
    }

    
}