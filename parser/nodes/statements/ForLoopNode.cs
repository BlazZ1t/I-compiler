namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Any for loop statement
    /// </summary>
    class ForLoopNode : StatementNode
    {
        public string Iterator { get; set; }
        public RangeNode Range { get; set; }
        public bool Reverse { get; set; }
        public List<Node> Body { get; set; } = new();

        public ForLoopNode(string iterator, RangeNode range, bool reverse, List<Node>? body = null)
        {
            Iterator = iterator;
            Range = range;
            Reverse = reverse;
            Body = body ?? new List<Node>();
        }
    }
}