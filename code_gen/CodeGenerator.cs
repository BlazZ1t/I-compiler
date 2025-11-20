using System.ComponentModel.DataAnnotations;
using System.Runtime;
using ImperativeLang.SemanticalAnalyzerNS;
using ImperativeLang.SyntaxAnalyzer;

namespace ImperativeLang.CodeGen
{
    class CodeGenerator
    {
        public CodeGenerator(StreamWriter writer) { _writer = writer;}

        private StreamWriter _writer;

        private Dictionary<string, IlInfo> TypeIdentifierToIlName = new Dictionary<string, IlInfo>();
        private Dictionary<string, IlInfo> VariableIdentifierToIlName = new Dictionary<string, IlInfo>();


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
                    if (!TypeIdentifierToIlName.ContainsKey(typeDec.Name))
                    {
                        TypeIdentifierToIlName.Add(typeDec.Name, new IlInfo());
                    }
                    string ilName = $"{typeDec.Name}@global@0";
                    TypeIdentifierToIlName[typeDec.Name].names.Push(ilName);
                    if (type is ArrayTypeInfo arrayTypeInfo)
                    {
                        GenerateArrayTypeClass(arrayTypeInfo, ilName, arrayTypeInfo.ElementType is PrimitiveTypeInfo ? null : ResolveIlTypeName(arrayTypeInfo.ElementType));
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
                foreach(var k in TypeIdentifierToIlName.Keys)
                {
                    TypeIdentifierToIlName[k].id = 0;
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
                    if (!TypeIdentifierToIlName.ContainsKey(typeDec.Name))
                    {
                        TypeIdentifierToIlName.Add(typeDec.Name, new IlInfo());
                    }
                    TypeIdentifierToIlName[typeDec.Name].id++;
                    string ilName = $"{typeDec.Name}@{context}@{TypeIdentifierToIlName[typeDec.Name].id}";
                    TypeIdentifierToIlName[typeDec.Name].names.Push(ilName);
                    scope.Add(typeDec.Name);

                    if (type is ArrayTypeInfo arrayTypeInfo)
                    {
                        GenerateArrayTypeClass(arrayTypeInfo, ilName, arrayTypeInfo.ElementType is PrimitiveTypeInfo ? null : ResolveIlTypeName(arrayTypeInfo.ElementType));
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
                TypeIdentifierToIlName[el].names.Pop();
            }
            
        }

        private string? ResolveIlTypeName(TypeInfo typeInfo)
        {
            string? ilTypeName = null;
            if(typeInfo is PrimitiveTypeInfo p)
            {
                return p.Type switch
                    {
                        PrimitiveType.Integer => "int32",
                        PrimitiveType.Boolean => "int32",
                        PrimitiveType.Real => "float32",
                        _ => throw new Exception("Unsupported primitive")
                    };
            }
            else if (typeInfo is ArrayTypeInfo a)
            {
                ilTypeName = "valuetype " + TypeIdentifierToIlName[a.Name].names.Peek();
            }
            else if (typeInfo is RecordTypeInfo r)
            {
                ilTypeName = "valuetype " + TypeIdentifierToIlName[r.Name].names.Peek();
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
                ilFieldType = $"{objectName!}";
                ilNewArrType = $"{objectName}";
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
            _writer.WriteLine($".method public hidebysig instance {ilFieldType}{(isStruct ? "&":"")} get_Item(int32 index) cil managed");
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

            _writer.Write(".method public hidebysig specialname rtspecialname instance void .ctor() cil managed");

            _writer.WriteLine("{");
            _writer.WriteLine(".locals init (");
            int i = 0;
            foreach (var (fieldName, ilFieldType) in ilFieldTypes)
            {
                if (ilFieldType != "int32" && ilFieldType != "float32")
                {
                    if (i != 0)
                    {
                        _writer.Write(",");
                    }
                    _writer.WriteLine($"[{i}] {ilFieldType} tempPoint_{i}");
                    i++;
                }
            }
            _writer.WriteLine(")");
            _writer.WriteLine(".maxstack 8");

            _writer.WriteLine("ldarg.0");
            _writer.WriteLine("call instance void [mscorlib]System.ValueType::.ctor()");
            i = 0;
            foreach (var (fieldName, ilFieldType) in ilFieldTypes)
            {
                _writer.WriteLine("ldarg.0");
                if (ilFieldType == "int32")
                {
                    _writer.WriteLine("ldc.i4.0");
                    _writer.WriteLine($"stfld {ilFieldType} {className}::{fieldName}");
                }
                else if(ilFieldType == "float32")
                {
                    _writer.WriteLine("ldc.i4.0");
                    _writer.WriteLine("conv.r4");
                    _writer.WriteLine($"stfld {ilFieldType} {className}::{fieldName}");
                }
                else
                {
                    _writer.WriteLine($"ldloca.s {i}");
                    i++;
                    _writer.WriteLine($"call instance void {ilFieldType}::.ctor()");
                    _writer.WriteLine($"stfld {ilFieldType} {className}::{fieldName}");
                }
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
                    return $"{objectNames[fieldName]}";

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
            foreach(var variable in AST.declarations.OfType<VariableDeclarationNode>())
            {
                string typeName = "";
                TypeInfo type = variable.VariableSymbol!.Type;
                if(type is PrimitiveTypeInfo primitiveType)
                {
                    switch (primitiveType.Type)
                    {
                        case PrimitiveType.Real:
                            typeName = "float32";
                            _writer.WriteLine($".field public static float32 {variable.Name}");
                            break;
                        case PrimitiveType.Integer:
                        case PrimitiveType.Boolean:
                            typeName = "int32";
                            _writer.WriteLine($".field public static int32 {variable.Name}");
                            break;
                    }
                }
                else if(type is ArrayTypeInfo arrayType)
                {   
                    typeName = TypeIdentifierToIlName[arrayType.Name].names.Peek();
                    _writer.WriteLine($".field public static valuetype {TypeIdentifierToIlName[arrayType.Name].names.Peek()} {variable.Name}");
                }
                else if(type is RecordTypeInfo recordType)
                {   
                    typeName = TypeIdentifierToIlName[recordType.Name].names.Peek();
                    _writer.WriteLine($".field public static valuetype {TypeIdentifierToIlName[recordType.Name].names.Peek()} {variable.Name}");
                }
                else throw new Exception("Hehe ;3");

                VariableIdentifierToIlName[variable.Name] = new IlInfo();
                VariableIdentifierToIlName[variable.Name].names.Push(variable.VariableSymbol.Type is PrimitiveTypeInfo ? $"sfld " : $"sflda valuetype " + $"{typeName} Program::{variable.Name}");
                VariableIdentifierToIlName[variable.Name].id = 0;
            }
        }

        private void WriteEntrypointMethod(ProgramNode AST)
        {
            List<RoutineSymbol> routines = new List<RoutineSymbol>();
            foreach(var routine in AST.declarations.OfType<RoutineDeclarationNode>())
            {
                if (!routine.RoutineSymbol!.IsForwardDeclared) routines.Add(routine.RoutineSymbol!);
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
            _writer.WriteLine("br WRONG_ROUTINE");
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
                _writer.WriteLine($"ldc.i4.{argumentTypes.Count+1}");
                _writer.WriteLine("blt TOO_FEW_ARGS");
                _writer.WriteLine("");
                _writer.WriteLine("ldarg.0");
                _writer.WriteLine("ldlen");
                _writer.WriteLine("conv.i4");
                _writer.WriteLine($"ldc.i4.{argumentTypes.Count+1}");
                _writer.WriteLine("bgt TOO_MANY_ARGS");
                _writer.WriteLine("");

                string parameters = "";
                for(int i = 0; i < argumentTypes.Count; i++)
                {
                    string argType = argumentTypes[i];
                    
                    parameters += argType;

                    if(i != argumentTypes.Count - 1)
                    {
                        parameters += ",";
                    }
                    

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
                _writer.WriteLine($"call {(routine.ReturnType == null ? "void" : ResolveIlTypeName(routine.ReturnType))} Program::{routine.Name}({parameters})");
                if(routine.ReturnType != null)
                {
                    _writer.WriteLine("pop");
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

            _writer.WriteLine("WRONG_ROUTINE:");
            _writer.WriteLine("ldstr \"Error: a non-existent entry point method was entered!\"");
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
            foreach (var routine in AST.declarations.OfType<RoutineDeclarationNode>())
            {
                if (routine.RoutineSymbol!.IsForwardDeclared) continue;
                string returnTypeString = "";
                if (routine.RoutineSymbol!.ReturnType is PrimitiveTypeInfo p)
                {
                    switch (p.Type)
                    {
                        case PrimitiveType.Boolean:
                        case PrimitiveType.Integer:
                            returnTypeString = "int32";
                            break;
                        case PrimitiveType.Real:
                            returnTypeString = "float32";
                            break;
                    }
                }
                else if (routine.RoutineSymbol!.ReturnType is ArrayTypeInfo a)
                {
                    returnTypeString = TypeIdentifierToIlName[a.Name].names.Peek();
                }
                else if (routine.RoutineSymbol!.ReturnType is RecordTypeInfo r)
                {
                    returnTypeString = TypeIdentifierToIlName[r.Name].names.Peek();
                } 
                else
                {
                    returnTypeString = "void";
                }
                int argCount = 0;
                foreach (var arg in routine.Parameters)
                {
                    if (!VariableIdentifierToIlName.ContainsKey(arg.Name))
                    {
                        VariableIdentifierToIlName[arg.Name] = new IlInfo();
                    }
                    VariableIdentifierToIlName[arg.Name].names.Push((arg.VariableSymbol!.Type is PrimitiveTypeInfo ? "arg" : "arga") + $".s {argCount}");
                    argCount++;
                }

                _writer.WriteLine($".method public static {returnTypeString} {routine.Name}({GenerateRoutineArguments(routine.Parameters)}) cil managed");
                _writer.WriteLine("{");
                GenerateRoutineBody(routine.Body!, routine.Name);
                _writer.WriteLine("ret");
                _writer.WriteLine("}");
            }
        }

        private string GenerateRoutineArguments(List<VariableDeclarationNode> arguments)
        {
            string result = "";
            for (int i = 0; i < arguments.Count(); i++)
            {
                var argument = arguments[i];
                if (argument.VariableSymbol!.Type is PrimitiveTypeInfo p)
                {
                    string typeString = "";
                    switch (p.Type)
                    {
                        case PrimitiveType.Boolean:
                        case PrimitiveType.Integer:
                            typeString = "int32";
                            break;
                        case PrimitiveType.Real:
                            typeString = "float32";
                            break;
                    }
                    result += $"{typeString}";
                }
                else if (argument.VariableSymbol!.Type is ArrayTypeInfo a)
                {
                    result += $"valuetype {TypeIdentifierToIlName[a.Name].names.Peek()}";
                } 
                else if (argument.VariableSymbol!.Type is RecordTypeInfo r)
                {
                    result += $"valuetype {TypeIdentifierToIlName[r.Name].names.Peek()}";    
                }

                if (i != arguments.Count() - 1)
                {
                    result += ", ";
                }
            }
            return result;
        }

        private void GenerateRoutineBody(RoutineBodyNode body, string context)
        {


            if (body is ExpressionRoutineBodyNode expression)
            {
                
            }
            else if(body is BlockRoutineBodyNode blockBody)
            {
                localsCounter = 0;
                _writer.WriteLine(".locals init (");
                GenerateLocals(blockBody.Body, context);
                foreach(var k in TypeIdentifierToIlName.Keys)
                {
                    TypeIdentifierToIlName[k].id = 0;
                }
                localsCounter = 0;
                _writer.WriteLine(")");
                GenerateScopeBody(blockBody.Body, context, 0);
                foreach(var k in TypeIdentifierToIlName.Keys)
                {
                    TypeIdentifierToIlName[k].id = 0;
                }
            }
        }

        private int localsCounter = 0;
        private void GenerateLocals(List<Node> body, string context)
        
        {
            List<string> typeScope = new List<string>();
            List<string> varScope = new List<string>();
            foreach(Node node in body)
            {
                
                if(node is VariableDeclarationNode varDec)
                {
                    if (localsCounter!=0)
                    {
                        _writer.Write(",");
                    }
                    
                    TypeInfo type = varDec.VariableSymbol!.Type;
                    if (!VariableIdentifierToIlName.ContainsKey(varDec.Name))
                    {
                        VariableIdentifierToIlName.Add(varDec.Name, new IlInfo());
                    }
                    VariableIdentifierToIlName[varDec.Name].id++;
                    string ilName = $"{varDec.Name}@{VariableIdentifierToIlName[varDec.Name].id}";
                    varScope.Add(varDec.Name);

                    if(type is PrimitiveTypeInfo primitiveTypeInfo)
                    {
                        VariableIdentifierToIlName[varDec.Name].names.Push($"loc {localsCounter}");
                        _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(primitiveTypeInfo)} {ilName}");
                    }
                    if (type is ArrayTypeInfo arrayTypeInfo)
                    {
                        VariableIdentifierToIlName[varDec.Name].names.Push($"loca {localsCounter}");
                        _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(arrayTypeInfo)} {ilName}");
                    }
                    else if(type is RecordTypeInfo recordTypeInfo)
                    {
                        VariableIdentifierToIlName[varDec.Name].names.Push($"loca {localsCounter}");
                        _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(recordTypeInfo)} {ilName}");
                    }
                    localsCounter++;
                }
                else if(node is IfStatementNode ifNode)
                {
                    GenerateLocals(ifNode.ThenBody, context);
                    if(ifNode.ElseBody != null)
                    {
                        GenerateLocals(ifNode.ElseBody, context);
                    }
                }
                else if(node is ForLoopNode forNode)
                {
                    if (forNode.IsArrayTraversal)
                    {
                        if (localsCounter!=0)
                        {
                            _writer.Write(",");
                        }
                        
                        TypeInfo type = new PrimitiveTypeInfo(PrimitiveType.Integer);
                        if (!VariableIdentifierToIlName.ContainsKey(forNode.Iterator))
                        {
                            VariableIdentifierToIlName.Add(forNode.Iterator, new IlInfo());
                        }
                        VariableIdentifierToIlName[forNode.Iterator].id++;
                        string ilName = $"{forNode.Iterator}@{VariableIdentifierToIlName[forNode.Iterator].id}";
                        varScope.Add(forNode.Iterator);

                        if(type is PrimitiveTypeInfo primitiveTypeInfo)
                        {
                            VariableIdentifierToIlName[forNode.Iterator].names.Push($"loc {localsCounter}");
                            _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(primitiveTypeInfo)} {ilName}");
                        }
                        localsCounter++;


                        if (localsCounter!=0)
                        {
                            _writer.Write(",");
                        }
                        
                        type = ((ArrayTypeInfo)forNode.Range.Start.ResolvedType!).ElementType;
                        if (!VariableIdentifierToIlName.ContainsKey(forNode.Iterator))
                        {
                            VariableIdentifierToIlName.Add(forNode.Iterator, new IlInfo());
                        }
                        VariableIdentifierToIlName[forNode.Iterator].id++;
                        ilName = $"{forNode.Iterator}@{VariableIdentifierToIlName[forNode.Iterator].id}";
                        varScope.Add(forNode.Iterator);

                        if(type is PrimitiveTypeInfo primitiveTypeInfo1)
                        {
                            VariableIdentifierToIlName[forNode.Iterator].names.Push($"loc {localsCounter}");
                            _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(primitiveTypeInfo1)} {ilName}");
                        }
                        if (type is ArrayTypeInfo arrayTypeInfo)
                        {
                            VariableIdentifierToIlName[forNode.Iterator].names.Push($"loca {localsCounter}");
                            _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(arrayTypeInfo)} {ilName}");
                        }
                        else if(type is RecordTypeInfo recordTypeInfo)
                        {
                            VariableIdentifierToIlName[forNode.Iterator].names.Push($"loca {localsCounter}");
                            _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(recordTypeInfo)} {ilName}");
                        }
                        localsCounter++;
                    } 
                    else
                    {
                        if (localsCounter!=0)
                        {
                            _writer.Write(",");
                        }
                        
                        TypeInfo type = new PrimitiveTypeInfo(PrimitiveType.Integer);
                        if (!VariableIdentifierToIlName.ContainsKey(forNode.Iterator))
                        {
                            VariableIdentifierToIlName.Add(forNode.Iterator, new IlInfo());
                        }
                        VariableIdentifierToIlName[forNode.Iterator].id++;
                        string ilName = $"{forNode.Iterator}@{VariableIdentifierToIlName[forNode.Iterator].id}";
                        varScope.Add(forNode.Iterator);

                        if(type is PrimitiveTypeInfo primitiveTypeInfo)
                        {
                            VariableIdentifierToIlName[forNode.Iterator].names.Push($"loc {localsCounter}");
                            _writer.WriteLine($"[{localsCounter}] {ResolveIlTypeName(primitiveTypeInfo)} {ilName}");
                        }
                        localsCounter++;
                    }
                    GenerateLocals(forNode.Body, context);
                }
                else if(node is WhileLoopNode whileNode)
                {
                    GenerateLocals(whileNode.Body, context);
                }
                else if(node is TypeDeclarationNode typeDec)
                {
                    TypeInfo type = typeDec.TypeSymbol!.Type;

                    if (type is PrimitiveTypeInfo) continue;
                    if (!TypeIdentifierToIlName.ContainsKey(typeDec.Name))
                    {
                        TypeIdentifierToIlName.Add(typeDec.Name, new IlInfo());
                    }
                    TypeIdentifierToIlName[typeDec.Name].id++;
                    string ilName = $"{typeDec.Name}@{context}@{TypeIdentifierToIlName[typeDec.Name].id}";
                    TypeIdentifierToIlName[typeDec.Name].names.Push(ilName);
                    typeScope.Add(typeDec.Name);
                }
            
            }

