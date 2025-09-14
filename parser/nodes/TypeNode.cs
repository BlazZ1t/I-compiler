namespace ImperativeLang.SyntaxAnalyzer
{
    abstract class TypeNode : AstNode { }

    enum PrimitiveType
    {
        Integer,
        Boolean,
        Real
    }

    /// <summary>
    /// Primitive type definition, such as integer, boolean or real
    /// </summary>
    class PrimitiveTypeNode : TypeNode  //integer, boolean, real
    {
        public PrimitiveType Type { get; set; }

        public PrimitiveTypeNode(PrimitiveType Type)
        {
            this.Type = Type;
        }
    }
    /// <summary>
    /// Any user type reference
    /// </summary>
    /// <seealso cref="TypeDeclarationNode"/>
    class UserTypeNode : TypeNode //Any user type reference
    {
        public string Name { get; set; }

        public UserTypeNode (string Name)
        {
            this.Name = Name;
        }
    }

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

    /// <summary>
    /// Array type definition, to be used with TypeDeclarationNode
    /// </summary>
    class ArrayTypeNode : TypeNode //Array type definition, to be used with TypeDeclarationNode
    {
        public ExpressionNode? Size { get; set; }
        public TypeNode ElementType { get; set; }

        public ArrayTypeNode(TypeNode elementType, ExpressionNode? size = null)
        {
            ElementType = elementType;
            Size = size;
        }

    }
}

