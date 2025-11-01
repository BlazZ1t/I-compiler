namespace ImperativeLang.SemanticalAnalyzerNS
{
    abstract class Symbol
    {
        public string Name { get; }

        protected Symbol(string name) => Name = name;

    }
}