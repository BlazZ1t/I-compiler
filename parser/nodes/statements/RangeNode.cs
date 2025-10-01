namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Helper for for loop node
    /// </summary>
    class RangeNode : Node
    {
        public ExpressionNode Start { get; set; }
        public ExpressionNode? End { get; set; }

        public RangeNode(ExpressionNode start, ExpressionNode? end = null)
        {
            Start = start;
            End = end;
        }
    }
}