namespace ImperativeLang.SyntaxAnalyzer
{

    /// <summary>
    /// Any routine call, for example: foo(1, 2)
    /// </summary>
    class RoutineCallNode : ExpressionNode
    {
        public string Name { get; set; }
        public List<ExpressionNode> Arguments { get; set; } = new();

        public RoutineCallNode(string name, List<ExpressionNode>? arguments = null)
        {
            Name = name;
            Arguments = arguments ?? new List<ExpressionNode>();
        }
    }
}