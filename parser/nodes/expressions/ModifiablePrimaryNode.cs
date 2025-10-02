namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Used for multipart expressions, e.g. arr[5] or point.x
    /// </summary>
     class ModifiablePrimaryNode : ExpressionNode // e.g. arr[5] or point.x
    {
        public string BaseName { get; set; } //Identifier name
        public List<AccessPart> AccessPart { get; set; } = new(); //Part accessed, e.g. [5] or .x

        public ModifiablePrimaryNode(string baseName)
        {
            BaseName = baseName;
        }
    }

    abstract class AccessPart : Node { } 
    /// <summary>
    /// Access part for record types, e.g. the .x part in point.x
    /// </summary>
    class FieldAccess : AccessPart //e.g. .x
    {
        public string Name { get; set; }

        public FieldAccess (string name)
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

        public ArrayAccess(ExpressionNode index)
        {
            Index = index;
        }
    }
}