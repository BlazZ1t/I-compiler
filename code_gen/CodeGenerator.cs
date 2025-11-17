using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using ImperativeLang.SemanticalAnalyzerNS;
using ImperativeLang.SyntaxAnalyzer;

namespace ImperativeLang.CodeGen
{
    class CodeGenerator
    {
        public CodeGenerator(StreamWriter writer) { _writer = writer;}

        private StreamWriter _writer;

        private Dictionary<string, IlInfo> IDToIlName = new Dictionary<string, IlInfo>();

        public void GenerateMSIL(ProgramNode AST)
        {
            using (_writer)
            {
                WriteHeader();
                GenerateTypeClasses(AST);
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
            foreach(DeclarationNode node in AST.declarations)
            {
                if(node is TypeDeclarationNode typeDec)
                {
                    TypeInfo type = typeDec.TypeSymbol!.Type;
                    if (type is PrimitiveTypeInfo) continue;
                    if (!IDToIlName.ContainsKey(typeDec.Name))
                    {
                        IDToIlName.Add(typeDec.Name, new IlInfo());
                    }
                    IDToIlName[typeDec.Name].id++;
                    string ilName = $"{typeDec.Name}@global@{IDToIlName[typeDec.Name].id}";
                    IDToIlName[typeDec.Name].names.Push(ilName);
                    if (type is ArrayTypeInfo arrayTypeInfo)
                    {
                        GenerateArrayTypeClass(arrayTypeInfo, ilName, ResolveIlTypeName(arrayTypeInfo.ElementType));
                    }
                    else if(type is RecordTypeInfo recordTypeInfo)
                    {
                        Dictionary<string, string> elementTypeNames = new Dictionary<string, string>();
                        foreach(var typeName in recordTypeInfo.Fields.Keys)
                        {
                            string? elementTypeName = ResolveIlTypeName(recordTypeInfo.Fields[typeName]);
                            if(elementTypeName != null)
                            {
                                elementTypeNames[typeName] = elementTypeName;
                            }
                        }

                        GenerateRecordTypeClass(recordTypeInfo, ilName, elementTypeNames);
                    }
                }else if((node is RoutineDeclarationNode routineDec) && (routineDec.Body is BlockRoutineBodyNode blockBody))
                {
                    TraverseBody(blockBody.Body, routineDec.Name);
                }

            }

        }

        private void TraverseBody(List<Node> body, string context)
        {
            List<string> scope = new List<string>();

            foreach(Node node in body)
            {
                if(node is TypeDeclarationNode typeDec)
                {
                    TypeInfo type = typeDec.TypeSymbol!.Type;
                    if (type is PrimitiveTypeInfo) continue;
                    if (!IDToIlName.ContainsKey(typeDec.Name))
                    {
                        IDToIlName.Add(typeDec.Name, new IlInfo());
                    }
                    IDToIlName[typeDec.Name].id++;
                    string ilName = $"{typeDec.Name}@{context}@{IDToIlName[typeDec.Name].id}";
                    IDToIlName[typeDec.Name].names.Push(ilName);
                    scope.Add(typeDec.Name);

                    if (type is ArrayTypeInfo arrayTypeInfo)
                    {
                        GenerateArrayTypeClass(arrayTypeInfo, ilName, ResolveIlTypeName(arrayTypeInfo.ElementType));
                    }
                    else if(type is RecordTypeInfo recordTypeInfo)
                    {
                        Dictionary<string, string> elementTypeNames = new Dictionary<string, string>();
                        foreach(var typeName in recordTypeInfo.Fields.Keys)
                        {
                            string? elementTypeName = ResolveIlTypeName(recordTypeInfo.Fields[typeName]);
                            if(elementTypeName != null)
                            {
                                elementTypeNames[typeName] = elementTypeName;
                            }
                        }

                        GenerateRecordTypeClass(recordTypeInfo, ilName, elementTypeNames);
                    }
                }else if(node is IfStatementNode ifNode)
                {
                    TraverseBody(ifNode.ThenBody, context);
                    if(ifNode.ElseBody != null)
                    {
                        TraverseBody(ifNode.ElseBody, context);
                    }
                }else if(node is ForLoopNode forNode)
                {
                    TraverseBody(forNode.Body, context);
                }else if(node is WhileLoopNode whileNode)
                {
                    TraverseBody(whileNode.Body, context);
                }
            }

            foreach(var el in scope)
            {
                IDToIlName[el].names.Pop();
            }
            
        }

        private string? ResolveIlTypeName(TypeInfo typeInfo)
        {
            string? ilTypeName = null;
            if (typeInfo is ArrayTypeInfo a)
            {
                ilTypeName = IDToIlName[a.Name].names.Peek();
            }
            else if (typeInfo is RecordTypeInfo r)
            {
                ilTypeName = IDToIlName[r.Name].names.Peek();
            }
            return ilTypeName;
        }

        private void GenerateArrayTypeClass(ArrayTypeInfo type, string className, string? objectName = null)
        {
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
        private void GenerateRecordTypeClass(RecordTypeInfo type, string className, Dictionary<string, string> objectNames)
        {
            _writer.WriteLine($".class public auto valuetype {className}");
            _writer.WriteLine("{");

            Dictionary<string, string> ilFieldTypes = new();

            //TODO: Check if works
            foreach(string field in type.Fields.Keys)
            {
                ilFieldTypes[field] = ResolveIlType(type.Fields[field], objectNames);;
                _writer.WriteLine($".field public {ilFieldTypes[field]} {field}");
            }

            _writer.Write(".method public hidebysig specialname rtspecialname instance void .ctor(");

            bool first = true;
            foreach (var (fieldName, ilFieldType) in ilFieldTypes)
            {
                if (!first) _writer.Write(", ");
                first = false;

                _writer.Write($"{ilFieldType} {fieldName}");
            }
            _writer.WriteLine(") cil managed");

             _writer.WriteLine("{");
            _writer.WriteLine(".maxstack 8");

            _writer.WriteLine("ldarg.0");
            _writer.WriteLine("call instance void [mscorlib]System.ValueType::.ctor()");

            int index = 1;
            foreach (var (fieldName, ilFieldType) in ilFieldTypes)
            {
                _writer.WriteLine("ldarg.0");
                _writer.WriteLine($"ldarg.{index}");
                _writer.WriteLine($"stfld {ilFieldType} {className}::{fieldName}");
                index++;
            }

            _writer.WriteLine("ret");
            _writer.WriteLine("}");

            _writer.WriteLine("}");
        }



        string ResolveIlType(TypeInfo type, Dictionary<string,string> objectNames)
        {
            switch (type)
            {
                case PrimitiveTypeInfo p:
                    return p.Type switch
                    {
                        PrimitiveType.Integer => "int32",
                        PrimitiveType.Boolean => "int32",
                        PrimitiveType.Real => "float32",
                        _ => throw new Exception("Unsupported primitive")
                    };

                case RecordTypeInfo r:
                    return $"valuetype {objectNames[r.Name]}";

                case ArrayTypeInfo a:
                    return $"{objectNames[a.Name]}[]";

                default:
                    throw new Exception("Unknown field type");
            }
        }
    }
}