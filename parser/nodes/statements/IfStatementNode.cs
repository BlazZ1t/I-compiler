namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any if statement
    /// </summary>
    class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public List<Node> ThenBody { get; set; } = new();
        public List<Node>? ElseBody { get; set; }

        public IfStatementNode(ExpressionNode condition, List<Node>? thenBody = null, List<Node>? elseBody = null)
        {
            Condition = condition;
            ThenBody = thenBody ?? new List<Node>();
            ElseBody = elseBody;
        }
    }
}