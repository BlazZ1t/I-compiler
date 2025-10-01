namespace ImperativeLang.SyntaxAnalyzer
{
    /// <summary>
    /// Any user type reference
    /// /// </summary>
    /// <seealso cref="TypeDeclarationNode"/>
    class UserTypeNode : TypeNode //Any user type reference
    {
        public string Name { get; set; }

        public UserTypeNode (string Name)
        {
            this.Name = Name;
        }
    }
}