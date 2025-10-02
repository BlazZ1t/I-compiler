namespace ImperativeLang.SyntaxAnalyzer
{

    class ProgramNode : Node  //Base of a AST, contains all routines, global types and variable declarations
    {
        public List<RoutineDeclarationNode> routines { get; set; } = new();
        public List<TypeDeclarationNode> types { get; set; } = new();
        public List<VariableDeclarationNode> variables { get; set; } = new();
    }
}