using ImperativeLang.SyntaxAnalyzer;

namespace ImperativeLang.SemanticalAnalyzerNS
{
    class RoutineSymbol : Symbol
    {
        public List<VariableSymbol> Parameters { get; set; }
        public TypeInfo? ReturnType { get; set; }
        public bool IsForwardDeclared { get; set; }

        public RoutineSymbol(string name, TypeInfo? returnType, List<VariableSymbol> parameters, bool isForwardDeclared) : base(name)
        {
            ReturnType = returnType;
            Parameters = parameters;
            IsForwardDeclared = isForwardDeclared;
        }
    }
}