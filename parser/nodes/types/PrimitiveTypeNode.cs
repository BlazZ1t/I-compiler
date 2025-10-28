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

        public PrimitiveTypeNode(PrimitiveType Type, int line = 0, int column = 0) : base(line, column)
        {
            this.Type = Type;
        }
    }
}