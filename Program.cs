using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    /// <summary>
    /// Registry of built-in commands mapping command names to their handler methods
    /// </summary>
    private static readonly Dictionary<string, Action<string>> BuiltinCommands = new()
    {
        { "echo", Echo },          // Simple text output
        { "exit", Exit },          // Shell termination
        { "type", TypeCommand },   // Command type inspection
        { "pwd", PrintWorkingDirectory },  // Current directory
        { "cd", ChangeDirectory }  // Directory navigation
    };

    /// <summary>
    /// List of all recognized built-in command names for quick lookup
    /// </summary>
    private static readonly string[] Builtins = { "echo", "exit", "type", "ls", "pwd", "cd" };

    static void Main()
    {
        // Main REPL (Read-Eval-Print Loop) for shell
        while (true)
        {
            Console.Write("$ ");
            string input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;
            
            ExecuteCommand(input);
        }
    }

    /// <summary>
    /// Command execution pipeline handler with redirection support
    /// </summary>
    /// <param name="input">Raw command input from user</param>
    private static void ExecuteCommand(string input)
    {
        var args = ParseCommand(input);
        string stdoutFile = null;
        bool stdoutAppend = false;
        string stderrFile = null;
        bool stderrAppend = false;
        var commandArgsList = new List<string>();

        // Process redirection operators and build command arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case ">":   // Stdout overwrite
                case "1>":  // Explicit stdout overwrite
                    HandleRedirection(ref i, args, ref stdoutFile, ref stdoutAppend, false);
                    break;
                case ">>":  // Stdout append
                case "1>>": // Explicit stdout append
                    HandleRedirection(ref i, args, ref stdoutFile, ref stdoutAppend, true);
                    break;
                case "2>":  // Stderr overwrite
                    HandleRedirection(ref i, args, ref stderrFile, ref stderrAppend, false);
                    break;
                case "2>>": // Stderr append
                    HandleRedirection(ref i, args, ref stderrFile, ref stderrAppend, true);
                    break;
                default:
                    commandArgsList.Add(args[i]);
                    break;
            }
        }

        if (commandArgsList.Count == 0) return;

        // Split into command and arguments
        string command = commandArgsList[0];
        string[] commandArgs = commandArgsList.Skip(1).ToArray();

        // Execute appropriate command type
        if (BuiltinCommands.TryGetValue(command, out Action<string> action))
        {
            ExecuteBuiltinCommand(action, commandArgs, stdoutFile, stdoutAppend, stderrFile, stderrAppend);
        }
        else
        {
            RunExternalCommand(command, commandArgs, stdoutFile, stdoutAppend, stderrFile, stderrAppend);
        }
    }

    /// <summary>
    /// Handles redirection operators by capturing target file and mode
    /// </summary>
    /// <param name="index">Current token index (modified by ref)</param>
    /// <param name="args">Command arguments array</param>
    /// <param name="file">Output file reference to populate</param>
    /// <param name="append">Append mode flag reference</param>
    /// <param name="isAppend">True if operator is append variant</param>
    private static void HandleRedirection(ref int index, string[] args, ref string file, ref bool append, bool isAppend)
    {
        if (index + 1 >= args.Length)
        {
            Console.Error.WriteLine($"Syntax error: missing filename for {(isAppend ? "append" : "redirection")}");
            return;
        }
        file = args[index + 1];
        append = isAppend;
        index++; // Skip filename in main loop
    }

    /// <summary>
    /// Executes built-in commands with output stream redirection
    /// </summary>
    /// <param name="action">Built-in command handler</param>
    /// <param name="commandArgs">Command arguments array</param>
    /// <param name="stdoutFile">Stdout target file (null for console)</param>
    /// <param name="stdoutAppend">Stdout append mode flag</param>
    /// <param name="stderrFile">Stderr target file (null for console)</param>
    /// <param name="stderrAppend">Stderr append mode flag</param>
    private static void ExecuteBuiltinCommand(Action<string> action, string[] commandArgs, 
        string stdoutFile, bool stdoutAppend, string stderrFile, bool stderrAppend)
    {
        try
        {
            // Backup original streams
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;

            // Create redirected streams if needed
            using StreamWriter stdoutWriter = GetOutputWriter(stdoutFile, stdoutAppend);
            using StreamWriter stderrWriter = GetOutputWriter(stderrFile, stderrAppend);

            // Redirect standard streams
            if (stdoutWriter != null) Console.SetOut(stdoutWriter);
            if (stderrWriter != null) Console.SetError(stderrWriter);

            // Join arguments and execute command
            action(string.Join(" ", commandArgs));
        }
        finally  // Ensure stream restoration
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }
    }

    /// <summary>
    /// Advanced command line parser supporting:
    /// - Nested quotes (single and double)
    /// - Escape sequences
    /// - Redirection operators as separate tokens
    /// </summary>
    /// <param name="input">Raw command input string</param>
    /// <returns>Array of parsed arguments</returns>
    private static string[] ParseCommand(string input)
    {
        var args = new List<string>();
        var currentArg = new StringBuilder();
        bool inSingle = false, inDouble = false, escapeNext = false;
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (escapeNext)  // Handle escape sequence
            {
                currentArg.Append(c);
                escapeNext = false;
                i++;
                continue;
            }

            if (inSingle)  // Single-quoted context
            {
                if (c == '\'') inSingle = false;
                else currentArg.Append(c);
                i++;
            }
            else if (inDouble)  // Double-quoted context
            {
                switch (c)
                {
                    case '\\':  // Escape sequences
                        if (i + 1 < input.Length)
                        {
                            char next = input[i + 1];
                            currentArg.Append(next == '\n' ? "" : next);
                            i += 2;
                            continue;
                        }
                        break;
                    case '"':  // End double quote
                        inDouble = false;
                        i++;
                        continue;
                }
                currentArg.Append(c);
                i++;
            }
            else  // Unquoted context
            {
                switch (c)
                {
                    case '\\':  // Escape next character
                        escapeNext = true;
                        i++;
                        break;
                    case '\'':  // Start single quote
                        inSingle = true;
                        i++;
                        break;
                    case '"':  // Start double quote
                        inDouble = true;
                        i++;
                        break;
                    case char ws when char.IsWhiteSpace(ws):  // Argument separator
                        if (currentArg.Length > 0)
                        {
                            args.Add(currentArg.ToString());
                            currentArg.Clear();
                        }
                        while (i < input.Length && char.IsWhiteSpace(input[i])) i++;
                        break;
                    default:  // Regular character
                        currentArg.Append(c);
                        i++;
                        break;
                }
            }
        }

        // Add final accumulated argument
        if (currentArg.Length > 0) args.Add(currentArg.ToString());

        return args.ToArray();
    }

    //region Built-in Command Implementations

    /// <summary>
    /// Echo command implementation
    /// </summary>
    /// <param name="args">Arguments to output</param>
    private static void Echo(string args) => Console.WriteLine(args);

    /// <summary>
    /// Exit command implementation
    /// </summary>
    /// <param name="args">Exit code (only 0 supported)</param>
    private static void Exit(string args)
    {
        if (args == "0") Environment.Exit(0);
        Console.Error.WriteLine("Usage: exit 0");
    }

    /// <summary>
    /// Type command implementation for command introspection
    /// </summary>
    /// <param name="args">Command name to inspect</param>
    private static void TypeCommand(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            Console.Error.WriteLine("Usage: type <command>");
            return;
        }

        if (Builtins.Contains(args))
        {
            Console.WriteLine($"{args} is a shell builtin");
            return;
        }

        string path = FindExecutableInPath(args);
        Console.WriteLine(path != null ? $"{args} is {path}" : $"{args}: not found");
    }

    /// <summary>
    /// Print current working directory
    /// </summary>
    private static void PrintWorkingDirectory(string _) => Console.WriteLine(Directory.GetCurrentDirectory());

    /// <summary>
    /// Change directory implementation with path resolution
    /// </summary>
    /// <param name="args">Target directory path</param>
    private static void ChangeDirectory(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            Console.Error.WriteLine("Usage: cd <directory>");
            return;
        }

        string targetDir = args switch
        {
            "~" => Environment.GetEnvironmentVariable("HOME") ?? "",
            _ when !Path.IsPathRooted(args) => Path.GetFullPath(args, Directory.GetCurrentDirectory()),
            _ => args
        };

        try
        {
            if (!Directory.Exists(targetDir))
                throw new DirectoryNotFoundException();

            Directory.SetCurrentDirectory(targetDir);
        }
        catch (Exception ex)
        {
            string error = ex switch
            {
                DirectoryNotFoundException => "No such file or directory",
                UnauthorizedAccessException => "Permission denied",
                _ => ex.Message
            };
            Console.Error.WriteLine($"cd: {targetDir}: {error}");
        }
    }

    //region External Command Execution

    /// <summary>
    /// Execute external program with PATH lookup and I/O redirection
    /// </summary>
    /// <param name="command">Command/executable name</param>
    /// <param name="arguments">Command arguments</param>
    /// <param name="stdoutFile">Stdout target file</param>
    /// <param name="stdoutAppend">Stdout append mode</param>
    /// <param name="stderrFile">Stderr target file</param>
    /// <param name="stderrAppend">Stderr append mode</param>
    static void RunExternalCommand(string command, string[] arguments, 
        string stdoutFile, bool stdoutAppend, 
        string stderrFile, bool stderrAppend)
    {
        string programPath = FindExecutableInPath(command);
        if (programPath == null)
        {
            Console.Error.WriteLine($"{command}: not found");
            return;
        }

        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    Arguments = EscapeArguments(arguments),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            
            // Capture output streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Handle output redirection
            RedirectOutput(stdoutFile, output, stdoutAppend);
            RedirectOutput(stderrFile, error, stderrAppend);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }

    //region Helper Methods

    /// <summary>
    /// Create output writer for redirection targets
    /// </summary>
    private static StreamWriter GetOutputWriter(string path, bool append) => 
        path != null ? new StreamWriter(path, append) { AutoFlush = true } : null;

    /// <summary>
    /// Handle output redirection to files or console
    /// </summary>
    private static void RedirectOutput(string file, string content, bool append)
    {
        if (file == null)
        {
            Console.Write(content);
            return;
        }

        try
        {
            File.WriteAllText(file, content + (append ? "\n" : ""));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing to {file}: {ex.Message}");
        }
    }

    /// <summary>
    /// Properly escape command line arguments
    /// </summary>
    private static string EscapeArguments(IEnumerable<string> arguments) => 
        string.Join(" ", arguments.Select(arg => $"\"{arg.Replace("\"", "\\\"")}\""));

    /// <summary>
    /// Search PATH environment variable for executable
    /// </summary>
    private static string FindExecutableInPath(string command)
    {
        string pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;

        return pathEnv.Split(Path.PathSeparator)
            .Select(dir => Path.Combine(dir, command))
            .FirstOrDefault(IsExecutable);
    }

    /// <summary>
    /// Basic executable validation check
    /// </summary>
    private static bool IsExecutable(string path) => 
        File.Exists(path) && (new FileInfo(path).Attributes & FileAttributes.Directory) == 0;
}