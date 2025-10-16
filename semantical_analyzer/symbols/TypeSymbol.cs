namespace ImperativeLang.SemanticalAnalyzer
{
    class TypeSymbol : Symbol
    {
        TypeInfo Type;

        public TypeSymbol(string name, TypeInfo type) : base(name)
        {
            Type = type;            
        }
    }
}