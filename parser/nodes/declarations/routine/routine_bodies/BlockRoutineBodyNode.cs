namespace ImperativeLang.SyntaxAnalyzer {
    class BlockRoutineBodyNode : RoutineBodyNode
    {
        public List<Node> Body { get; set; } = new();

        public BlockRoutineBodyNode(List<Node>? body = null, int line = 0, int column = 0) : base(line, column)
        {
            Body = body ?? new List<Node>();
        }
    }
}