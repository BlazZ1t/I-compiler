using System;
using System.IO;
using ImperativeLang.SyntaxAnalyzer;

namespace ImperativeLang
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
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

            Lexer lexer = new Lexer(File.ReadAllText(filePath));
            try
            {
                List<Token> tokens = lexer.Tokenize().ToList();
                tokens = lexer.CleanUp(tokens);
                foreach (Token item in tokens)
                {
                    System.Console.WriteLine(item);
                }
                System.Console.WriteLine($"Recognized {tokens.Count} tokens");
                // Parser parser = new Parser(tokens);
                // ProgramNode programNode = parser.getAST();
            }
            catch (CompilerException e)
            {
                if (e is LexerException)
                {
                    System.Console.WriteLine($"Lexer error: {e.Message}");
                    Environment.Exit(1);
                }
                else if (e is ParserException)
                {
                    System.Console.WriteLine($"Parser error: {e.Message}");
                    Environment.Exit(1);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  compile [file.impp]  Compile the given source file");
        }
    }
}