            foreach(var el in typeScope)
            {
                TypeIdentifierToIlName[el].names.Pop();
            }

            foreach(var el in varScope)
            {
                VariableIdentifierToIlName[el].names.Pop();
            }
            
        }

        
        private int GenerateScopeBody(List<Node> body, string context, int bodyCount)
        {
            List<string> typeScope = new List<string>();
            List<string> varScope = new List<string>();

            foreach (var node in body)
            {
                if (node is VariableDeclarationNode varDec)
                {
                    
                    TypeInfo type = varDec.VariableSymbol!.Type;
                    if (!VariableIdentifierToIlName.ContainsKey(varDec.Name))
                    {
                        VariableIdentifierToIlName.Add(varDec.Name, new IlInfo());
                    }
                    VariableIdentifierToIlName[varDec.Name].id++;
                    string ilName = $"{varDec.Name}@{VariableIdentifierToIlName[varDec.Name].id}";
                    varScope.Add(varDec.Name);

                    if(type is PrimitiveTypeInfo primitiveTypeInfo)
                    {
                        VariableIdentifierToIlName[varDec.Name].names.Push($"loc.{localsCounter}");
                    }
                    else if (type is ArrayTypeInfo arrayTypeInfo)
                    {
                        VariableIdentifierToIlName[varDec.Name].names.Push($"loca {localsCounter}");
                        _writer.WriteLine($"ld{VariableIdentifierToIlName[varDec.Name].names.Peek()}");
                        _writer.WriteLine($"call instance void {ResolveIlTypeName(arrayTypeInfo)}::.ctor()");
                    }
                    else if(type is RecordTypeInfo recordTypeInfo)
                    {
                        VariableIdentifierToIlName[varDec.Name].names.Push($"loca {localsCounter}");
                        _writer.WriteLine($"ld{VariableIdentifierToIlName[varDec.Name].names.Peek()}");
                        _writer.WriteLine($"call instance void {ResolveIlTypeName(recordTypeInfo)}::.ctor()");
                    }
                    if(varDec.Initializer != null)
                    {
                        WriteExpression(varDec.Initializer);
                        if((varDec.Initializer.ResolvedType is PrimitiveTypeInfo p1) 
                            && (varDec.VariableSymbol.Type is PrimitiveTypeInfo p2)
                            && (p1.Type != p2.Type))
                        {
                            _writer.WriteLine($"conv.{(p2.Type is PrimitiveType.Real ? "r4" : "i4")}");
                        }
                        _writer.WriteLine($"st{VariableIdentifierToIlName[varDec.Name].names.Peek()}");
                    }
                    localsCounter++;
                }
                else if(node is TypeDeclarationNode typeDec)
                {
                    TypeInfo type = typeDec.TypeSymbol!.Type;

                    if (type is PrimitiveTypeInfo) continue;
                    if (!TypeIdentifierToIlName.ContainsKey(typeDec.Name))
                    {
                        TypeIdentifierToIlName.Add(typeDec.Name, new IlInfo());
                    }
                    TypeIdentifierToIlName[typeDec.Name].id++;
                    string ilName = $"{typeDec.Name}@{context}@{TypeIdentifierToIlName[typeDec.Name].id}";
                    TypeIdentifierToIlName[typeDec.Name].names.Push(ilName);
                    typeScope.Add(typeDec.Name);
                }
                else if(node is PrintStatementNode print)
                {
                    foreach(var expression in print.Expressions)
                    {
                        WriteExpression(expression);
                        if(((PrimitiveTypeInfo)expression.ResolvedType!).Type is PrimitiveType.Integer)
                        {
                            _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(int32)");
                        }
                        else if((((PrimitiveTypeInfo)(expression.ResolvedType)).Type is PrimitiveType.Boolean) 
                            || (((PrimitiveTypeInfo)(expression.ResolvedType)).Type is PrimitiveType.Real))
                        {
                            _writer.WriteLine("call void [mscorlib]System.Console::WriteLine(float32)");
                        }
                    }
                } 
                else if (node is AssignmentNode assignmentNode)
                {
                    Assignment(assignmentNode);
                }
                else if (node is ForLoopNode forLoopNode)
                {
                    bodyCount++;
                    if (forLoopNode.IsArrayTraversal)
                    {
                        int Start = 0;
                        int End = ((ArrayTypeInfo)forLoopNode.Range.Start.ResolvedType!).Size;
                        


                        TypeInfo type = new PrimitiveTypeInfo(PrimitiveType.Integer);
                        
                        if (!VariableIdentifierToIlName.ContainsKey(forLoopNode.Iterator))
                        {
                            VariableIdentifierToIlName.Add(forLoopNode.Iterator, new IlInfo());
                        }
                        VariableIdentifierToIlName[forLoopNode.Iterator].id++;
                        string ilName = $"{forLoopNode.Iterator}@{VariableIdentifierToIlName[forLoopNode.Iterator].id}";
                        varScope.Add(forLoopNode.Iterator);

                        if(type is PrimitiveTypeInfo primitiveTypeInfo)
                        {
                            VariableIdentifierToIlName[forLoopNode.Iterator].names.Push($"loc.{localsCounter}");
                        }
                        localsCounter++;

                        string i_int = VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek();


                        type = ((ArrayTypeInfo)forLoopNode.Range.Start.ResolvedType!).ElementType;
                        TypeInfo oldType = (ArrayTypeInfo)forLoopNode.Range.Start.ResolvedType!;
                        if (!VariableIdentifierToIlName.ContainsKey(forLoopNode.Iterator))
                        {
                            VariableIdentifierToIlName.Add(forLoopNode.Iterator, new IlInfo());
                        }
                        VariableIdentifierToIlName[forLoopNode.Iterator].id++;
                        ilName = $"{forLoopNode.Iterator}@{VariableIdentifierToIlName[forLoopNode.Iterator].id}";
                        varScope.Add(forLoopNode.Iterator);

                        if(type is PrimitiveTypeInfo primitiveTypeInfo1)
                        {
                            VariableIdentifierToIlName[forLoopNode.Iterator].names.Push($"loc.{localsCounter}");
                        }
                        else if (type is ArrayTypeInfo arrayTypeInfo)
                        {
                            VariableIdentifierToIlName[forLoopNode.Iterator].names.Push($"loca {localsCounter}");
                            _writer.WriteLine($"ld{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                            _writer.WriteLine($"call instance void {ResolveIlTypeName(arrayTypeInfo)}::.ctor()");
                        }
                        else if(type is RecordTypeInfo recordTypeInfo)
                        {
                            VariableIdentifierToIlName[forLoopNode.Iterator].names.Push($"loca {localsCounter}");
                            _writer.WriteLine($"ld{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                            _writer.WriteLine($"call instance void {ResolveIlTypeName(recordTypeInfo)}::.ctor()");
                        }
                        localsCounter++;


                        _writer.WriteLine($"ld.i4 {(!forLoopNode.Reverse ? Start : End-1)}");
                        _writer.WriteLine($"st{i_int}");
                        WriteExpression(forLoopNode.Range.Start);
                        _writer.WriteLine($"ld{i_int}");
                        _writer.WriteLine($"call instance {ResolveIlTypeName(type)}{(type is PrimitiveTypeInfo ? "" : "&")} {ResolveIlTypeName(oldType)}::get_Item(int32)");
                        _writer.WriteLine($"st{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                        
                        
                        _writer.WriteLine($"br.s CHECK_LOOP_CONDITION_{bodyCount}");
                        _writer.WriteLine();
                        _writer.WriteLine($"LOOP_BODY_{bodyCount}:");
                        int bodiesInside = GenerateScopeBody(forLoopNode.Body, context, bodyCount);


                        _writer.WriteLine($"ld{i_int}");
                        _writer.WriteLine($"ldc.i4 {(forLoopNode.Reverse ? "-1" : "1")}");
                        _writer.WriteLine("add");
                        _writer.WriteLine($"st{i_int}");

                        WriteExpression(forLoopNode.Range.Start);
                        _writer.WriteLine($"ld{i_int}");
                        _writer.WriteLine($"call instance {ResolveIlTypeName(type)}{(type is PrimitiveTypeInfo ? "" : "&")} {ResolveIlTypeName(oldType)}::get_Item(int32)");
                        _writer.WriteLine($"st{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");


                        _writer.WriteLine();
                        _writer.WriteLine($"CHECK_LOOP_CONDITION_{bodyCount}:");

                        _writer.WriteLine($"ld{i_int}");
                        _writer.WriteLine($"ld.i4 {(!forLoopNode.Reverse ? End-1 : Start)}");
                        _writer.WriteLine($"{(forLoopNode.Reverse ? "bge" : "ble")}.s LOOP_BODY_{bodyCount}");
                        _writer.WriteLine();
                        _writer.WriteLine($"LOOP_END_{bodyCount}:");
                        bodyCount = bodiesInside;

                    }
                    else
                    {
                        TypeInfo type = new PrimitiveTypeInfo(PrimitiveType.Integer);
                        if (!VariableIdentifierToIlName.ContainsKey(forLoopNode.Iterator))
                        {
                            VariableIdentifierToIlName.Add(forLoopNode.Iterator, new IlInfo());
                        }
                        VariableIdentifierToIlName[forLoopNode.Iterator].id++;
                        string ilName = $"{forLoopNode.Iterator}@{VariableIdentifierToIlName[forLoopNode.Iterator].id}";
                        varScope.Add(forLoopNode.Iterator);

                        if(type is PrimitiveTypeInfo primitiveTypeInfo)
                        {
                            VariableIdentifierToIlName[forLoopNode.Iterator].names.Push($"loc.{localsCounter}");
                        }
                        localsCounter++;
                        WriteExpression(!forLoopNode.Reverse ? forLoopNode.Range.Start : forLoopNode.Range.End!);
                        _writer.WriteLine($"st{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                        _writer.WriteLine($"br.s CHECK_LOOP_CONDITION_{bodyCount}");
                        _writer.WriteLine();
                        _writer.WriteLine($"LOOP_BODY_{bodyCount}:");
                        int bodiesInside = GenerateScopeBody(forLoopNode.Body, context, bodyCount);

                        _writer.WriteLine($"ld{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                        _writer.WriteLine($"ldc.i4 {(forLoopNode.Reverse ? "-1" : "1")}");
                        _writer.WriteLine("add");
                        _writer.WriteLine($"st{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                        _writer.WriteLine();
                        _writer.WriteLine($"CHECK_LOOP_CONDITION_{bodyCount}:");

                        _writer.WriteLine($"ld{VariableIdentifierToIlName[forLoopNode.Iterator].names.Peek()}");
                        WriteExpression(forLoopNode.Reverse ? forLoopNode.Range.Start : forLoopNode.Range.End!);
                        _writer.WriteLine($"{(forLoopNode.Reverse ? "bge" : "ble")}.s LOOP_BODY_{bodyCount}");
                        _writer.WriteLine();
                        _writer.WriteLine($"LOOP_END_{bodyCount}:");
                        bodyCount = bodiesInside;
                    }
                }
                else if (node is IfStatementNode ifStatementNode)
                {
                    bodyCount++;
                    WriteExpression(ifStatementNode.Condition);
                    _writer.WriteLine($"brfalse.s {(ifStatementNode.ElseBody != null && ifStatementNode.ElseBody.Count() > 0 ? $"ELSE_{bodyCount}" : $"IF_END_{bodyCount}")}");
                    var bodiesInside = GenerateScopeBody(ifStatementNode.ThenBody, context, bodyCount) - bodyCount;
                    _writer.WriteLine($"br IF_END_{bodyCount}");

                    if (ifStatementNode.ElseBody != null && ifStatementNode.ElseBody.Count() > 0)
                    {
                        _writer.WriteLine($"ELSE_{bodyCount}:");
                        bodiesInside += GenerateScopeBody(ifStatementNode.ElseBody, context, bodyCount) - bodyCount;
                    }

                    _writer.WriteLine($"IF_END_{bodyCount}:");
                    bodyCount = bodiesInside;

                }
                else if (node is ReturnStatementNode returnStatementNode)
                {
                    if (returnStatementNode.Value != null)
                    {
                        WriteExpression(returnStatementNode.Value);
                    }
                    _writer.WriteLine("ret");
                }
                else if (node is RoutineCallStatementNode routineCallStatementNode)
                {
                    WriteExpression(routineCallStatementNode.Call);

                    if (routineCallStatementNode.Call.RoutineSymbol!.ReturnType != null)
                    {
                        _writer.WriteLine("pop");
                    }
                }
                else if (node is WhileLoopNode whileLoopNode)
                {
                    bodyCount++;
                    _writer.WriteLine($"br.s CHECK_LOOP_CONDITION_{bodyCount}");

                    _writer.WriteLine($"LOOP_BODY_{bodyCount}:");
                    int bodiesInside = GenerateScopeBody(whileLoopNode.Body, context, bodyCount);
                    _writer.WriteLine("");
                    _writer.WriteLine($"CHECK_LOOP_CONDITION_{bodyCount}:");
                    WriteExpression(whileLoopNode.Condition);
                    _writer.WriteLine($"brtrue.s LOOP_BODY_{bodyCount}");
                    _writer.WriteLine("");
                    _writer.WriteLine($"LOOP_END_{bodyCount}:");
                    bodyCount = bodiesInside;
                }

                _writer.WriteLine("");
            }

            foreach(var el in typeScope)
            {
                TypeIdentifierToIlName[el].names.Pop();
            }

            foreach(var el in varScope)
            {
                VariableIdentifierToIlName[el].names.Pop();
            }

            return bodyCount;
        }

        private void Assignment(AssignmentNode assignmentNode)
        {
            ModifiablePrimaryNode modifiablePrimary = assignmentNode.Target;
            TypeInfo type = modifiablePrimary.VariableSymbol!.Type;
            TypeInfo oldtype = type;
            
            
            if(modifiablePrimary.AccessPart.Count == 0)
            {
                WriteExpression(assignmentNode.Value);
                _writer.WriteLine($"st{VariableIdentifierToIlName[assignmentNode.Target.BaseName].names.Peek()}");
                return;
            }

            _writer.WriteLine($"ld{VariableIdentifierToIlName[modifiablePrimary.BaseName].names.Peek()}");

            for(int i = 0; i < modifiablePrimary.AccessPart.Count; i++)
            {
                AccessPart accessPart = modifiablePrimary.AccessPart[i];
                oldtype = type;

                if(accessPart is FieldAccess fieldAccess)
                {

                    type = ((RecordTypeInfo)type).Fields[fieldAccess.Name];
                    if(i == modifiablePrimary.AccessPart.Count - 1)
                    {
                        WriteExpression(assignmentNode.Value);
                        _writer.WriteLine($"stfld {ResolveIlTypeName(type)} {ResolveIlTypeName(oldtype)}::{fieldAccess.Name}");
                    }
                    else
                    {
                        _writer.WriteLine($"ldflda {ResolveIlTypeName(type)} {ResolveIlTypeName(oldtype)}::{fieldAccess.Name}");
                    }
                    
                }
                else if(accessPart is ArrayAccess arrayAccess)
                {
                    if(i == modifiablePrimary.AccessPart.Count - 1)
                    {
                        type = ((ArrayTypeInfo)type).ElementType;
                        WriteExpression(arrayAccess.Index);
                        WriteExpression(assignmentNode.Value);
                        _writer.WriteLine($"call instance void {ResolveIlTypeName(oldtype)}::set_Item(int32,{ResolveIlTypeName(type)})");
                    }
                    else
                    {
                        type = ((ArrayTypeInfo)type).ElementType;
                        WriteExpression(arrayAccess.Index);
                        _writer.WriteLine($"call instance {ResolveIlTypeName(type)}{(type is PrimitiveTypeInfo ? "" : "&")} {ResolveIlTypeName(oldtype)}::get_Item(int32)");
                    }
                }
            }
        }

        private void WriteExpression(ExpressionNode expression)
        {
            if(expression is BinaryExpressionNode binaryExpression)
            {
                WriteExpression(binaryExpression.Left);
                if (expression.ResolvedType is PrimitiveTypeInfo {Type : PrimitiveType.Real}) _writer.WriteLine("conv.r4");
                WriteExpression(binaryExpression.Right);

                switch (binaryExpression.Operator)
                {
                    case Operator.Plus:
                        _writer.WriteLine("add");
                        break;
                    case Operator.Minus:
                        _writer.WriteLine("sub");
                        break;
                    case Operator.Multiply:
                        _writer.WriteLine("mul");
                        break;
                    case Operator.Divide:
                        _writer.WriteLine("conv.r4");
                        _writer.WriteLine("div");
                        break;
                    case Operator.Modulo:
                        _writer.WriteLine("rem");
                        break;
                    case Operator.Less:
                        _writer.WriteLine("clt");
                        break;
                    case Operator.Greater:
                        _writer.WriteLine("cgt");
                        break;
                    case Operator.Equal:
                        _writer.WriteLine("ceq");
                        break;
                    case Operator.NotEqual:
                        _writer.WriteLine("ceq");
                        _writer.WriteLine("ldc.i4.0");
                        _writer.WriteLine("ceq");
                        break;
                    case Operator.LessEqual:
                        _writer.WriteLine("cgt");
                        _writer.WriteLine("ldc.i4.0");
                        _writer.WriteLine("ceq");
                        break;
                    case Operator.GreaterEqual:
                        _writer.WriteLine("clt");
                        _writer.WriteLine("ldc.i4.0");
                        _writer.WriteLine("ceq");
                        break;
                    case Operator.And:
                        _writer.WriteLine("and");
                        break;
                    case Operator.Or:
                        _writer.WriteLine("or");
                        break;
                    case Operator.Xor:
                        _writer.WriteLine("xor");
                        break;
                }
            }
            else if(expression is UnaryExpressionNode unaryExpression)
            {
                WriteExpression(unaryExpression);
                switch (unaryExpression.Operator)
                {
                    case UnaryOperator.Plus:
                        _writer.WriteLine("call int32 [mscorlib]System.Math::Abs(int32)");
                        break;
                    case UnaryOperator.Not:
                        _writer.WriteLine("ldc.i4.0");
                        _writer.WriteLine("ceq");
                        break;
                    case UnaryOperator.Minus:
                        _writer.WriteLine("neg");
                        break;
                }
            }
            else if(expression is LiteralNode literal)
            {
                if (literal.Value is int i)
                {
                    _writer.WriteLine($"ldc.i4 {i}");
                }
                else if (literal.Value is double f)
                {
                    _writer.WriteLine($"ldc.r4 {f}");
                }
                else if (literal.Value is bool b)
                {
                    _writer.WriteLine($"ldc.i4 {(b ? "1" : "0")}");
                }
            }
            else if(expression is ModifiablePrimaryNode modifiablePrimary)
            {
                TypeInfo type = modifiablePrimary.VariableSymbol!.Type;
                _writer.WriteLine($"ld{VariableIdentifierToIlName[modifiablePrimary.BaseName].names.Peek()}");
                foreach(var accessPart in modifiablePrimary.AccessPart)
                {
                    TypeInfo oldtype = type;

                    if(accessPart is FieldAccess fieldAccess)
                    {
                        
                        type = ((RecordTypeInfo)type).Fields[fieldAccess.Name];
                        if(type is PrimitiveTypeInfo primitiveType)
                        {
                            _writer.WriteLine($"ldfld {ResolveIlTypeName(primitiveType)} {ResolveIlTypeName(oldtype)}::{fieldAccess.Name}");
                        }
                        else if(type is ArrayTypeInfo arrayType)
                        {
                            _writer.WriteLine($"ldflda {ResolveIlTypeName(arrayType)} {ResolveIlTypeName(oldtype)}::{fieldAccess.Name}");
                        }
                        else if(type is RecordTypeInfo recordType)
                        {
                            _writer.WriteLine($"ldflda {ResolveIlTypeName(recordType)} {ResolveIlTypeName(oldtype)}::{fieldAccess.Name}");
                        }
                    }
                    else if(accessPart is ArrayAccess arrayAccess)
                    {
                        type = ((ArrayTypeInfo)type).ElementType;
                        WriteExpression(arrayAccess.Index);
                        _writer.WriteLine($"call instance {ResolveIlTypeName(type)}{(type is PrimitiveTypeInfo ? "" : "&")} {ResolveIlTypeName(oldtype)}::get_Item(int32)");
                    }
                }
            }
            else if(expression is RoutineCallNode routineCall)
            {
                string parameterList = "";
                for (int i = 0; i < routineCall.RoutineSymbol!.Parameters.Count(); i++)
                {
                    TypeInfo type = routineCall.RoutineSymbol!.Parameters[i].Type;
                    parameterList += ResolveIlTypeName(type);
                    if (i != routineCall.RoutineSymbol!.Parameters.Count() - 1) parameterList += ", ";
                }

                foreach(var parameter in routineCall.Arguments)
                {
                    WriteExpression(parameter);
                }
                _writer.WriteLine($"call {(routineCall.RoutineSymbol!.ReturnType == null ? "void" : ResolveIlTypeName(routineCall.RoutineSymbol!.ReturnType))} Program::{routineCall.Name}({parameterList})");
            }
            else throw new Exception("Expression is not expression");
        }
    }
}