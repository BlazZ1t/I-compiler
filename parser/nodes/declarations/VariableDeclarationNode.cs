using ImperativeLang.SemanticalAnalyzerNS;

namespace ImperativeLang.SyntaxAnalyzer {
    /// <summary>
    /// Any variable declaration
    /// </summary>
    class VariableDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public VariableSymbol? VariableSymbol { get; set; }
        public TypeNode? VarType { get; set; }
        public ExpressionNode? Initializer { get; set; }

        public VariableDeclarationNode(string name, TypeNode? varType = null, ExpressionNode? initializer = null, int line = 0, int column = 0) : base(line, column)
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