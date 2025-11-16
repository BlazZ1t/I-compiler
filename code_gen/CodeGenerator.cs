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
            string className = $"{type.Name}@{context}@{id}";
            _writer.WriteLine($".class public auto valuetype {className}");
            _writer.WriteLine("{");
            string ilFieldType = "";
            string ilNewArrType = "";

            if (objectName != null)
            {
                ilFieldType = objectName;
                ilNewArrType = objectName;
            }
            else if (type.ElementType is PrimitiveTypeInfo p)
            {
                switch (p.Type)
                {
                    case PrimitiveType.Integer:
                    case PrimitiveType.Boolean:
                        ilFieldType = "int32";
                        ilNewArrType = "[mscorlib]System.Int32";
                        break;

                    case PrimitiveType.Real:
                        ilFieldType = "float32";
                        ilNewArrType = "[mscorlib]System.Single";
                        break;
                }
            }
            else throw new Exception("Unhandled element type!");
            _writer.WriteLine($".field public {ilFieldType}[] data");

            //Constructor
            _writer.WriteLine(".method public hidebysig specialname rtspecialname instance void .ctor() cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine(".maxstack 3");
            _writer.WriteLine("ldarg.0");
            _writer.WriteLine("call instance void [mscorlib]System.ValueType::.ctor()");
            _writer.WriteLine("ldarg.0");
            _writer.WriteLine($"ldc.i4.s {type.Size}");
            _writer.WriteLine($"newarr {ilNewArrType}");
            _writer.WriteLine($"stfld {ilFieldType}[] {className}::data");
            _writer.WriteLine("ret");
            _writer.WriteLine("}");

            //Getter
            _writer.WriteLine(".method public hidebysig instance " + ilFieldType + " get_Item(int32 index) cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine(".maxstack 3");
            _writer.WriteLine("ldarg.0");
            _writer.WriteLine($"ldfld {ilFieldType}[] {className}::data");
            _writer.WriteLine("ldarg.1");
            _writer.WriteLine($"ldelem.{ilFieldType}");
            _writer.WriteLine("ret");
            _writer.WriteLine("}");
            
            //Setter
            _writer.WriteLine(".method public hidebysig instance void set_Item(int32 index, " + ilFieldType + " value) cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine(".maxstack 4");
            _writer.WriteLine("ldarg.0");
            _writer.WriteLine($"ldfld {ilFieldType}[] {className}::data");
            _writer.WriteLine("ldarg.1");
            _writer.WriteLine("ldarg.2");
            _writer.WriteLine($"stelem.{ilFieldType}");
            _writer.WriteLine("ret");
            _writer.WriteLine("}");

            _writer.WriteLine("}");
        }
        private void GenerateRecordTypeClass(RecordTypeInfo type, string context, int id)
        {
            //TODO: Implement method
        }
    }
}