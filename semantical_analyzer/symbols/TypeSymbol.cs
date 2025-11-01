namespace ImperativeLang.SemanticalAnalyzerNS
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