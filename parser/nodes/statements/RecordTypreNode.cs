namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Record type definition, to be used with TypeDeclarationNode
    /// </summary>
    class RecordTypeNode : TypeNode //Record type definition, to be used with TypeDeclarationNode
    {
        public List<VariableDeclarationNode> Fields { get; set; }

        public RecordTypeNode(List<VariableDeclarationNode>? fields = null)
        {
            Fields = fields ?? new List<VariableDeclarationNode>();
        }
    }
}