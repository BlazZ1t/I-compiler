using Newtonsoft.Json;
using ImperativeLang.SyntaxAnalyzer;
using ImperativeLang.SemanticalAnalyzerNS;
using Newtonsoft.Json.Converters;
using ImperativeLang.CodeGen;
using System.Diagnostics;

namespace ImperativeLang
{
    class Program
    {
        static bool verbose = false;
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "compile":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine("Error: Missing file path");
                        return;
                    }
                    if (args.Contains("-v"))
                    {
                        verbose = true;
                    }
                    HandleCompile(args[1]);
                    break;
                default:
                    System.Console.WriteLine($"Unkown command: {command}");
                    PrintUsage();
                    break;
            }
        }

        private static void HandleCompile(string filePath)
        {
            if (!filePath.EndsWith(".impp", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("Error: File must have .impp extension.");
                return;
            }

            if (!File.Exists(filePath))
            {
                System.Console.WriteLine($"Error: File '{filePath}' not found");
                return;
            }

            try
            {
                Lexer lexer = new Lexer(File.ReadAllText(filePath));
                List<Token> tokens = lexer.Tokenize().ToList();
                tokens = lexer.CleanUp(tokens);
                if (verbose)
                {
                    System.Console.WriteLine("Tokens:");
                    foreach (var token in tokens)
                    {
                        System.Console.WriteLine(token);
                    }
                }

                Parser parser = new Parser(tokens);
                ProgramNode programNode = parser.getAST();
                if (verbose)
                {
                    System.Console.WriteLine("AST (not fully representative):");
                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    settings.Converters.Add(new StringEnumConverter());
                    string json = JsonConvert.SerializeObject(programNode, settings);
                    System.Console.WriteLine(json);
                }

                SemanticalAnalyzer semanticalAnalyzer = new SemanticalAnalyzer(programNode);
                semanticalAnalyzer.Analyze();

                if (verbose)
                {
                    System.Console.WriteLine("AAST (not fully representative):");
                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    settings.Converters.Add(new StringEnumConverter());
                    string json = JsonConvert.SerializeObject(programNode, settings);
                    System.Console.WriteLine(json);
                }

                string outputPath = Path.GetFileNameWithoutExtension(filePath);
                var codegen = new CodeGenerator(new StreamWriter($"{outputPath}.il"));
                codegen.GenerateMSIL(programNode);
                
                var process = new Process();
                string ilasm = FindIlasm();
                bool isDll = ilasm.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                if (isDll)
                {
                    process.StartInfo.FileName = "dotnet";
                    process.StartInfo.Arguments =
                        $"\"{ilasm}\" \"{outputPath}.il\" /exe /out:\"{outputPath}.exe\"";
                }
                else
                {
                    process.StartInfo.FileName = ilasm;
                    process.StartInfo.Arguments =
                        $"\"{outputPath}.il\" /exe /out:\"{outputPath}.exe\"";
                }

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    System.Console.WriteLine("Done");
                }
                else
                {
                    System.Console.WriteLine("Assembly error:");
                    System.Console.WriteLine(errors);
                }
            }
            catch (CompilerException e)
            {
            
                System.Console.WriteLine($"Error while compiling {filePath}");
                if (e is LexerException)
                {
                    System.Console.WriteLine($"Lexer error: {e.Message}");
                }
                else if (e is ParserException)
                {
                    System.Console.WriteLine($"Parser error: {e.Message}");
                }
                else if (e is AnalyzerException)
                {
                    System.Console.WriteLine($"Analyzer error: {e.Message}");
                }
                return;
                
            }
        }

        public static string FindIlasm()
        {
            var searchRoots = new List<string>();

            string? dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrWhiteSpace(dotnetRoot))
                searchRoots.Add(dotnetRoot);

            if (OperatingSystem.IsWindows())
            {
                searchRoots.Add(@"C:\Program Files\dotnet");
                searchRoots.Add(@"C:\Program Files (x86)\dotnet");
            }
            else
            {
                searchRoots.Add("/usr/share/dotnet");
                searchRoots.Add("/usr/local/share/dotnet");
            }

            if (OperatingSystem.IsWindows())
            {
                searchRoots.Add(@"C:\Windows\Microsoft.NET\Framework");
                searchRoots.Add(@"C:\Windows\Microsoft.NET\Framework64");
            }

            string ilasmName = OperatingSystem.IsWindows() ? "ilasm.exe" : "ilasm";

            foreach (var root in searchRoots)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    continue;
                string sdkPath = Path.Combine(root, "sdk");
                if (Directory.Exists(sdkPath))
                {
                    var sdks = Directory.GetDirectories(sdkPath)
                        .OrderByDescending(Path.GetFileName);

                    foreach (var sdk in sdks)
                    {
                        string exe = Path.Combine(sdk, ilasmName);
                        if (File.Exists(exe))
                            return exe;

                        string dll = Path.Combine(sdk, "ilasm.dll");
                        if (File.Exists(dll))
                            return dll;
                    }
                }
                if (OperatingSystem.IsWindows() &&
                    (root.Contains("Framework") || root.Contains("Framework64")))
                {
                    var frameworkVersions = Directory.GetDirectories(root)
                        .OrderByDescending(Path.GetFileName);

                    foreach (var versionDir in frameworkVersions)
                    {
                        string exe = Path.Combine(versionDir, ilasmName);
                        if (File.Exists(exe))
                            return exe;
                    }
                }
            }

            throw new FileNotFoundException(
                "Could not find ilasm.exe or ilasm.dll in any known .NET paths."
            );
        }


        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  compile {file.impp} [-v] Compile the given source file");
            Console.WriteLine("  -v  Print verbose compilation info");
        }
    }
}