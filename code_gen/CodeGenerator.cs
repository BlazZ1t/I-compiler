using ImperativeLang.SemanticalAnalyzerNS;
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

            }
        }


        private void WriteHeader()
        {
            _writer.WriteLine(".assembly extern mscorlib {}");
            _writer.WriteLine(".assembly ImperativeLangProgram {}");
            _writer.WriteLine(".module ImperativeLangProgram.exe");
            _writer.WriteLine();
        }

        
        //Type declarations
        private void GenerateTypeClasses(ProgramNode AST)
        {
            //TODO: Make a full pass with context tracking
        }

        private void GenerateArrayTypeClass(ArrayTypeInfo type, string context, int id, string? objectName = null)
        {
            _writer.WriteLine($".class public auto valueType {type.Name}@{context}@{id}");
            _writer.WriteLine("{");
            string elementType = "";
            bool flag = true;
            if (objectName != null)
            {
                elementType = objectName;
                flag = false;
            }
            else if (type.ElementType is PrimitiveTypeInfo primitiveTypeInfo)
            {
                if (primitiveTypeInfo.Type == PrimitiveType.Integer 
                || primitiveTypeInfo.Type == PrimitiveType.Boolean) elementType = "int32";
                if (primitiveTypeInfo.Type == PrimitiveType.Real) elementType = "float32";
            } else
            {
                throw new Exception("Something went wrong ;3");
            }
            _writer.WriteLine($".field public {elementType}[] data");

            //Constructor
            _writer.WriteLine(".method public hidebysig specialname rtspecialname instance void .ctor() cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine(".maxstack 2");
            _writer.WriteLine("ldarg.0");
            _writer.WriteLine($"ldc.i4.s {type.Size}");
            _writer.WriteLine($"newarr {(
                flag 
                ? $"[mscorlib]System.{char.ToUpper(elementType[0]) + elementType.Substring(1)}" 
                : $"{elementType}")}");
            _writer.WriteLine($"stfld {elementType}[] {type.Name}@{context}@{id}::data");
            _writer.WriteLine("ret");
            _writer.WriteLine("}");

            //TODO: Add getter (optional)
            //TODO: Add setter (optional)
        }
        private void GenerateRecordTypeClass(RecordTypeInfo type, string context, int id)
        {
            //TODO: Implement method
        }
    }
}