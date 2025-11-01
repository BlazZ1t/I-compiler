using Newtonsoft.Json;
using ImperativeLang.SyntaxAnalyzer;
using ImperativeLang.SemanticalAnalyzerNS;
using Newtonsoft.Json.Converters;

namespace ImperativeLang
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                HandleCompile("D:/VsCodeProjects/I-compiler/i_tests/chatgpt_errors.impp");
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

                if (!testing)
                { 
                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    settings.Converters.Add(new StringEnumConverter());
                    string json = JsonConvert.SerializeObject(programNode, settings);
                    System.Console.WriteLine(json);
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

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  compile [file.impp]  Compile the given source file");
            Console.WriteLine("  test                 Test on all files in i_test directory");
        }
    }
}