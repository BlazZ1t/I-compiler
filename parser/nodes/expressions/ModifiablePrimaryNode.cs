namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Used for multipart expressions, e.g. arr[5] or point.x
    /// </summary>
     class ModifiablePrimaryNode : ExpressionNode // e.g. arr[5] or point.x
    {
        public string BaseName { get; set; } //Identifier name
        public AccessPart? AccessPart { get; set; } //Part accessed, e.g. [5] or .x

        public ModifiablePrimaryNode(string baseName, AccessPart? accessPart = null)
        {
            BaseName = baseName;
            AccessPart = accessPart;
        }
    }

    abstract class AccessPart : Node { } 
    /// <summary>
    /// Access part for record types, e.g. the .x part in point.x
    /// </summary>
    class FieldAccess : AccessPart //e.g. .x
    {
        public ModifiablePrimaryNode NextPrimary { get; set; }

        public FieldAccess (ModifiablePrimaryNode nextPrimary)
        {
            NextPrimary = nextPrimary;
        }
    }

    /// <summary>
    /// Access part for array types, e.g. the [5] part in arr[5]
    /// </summary>
    class ArrayAccess : AccessPart  //e.g. [5]
    {
        public ExpressionNode Index { get; set; }

        public AccessPart? AccessPart { get; set; }

        public ArrayAccess(ExpressionNode index, AccessPart accessPart)
        {
            Index = index;
            AccessPart = accessPart;
        }
    }
}