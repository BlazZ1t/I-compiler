namespace ImperativeLang.SemanticalAnalyzer
{
    abstract class Symbol
    {
        string Name { get; }

        protected Symbol(string name) => Name = name;

    }
}