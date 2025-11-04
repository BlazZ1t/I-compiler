namespace ImperativeLang.SyntaxAnalyzer
{
    using ImperativeLang.SemanticalAnalyzerNS;

    abstract class ExpressionNode : Node
    {
        public TypeInfo? ResolvedType { get; set; }

        protected ExpressionNode(int line = 0, int column = 0, TypeInfo? resolvedType = null) : base(line, column)
        {
            ResolvedType = resolvedType;
        }
    }

}