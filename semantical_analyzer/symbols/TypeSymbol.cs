namespace ImperativeLang.SemanticalAnalyzer
{
    class TypeSymbol : Symbol
    {
        public TypeInfo Type { get; }

        public TypeSymbol(string name, TypeInfo type) : base(name)
        {
            Type = type;            
        }
    }
}