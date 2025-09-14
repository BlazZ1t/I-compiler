namespace ImperativeLang.Parser
{
    class Parser
    {
        private List<Token> Tokens;

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
        }

        public ProgramNode getAST()
        {
            ProgramNode programNode = new ProgramNode();

            

            return programNode;
        }
    }
}