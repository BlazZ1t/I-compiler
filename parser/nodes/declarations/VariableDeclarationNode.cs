namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Any variable declaration
    /// </summary>
    class VariableDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public TypeNode? VarType { get; set; }
        public ExpressionNode? Initializer { get; set; }

        public VariableDeclarationNode(string name, TypeNode? varType = null, ExpressionNode? initializer = null)
        {
            if (varType == null && initializer == null)
            {
                throw new ParserException("Expected either type or an initializer value");
            }
            Name = name;
            VarType = varType;
            Initializer = initializer;
        }
    }
}