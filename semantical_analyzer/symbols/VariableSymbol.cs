namespace ImperativeLang.SemanticalAnalyzerNS
{
    class VariableSymbol : Symbol
    {
        public TypeInfo Type { get; set; }

        public bool IsReadOnly { get; set; }

        public VariableSymbol(string name, TypeInfo type, bool isReadOnly = false) : base(name)
        {
            Type = type;
            IsReadOnly = isReadOnly;
        }
    }
}