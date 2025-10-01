namespace ImperativeLang.SyntaxAnalyzer
{
    class ExpressionRoutineBodyNode : RoutineBodyNode
    {
        public ExpressionNode Expression { get; set; }

        public ExpressionRoutineBodyNode(ExpressionNode expression)
        {
            Expression = expression;
        }
    }
}