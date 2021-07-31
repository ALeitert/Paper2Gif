using System;
using System.Diagnostics;
using System.IO;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("*** Paper 2 Code ***");
    }

    // Helper function to run terminal commands.
    // Returns the output given by the program.
    string RunTerminalCommand(string prog, string args, string dir)
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

        Process termProg = Process.Start(psi);

        StreamReader outReader = termProg.StandardOutput;
        string output = outReader.ReadToEnd();
        outReader.Dispose();

        return output;
    }
}
