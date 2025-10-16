namespace ImperativeLang.SemanticalAnalyzer
{
    class VariableSymbol : Symbol
    {
        TypeInfo Type { get; set; }

        bool isInitialized { get; set; }

        public VariableSymbol(string name, TypeInfo type, bool isInitialized = false) : base(name)
        {
            Type = type;
            this.isInitialized = isInitialized;       
        }
    }
}