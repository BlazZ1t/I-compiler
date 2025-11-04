namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any literal expression for integers, reals and booleans
    /// </summary>
    class LiteralNode : ExpressionNode
    {
        public object Value { get; set; }
        public PrimitiveType Type;

        public LiteralNode(object Value, PrimitiveType type, int line = 0, int column = 0, ImperativeLang.SemanticalAnalyzerNS.TypeInfo? resolvedType = null) : base(line, column, resolvedType)
        {
            this.Value = Value;
            Type = type;
        }
    }
}