namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Array type definition, to be used with TypeDeclarationNode
    /// </summary>
    class ArrayTypeNode : TypeNode //Array type definition, to be used with TypeDeclarationNode
    {
        public ExpressionNode? Size { get; set; }
        public TypeNode ElementType { get; set; }

        public ArrayTypeNode(TypeNode elementType, ExpressionNode? size = null, int line = 0, int column = 0) : base(line, column)
        {
            ElementType = elementType;
            Size = size;
        }

    }
}