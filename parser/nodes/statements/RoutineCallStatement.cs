namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Usage of routine calls in statements
    /// </summary>
    class RoutineCallStatementNode : StatementNode
    {
        public required RoutineCallNode Call { get; set; }
    }
}