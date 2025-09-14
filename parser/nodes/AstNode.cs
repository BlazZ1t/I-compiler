namespace ImperativeLang.Parser
{
   abstract class AstNode { }

    class ProgramNode : AstNode  //Base of a AST, contains all routines
    {
        public List<RoutineDeclarationNode> routines = new();
        public List<TypeDeclarationNode> types = new();
        public List<VariableDeclarationNode> variables = new();
    } 
}