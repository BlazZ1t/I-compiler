namespace ImperativeLang.SyntaxAnalyzer
{
    class ExpressionRoutineBodyNode : RoutineBodyNode
    {
        public ExpressionNode Expression { get; set; }

        public ExpressionRoutineBodyNode(ExpressionNode expression, int line = 0, int column = 0) : base(line, column)
        {
            Expression = expression;
        }
    }
}