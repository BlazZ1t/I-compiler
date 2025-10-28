namespace ImperativeLang.SyntaxAnalyzer {
    abstract class RoutineBodyNode : Node
    {
        protected RoutineBodyNode(int line = 0, int column = 0) : base(line, column)
        {
        }
    }
}