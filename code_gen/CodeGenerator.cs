using System.Collections;
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
                WriteMainClass(AST);
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
            _writer.WriteLine($".class public auto ansi sealed {className} extends [mscorlib]System.ValueType");
            _writer.WriteLine("{");

            string ilFieldType = "";
            string ilNewArrType = "";
            string elemLoadOpcode = "";
            string elemStoreOpcode = "";
            bool isStruct = objectName != null;

            // Determine IL type and load/store behavior
            if (isStruct)
            {
                // Struct (valuetype) element
                ilFieldType = $"valuetype {objectName!}";
                ilNewArrType = $"valuetype {objectName}";
                // For structs: load/store done via ldelema + ldobj/stobj
            }
            else if (type.ElementType is PrimitiveTypeInfo p)
            {
                switch (p.Type)
                {
                    case PrimitiveType.Integer:
                    case PrimitiveType.Boolean:
                        ilFieldType = "int32";
                        ilNewArrType = "[mscorlib]System.Int32";
                        elemLoadOpcode = "ldelem.i4";
                        elemStoreOpcode = "stelem.i4";
                        break;

                    case PrimitiveType.Real:
                        ilFieldType = "float32";
                        ilNewArrType = "[mscorlib]System.Single";
                        elemLoadOpcode = "ldelem.r4";
                        elemStoreOpcode = "stelem.r4";
                        break;

                    default:
                        throw new Exception("Unhandled primitive type!");
                }
            }
            else
            {
                throw new Exception("Unhandled element type!");
            }

            // FIELD
            _writer.WriteLine($".field public {ilFieldType}[] data");


            // CONSTRUCTOR
            _writer.WriteLine(".method public hidebysig specialname rtspecialname instance void .ctor() cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine("  .maxstack 3");
            _writer.WriteLine("  ldarg.0");
            _writer.WriteLine("  call instance void [mscorlib]System.ValueType::.ctor()");
            _writer.WriteLine("  ldarg.0");
            _writer.WriteLine($"  ldc.i4.s {type.Size}");
            _writer.WriteLine($"  newarr {ilNewArrType}");
            _writer.WriteLine($"  stfld {ilFieldType}[] {className}::data");
            _writer.WriteLine("  ret");
            _writer.WriteLine("}");


            // GETTER
            _writer.WriteLine($".method public hidebysig instance {ilFieldType} get_Item(int32 index) cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine("  .maxstack 3");

            if (!isStruct)
            {
                // Primitive getter
                _writer.WriteLine("  ldarg.0");
                _writer.WriteLine($"  ldfld {ilFieldType}[] {className}::data");
                _writer.WriteLine("  ldarg.1");
                _writer.WriteLine($"  {elemLoadOpcode}");
                _writer.WriteLine("  ret");
            }
            else
            {
                // Struct getter: ldelema + ldobj
                _writer.WriteLine("  ldarg.0");
                _writer.WriteLine($"  ldfld {ilFieldType}[] {className}::data");
                _writer.WriteLine("  ldarg.1");
                _writer.WriteLine($"  ldelema {ilFieldType}");
                _writer.WriteLine($"  ldobj {ilFieldType}");
                _writer.WriteLine("  ret");
            }

            _writer.WriteLine("}");


            // SETTER
            _writer.WriteLine($".method public hidebysig instance void set_Item(int32, {ilFieldType}) cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine("  .maxstack 4");

            if (!isStruct)
            {
                _writer.WriteLine("  ldarg.0");
                _writer.WriteLine($"  ldfld {ilFieldType}[] {className}::data");
                _writer.WriteLine("  ldarg.1");
                _writer.WriteLine("  ldarg.2");
                _writer.WriteLine($"  {elemStoreOpcode}");
                _writer.WriteLine("  ret");
            }
            else
            {
                _writer.WriteLine("  ldarg.0");
                _writer.WriteLine($"  ldfld {ilFieldType}[] {className}::data");
                _writer.WriteLine("  ldarg.1");
                _writer.WriteLine($"  ldelema {ilFieldType}");
                _writer.WriteLine("  ldarg.2");
                _writer.WriteLine($"  stobj {ilFieldType}");
                _writer.WriteLine("  ret");
            }

            _writer.WriteLine("}");

            _writer.WriteLine("}"); // end class
        }

        private void GenerateRecordTypeClass(RecordTypeInfo type, string className, Dictionary<string, string> objectNames)
        {
            _writer.WriteLine($".class public auto sealed {className} extends [mscorlib]System.ValueType");
            _writer.WriteLine("{");

            Dictionary<string, string> ilFieldTypes = new();

            //TODO: Check if works
            foreach(string field in type.Fields.Keys)
            {
                ilFieldTypes[field] = ResolveIlType(field, type.Fields[field], objectNames);
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
        private string ResolveIlType(string fieldName, TypeInfo type, Dictionary<string,string> objectNames)
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
                    return $"valuetype {objectNames[fieldName]}";

                case ArrayTypeInfo a:
                    return $"{objectNames[fieldName]}[]";

                default:
                    throw new Exception("Unknown field type");
            }
        }
    
        // Main class
        private void WriteMainClass(ProgramNode AST)
        {
            _writer.WriteLine(".class public auto ansi Program");
            _writer.WriteLine("{");
            WriteGlobalVariables(AST);
            WriteEntrypointMethod(AST);
            WriteRoutineMethods(AST);
            _writer.WriteLine("}");
        }

        private void WriteGlobalVariables(ProgramNode AST)
        {
            foreach(var node in AST.declarations.OfType<VariableDeclarationNode>())
            {
                TypeInfo type = node.VariableSymbol!.Type;
                if(type is PrimitiveTypeInfo primitiveType)
                {
                    switch (primitiveType.Type)
                    {
                        case PrimitiveType.Real:
                            _writer.WriteLine($".field public static float32 {node.Name}");
                            break;
                        case PrimitiveType.Integer:
                        case PrimitiveType.Boolean:
                            _writer.WriteLine($".field public static int32 {node.Name}");
                            break;
                    }
                }
                else if(type is ArrayTypeInfo arrayType)
                {
                    _writer.WriteLine($".field public static valuetype {IDToIlName[arrayType.Name].names.Peek()} {node.Name}");
                }
                else if(type is RecordTypeInfo recordType)
                {
                    _writer.WriteLine($".field public static valuetype {IDToIlName[recordType.Name].names.Peek()} {node.Name}");
                }
                else throw new Exception("Hehe ;3");
            }
        }

        private void WriteEntrypointMethod(ProgramNode AST)
        {
            List<RoutineSymbol> routines = new List<RoutineSymbol>();
            foreach(var routine in AST.declarations.OfType<RoutineDeclarationNode>())
            {
                routines.Add(routine.RoutineSymbol!);
            }
            _writer.WriteLine("");
            _writer.WriteLine(".method public static void Main(string[] args) cil managed");
            _writer.WriteLine("{");
            _writer.WriteLine(".entrypoint");
            
            _writer.WriteLine("ldarg.0");
            _writer.WriteLine("brfalse NO_ARGS");

            _writer.WriteLine("ldarg.0");
            _writer.WriteLine("ldlen");
            _writer.WriteLine("conv.i4");
            _writer.WriteLine("ldc.i4.1");
            _writer.WriteLine("blt NO_ARGS");
            _writer.WriteLine("");

            foreach (var routine in routines)
            {
                _writer.WriteLine("ldarg.0");
                _writer.WriteLine("ldc.i4.0");
                _writer.WriteLine("ldelem.ref");
                _writer.WriteLine($"ldstr \"{routine.Name}\"");
                _writer.WriteLine("call bool [mscorlib]System.String::Equals(string, string)");
                _writer.WriteLine($"brtrue FILL_{routine.Name}");
                _writer.WriteLine("");
            }
            _writer.WriteLine("br END");
            _writer.WriteLine("");
            
            foreach (var routine in routines)
            {
                _writer.WriteLine($"FILL_{routine.Name}:");
                bool badRoutineFlag = false;
                List<string> argumentTypes = new List<string>();
                foreach(var arg in routine.Parameters)
                {
                    if(arg.Type is PrimitiveTypeInfo primitiveType)
                    {
                        switch (primitiveType.Type)
                        {
                            case PrimitiveType.Integer:
                            case PrimitiveType.Boolean:
                                argumentTypes.Add("int32");
                                break;
                            case PrimitiveType.Real:
                                argumentTypes.Add("float32");
                                break;
                        }
                    }
                    else
                    {
                        _writer.WriteLine("br BAD_ROUTINE");
                        badRoutineFlag = true;
                        break;
                    }
                }

                if (badRoutineFlag)
                {
                    continue;
                }


                _writer.WriteLine("ldarg.0");
                _writer.WriteLine("ldlen");
                _writer.WriteLine("conv.i4");
                _writer.WriteLine($"ldc.i4.{argumentTypes.Count}");
                _writer.WriteLine("blt TOO_FEW_ARGS");
                _writer.WriteLine("ldarg.0");
                _writer.WriteLine("ldlen");
                _writer.WriteLine("conv.i4");
                _writer.WriteLine($"ldc.i4.{argumentTypes.Count+1}");
                _writer.WriteLine("bgt TOO_MANY_ARGS");
                _writer.WriteLine("");

                for(int i = 0; i < argumentTypes.Count; i++)
                {
                    string argType = argumentTypes[i];

                    

                    _writer.WriteLine("ldarg.0");
                    _writer.WriteLine($"ldc.i4.{i+1}");
                    _writer.WriteLine("ldelem.ref");
                    if(argType == "int32")
                    {
                        _writer.WriteLine("call int32 [mscorlib]System.Int32::Parse(string)");
                    }
                    else if(argType == "float32")
                    {
                        _writer.WriteLine("call float32 [mscorlib]System.Single::Parse(string)");
                    }
                    else
                    {
                        throw new Exception("Something went wrond while generating entrypoint method!");
                    }
                    
                }
                _writer.WriteLine("br END");
                _writer.WriteLine("");
            }

            _writer.WriteLine("TOO_FEW_ARGS:");
            _writer.WriteLine("ldstr \"Error: too few arguments provided!\"");
            _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(string)");
            _writer.WriteLine("br END");
            _writer.WriteLine("");

            _writer.WriteLine("TOO_MANY_ARGS:");
            _writer.WriteLine("ldstr \"Error: too many arguments provided!\"");
            _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(string)");
            _writer.WriteLine("br END");
            _writer.WriteLine("");

            _writer.WriteLine("BAD_ROUTINE:");
            _writer.WriteLine("ldstr \"Error: entry point routine takes an argument of a user-defined type!\"");
            _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(string)");
            _writer.WriteLine("br END");
            _writer.WriteLine("");

            _writer.WriteLine("NO_ARGS:");
            _writer.WriteLine("ldstr \"Error: no arguments provided!\"");
            _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(string)");
            _writer.WriteLine("");

            _writer.WriteLine("END:");
            _writer.WriteLine("ret");
            

            _writer.WriteLine("}");
        }

        private void WriteRoutineMethods(ProgramNode AST)
        {
            
        }
    }
}