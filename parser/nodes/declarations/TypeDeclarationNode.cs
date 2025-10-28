namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any type declaration
    /// For example:
    /// type int is integer
    /// type Point is record etc.
    /// type IntegerArray is array etc.
    /// </summary>
    class TypeDeclarationNode : DeclarationNode
    {
        public string Name { get; set; }
        public TypeNode Type { get; set; }

        public TypeDeclarationNode(string name, TypeNode type, int line = 0, int column = 0) : base(line, column)
        {
            Name = name;
            Type = type;
        }
    }

}