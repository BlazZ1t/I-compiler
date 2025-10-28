namespace ImperativeLang.SyntaxAnalyzer
{
    abstract class DeclarationNode : Node
    {
        protected DeclarationNode(int line = 0, int column = 0) : base(line, column)
        {
        }
    }
}