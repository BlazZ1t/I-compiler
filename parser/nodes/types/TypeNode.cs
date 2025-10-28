namespace ImperativeLang.SyntaxAnalyzer {
    abstract class TypeNode : Node
    {
        protected TypeNode(int line = 0, int column = 0) : base(line, column)
        {
        }
    }
}