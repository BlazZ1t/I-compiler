namespace ImperativeLang.Parser
{
    /// <summary>
    /// Used for multipart expressions, e.g. arr[5] or point.x
    /// </summary>
     class ModifiablePrimaryNode : ExpressionNode // e.g. arr[5] or point.x
    {
        public string BaseName { get; set; } //Identifier name
        public List<AccessPart> AccessPart { get; set; } = new(); //Part accessed, e.g. [5] or .x

        public ModifiablePrimaryNode(string baseName, List<AccessPart>? accessPart)
        {
            BaseName = baseName;
            AccessPart = accessPart ?? new List<AccessPart>();
        }
    }

    abstract class AccessPart : AstNode { } 
    /// <summary>
    /// Access part for record types, e.g. the .x part in point.x
    /// </summary>
    class FieldAccess : AccessPart //e.g. .x
    {
        public string FieldName { get; set; }

        public FieldAccess (string fieldName)
        {
            FieldName = fieldName;
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