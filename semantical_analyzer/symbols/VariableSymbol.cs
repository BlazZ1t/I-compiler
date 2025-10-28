namespace ImperativeLang.SemanticalAnalyzer
{
    class VariableSymbol : Symbol
    {
        public TypeInfo Type { get; set; }

        public bool IsInitialized { get; set; }

        public VariableSymbol(string name, TypeInfo type, bool isInitialized = false) : base(name)
        {
            Type = type;
            IsInitialized = isInitialized;
        }
    }
}