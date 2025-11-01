namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Usage of routine calls in statements
    /// </summary>
    class RoutineCallStatementNode : StatementNode
    {
        public RoutineCallNode Call { get; set; }

        public RoutineCallStatementNode(RoutineCallNode call, int line = 0, int column = 0) : base(line, column)
        {
            Call = call;
        }
    }
}