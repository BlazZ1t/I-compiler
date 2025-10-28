namespace ImperativeLang.SyntaxAnalyzer
{

    class ProgramNode : Node  //Base of a AST, contains all routines, global types and variable declarations
    {
        public List<DeclarationNode> declarations { get; set; } = new();

        public ProgramNode(int line = 0, int column = 0) : base(line, column)
        {
        }
    }
}