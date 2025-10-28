namespace ImperativeLang.SyntaxAnalyzer
{
    abstract class Node
    {
        public int Line { get; set; }
        public int Column { get; set; }

        protected Node(int line = 0, int column = 0)
        {
            Line = line;
            Column = column;
        }
    }
   
}