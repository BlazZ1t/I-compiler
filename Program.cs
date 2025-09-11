using System;
using System.IO;

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
            System.Console.WriteLine("Starting lexer...");
            List<Token> tokens = lexer.Tokenize().ToList();
            foreach (Token item in tokens)
            {
                System.Console.WriteLine(item);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  compile [file.impp]  Compile the given source file");
        }
    }
}