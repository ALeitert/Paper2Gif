using System;
using System.Diagnostics;
using System.IO;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("*** Paper 2 Code ***");


        // --- Program arguments ---

        //   0: The path to the repository. Must contain a .git directory.

        //   1: The name of the tex-file. Only the name of the file, not the
        //      whole path.

        //   2: The branch which to use. If not given, the program tries the
        //      branches 'master' and 'main' (in that order).


        // --- Check repository directory. ---

        // Path to repository given?
        if (args.Length < 1)
        {
            Console.WriteLine("No path to repository given.");
            return;
        }

        string repoGitPath = Path.Combine(args[0], ".git");
        if (!Directory.Exists(repoGitPath))
        {
            Console.WriteLine("Given path is not a git repository.");
            return;
        }


        // --- Create temporary directory and copy the repository into it. ---

        string tmpTexPath = RunTerminalCommand("mktemp", "-d", "./");
        RunTerminalCommand
        (
            "cp",
            "-R " + repoGitPath + " " + tmpTexPath,
            "./"
        );


        // --- Determine branch to checkout. ---

        // Determine all branches.
        string allBranches = RunTerminalCommand("git", "branch -l", tmpTexPath);
        string[] brList = allBranches.Split
        (
            new string[] { Environment.NewLine },
            StringSplitOptions.RemoveEmptyEntries
        );

        // Clean leading characters.
        for (int i = 0; i < brList.Length; i++)
        {
            brList[i] = brList[i].Substring(2);
        }

        // Find a desired branch.
        string coBranch = null;

        if (args.Length >= 3 && Array.IndexOf(brList, args[2]) >= 0)
        {
            coBranch = args[2];
        }
        else if (Array.IndexOf(brList, "master") >= 0)
        {
            coBranch = "master";
        }
        else if (Array.IndexOf(brList, "main") >= 0)
        {
            coBranch = "main";
        }

        // Branch found?
        if (string.IsNullOrEmpty(coBranch))
        {
            Console.WriteLine("Unable to find a branch to checkout.");
            return;
        }

        // Checkout branch.
        RunTerminalCommand("git", "checkout " + coBranch, tmpTexPath);




        // --- Cleanup: Remove temporary folders. ---

        RunTerminalCommand("rm", tmpTexPath + " -r", "./");
    }

    // Helper function to run terminal commands.
    // Returns the output given by the program.
    static string RunTerminalCommand(string prog, string args, string dir)
    {
        ProcessStartInfo psi = new ProcessStartInfo()
        {
            FileName = prog,
            WorkingDirectory = dir,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false
        };


        // --- Run program. ---

        Process termProg = Process.Start(psi);

        StreamReader outReader = termProg.StandardOutput;
        string output = outReader.ReadToEnd();
        outReader.Dispose();


        // --- Remove ending line break. ---

        string NL = Environment.NewLine;
        if (output.EndsWith(NL))
        {
            return output.Substring(0, output.Length - NL.Length);
        }
        else
        {
            return output;
        }
    }
}
