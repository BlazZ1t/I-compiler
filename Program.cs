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
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                HandleCompile("D:/VsCodeProjects/I-compiler/i_tests/test.impp");
                return;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "compile":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine("Error: Missing file path");
                        return;
                    }
                    HandleCompile(args[1]);
                    break;
                case "test":
                    RunTests();
                    break;
                default:
                    System.Console.WriteLine($"Unkown command: {command}");
                    PrintUsage();
                    break;
            }
        }

        private static void RunTests()
        {
            string testDir = Path.Combine(Directory.GetCurrentDirectory(), "i_tests");
            if (!Directory.Exists(testDir))
            {
                Console.WriteLine($"Test directory not found: {testDir}");
                return;
            }

            string[] files = Directory.GetFiles(testDir, "*.*", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                Console.WriteLine("No test files found.");
                return;
            }

            int passed = 0;

            foreach (var file in files)
            {
                Console.WriteLine($"=== Running test: {Path.GetFileName(file)} ===");
                HandleCompile(file, true);
                System.Console.WriteLine();
                System.Console.WriteLine("Passed!");
                passed++;
                System.Console.WriteLine();
            }
            if (passed == files.Length)
            {
                System.Console.WriteLine("All tests passed!");
            }
            else
            {
                Console.WriteLine($"{passed}/{files.Length} passed");   
            }
        }

        private static void HandleCompile(string filePath, bool testing = false)
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

                Parser parser = new Parser(tokens);
                ProgramNode programNode = parser.getAST();

                SemanticalAnalyzer semanticalAnalyzer = new SemanticalAnalyzer(programNode);
                semanticalAnalyzer.Analyze();

                string outputPath = Path.GetFileNameWithoutExtension(filePath);
                var codegen = new CodeGenerator(new StreamWriter($"{outputPath}.il"));
                codegen.GenerateMSIL(programNode);
                
                var process = new Process();
                process.StartInfo.FileName = "ilasm";
                process.StartInfo.Arguments = $"\"{outputPath}.il\" /exe /out:\"{outputPath}.exe\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

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

                // if (!testing)
                // { 
                //     var settings = new JsonSerializerSettings
                //     {
                //         Formatting = Formatting.Indented
                //     };
                //     settings.Converters.Add(new StringEnumConverter());
                //     string json = JsonConvert.SerializeObject(programNode, settings);
                //     System.Console.WriteLine(json);
                // }
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

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  compile [file.impp]  Compile the given source file");
            Console.WriteLine("  test                 Test on all files in i_test directory");
        }
    }
}