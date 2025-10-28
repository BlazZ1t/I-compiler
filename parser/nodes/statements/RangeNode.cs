namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Helper for for loop node
    /// </summary>
    class RangeNode : Node
    {
        public ExpressionNode Start { get; set; }
        public ExpressionNode? End { get; set; }
        public RangeNode(ExpressionNode start, ExpressionNode? end = null, int line = 0, int column = 0) : base(line, column)
        {
            Start = start;
            End = end;
        }
    }
}