namespace ImperativeLang.SyntaxAnalyzer {
    class BlockRoutineBodyNode : RoutineBodyNode
    {
        public List<Node> Body { get; set; } = new();

        public BlockRoutineBodyNode(List<Node>? body = null)
        {
            Body = body ?? new List<Node>();
        }
    }
}