namespace ImperativeLang.SyntaxAnalyzer
{
    abstract class ExpressionNode : Node
    {
        protected ExpressionNode(int line = 0, int column = 0) : base(line, column)
        {
        }
    }

}