namespace ImperativeLang.SyntaxAnalyzer
{
   abstract class AstNode { }

    class ProgramNode : AstNode  //Base of a AST, contains all routines, global types and variable declarations
    {
        public List<RoutineDeclarationNode> routines = new();
        public List<TypeDeclarationNode> types = new();
        public List<VariableDeclarationNode> variables = new();
    } 
}