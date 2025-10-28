namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any assignment of type target := expression
    /// </summary>
    class AssignmentNode : StatementNode
    {
        public ModifiablePrimaryNode Target { get; set; }
        public ExpressionNode Value { get; set; }

        public AssignmentNode(ModifiablePrimaryNode target, ExpressionNode value, int line = 0, int column = 0) : base(line, column)
        {
            Target = target;
            Value = value;
        }
    }
}