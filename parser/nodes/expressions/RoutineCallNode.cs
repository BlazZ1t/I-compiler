using ImperativeLang.SemanticalAnalyzerNS;

namespace ImperativeLang.SyntaxAnalyzer
{

    /// <summary>
    /// Any routine call, for example: foo(1, 2)
    /// </summary>
    class RoutineCallNode : ExpressionNode
    {
        public string Name { get; set; }
        public RoutineSymbol? RoutineSymbol { get; set; }
        public List<ExpressionNode> Arguments { get; set; } = new();
        public RoutineCallNode(string name, List<ExpressionNode>? arguments = null, int line = 0, int column = 0, ImperativeLang.SemanticalAnalyzerNS.TypeInfo? resolvedType = null) : base(line, column, resolvedType)
        {
            Name = name;
            Arguments = arguments ?? new List<ExpressionNode>();
        }
    }
}