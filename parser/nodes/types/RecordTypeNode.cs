namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Record type definition, to be used with TypeDeclarationNode
    /// </summary>
    class RecordTypeNode : TypeNode //Record type definition, to be used with TypeDeclarationNode
    {
        public List<VariableDeclarationNode> Fields { get; set; }

        public RecordTypeNode(List<VariableDeclarationNode>? fields = null, int line = 0, int column = 0) : base(line, column)
        {
            Fields = fields ?? new List<VariableDeclarationNode>();
        }
    }
}