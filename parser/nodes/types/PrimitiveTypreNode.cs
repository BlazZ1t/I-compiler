namespace ImperativeLang.SyntaxAnalyzer
{
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
}