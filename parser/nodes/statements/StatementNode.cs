namespace ImperativeLang.SyntaxAnalyzer
{
    abstract class StatementNode : Node
    {
        protected StatementNode(int line = 0, int column = 0) : base(line, column)
        {
        }
    }
}