using ImperativeLang.SemanticalAnalyzerNS;

namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Used for multipart expressions, e.g. arr[5] or point.x
    /// </summary>
    class ModifiablePrimaryNode : ExpressionNode
    {
        public string BaseName { get; set; }
        public VariableSymbol? VariableSymbol { get; set; }
        public List<AccessPart> AccessPart { get; set; } = new();
        public ModifiablePrimaryNode(string baseName, int line = 0, int column = 0, ImperativeLang.SemanticalAnalyzerNS.TypeInfo? resolvedType = null) : base(line, column, resolvedType)
        {
            BaseName = baseName;
        }
    }

    abstract class AccessPart : Node
    {
        protected AccessPart(int line = 0, int column = 0) : base(line, column)
        {
        }
    }
    /// <summary>
    /// Access part for record types, e.g. the .x part in point.x
    /// </summary>
    class FieldAccess : AccessPart //e.g. .x
    {
        public string Name { get; set; }

        public FieldAccess (string name, int line = 0, int column = 0) : base(line, column)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Access part for array types, e.g. the [5] part in arr[5]
    /// </summary>
    class ArrayAccess : AccessPart  //e.g. [5]
    {
        public ExpressionNode Index { get; set; }

        public ArrayAccess(ExpressionNode index, int line = 0, int column = 0) : base(line, column)
        {
            Index = index;
        }
    }
}