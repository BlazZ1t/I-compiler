using ImperativeLang.SyntaxAnalyzer;

namespace ImperativeLang.CodeGen
{
    class CodeGenerator
    {
        public CodeGenerator(StreamWriter writer) { _writer = writer;}

        private StreamWriter _writer;

        public void GenerateMSIL(ProgramNode AST)
        {
            using (_writer)
            {
                WriteHeader();
                WriteProgramShell();
            }
        }

        private void WriteHeader()
        {
            _writer.WriteLine(".assembly ImperativeLangProgram {}");
            _writer.WriteLine(".module ImperativeLangProgram.exe");
            _writer.WriteLine();
        }

        private void WriteProgramShell()
        {
            _writer.WriteLine(".class public Program");
            _writer.WriteLine("{");

            _writer.WriteLine("    .method public static void main() cil managed");
            _writer.WriteLine("    {");
            _writer.WriteLine("        .entrypoint");

            _writer.WriteLine("        ldstr \"Hello World!\""); //Placeholder just to see that it works.
            _writer.WriteLine("        call void [System.Console]System.Console::WriteLine(string)");

            _writer.WriteLine("        ret");
            _writer.WriteLine("    }");

            _writer.WriteLine("}");
        }
    }
}