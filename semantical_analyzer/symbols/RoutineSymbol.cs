namespace ImperativeLang.SemanticalAnalyzer
{
    class RoutineSymbol : Symbol
    {
        List<VariableSymbol> Parameters { get; set; }
        TypeInfo ReturnType { get; set; }

        public RoutineSymbol(string name, TypeInfo returnType, List<VariableSymbol> parameters) : base(name)
        {
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
}