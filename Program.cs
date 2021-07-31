using System;
using System.Collections.Generic;
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


        // --- Determine commit-IDs. ---

        string log = RunTerminalCommand("git", "log --oneline", tmpTexPath);
        string[] logLines = log.Split
        (
            new string[] { Environment.NewLine },
            StringSplitOptions.RemoveEmptyEntries
        );

        List<string> commitIds = new List<string>();

        for (int i = 0; i < logLines.Length; i++)
        {
            commitIds.Add(logLines[i].Substring(0, 7));
        }



        // --- Compile each version. ---

        // Create temporary directory for outputs.
        string tmpOutPath = RunTerminalCommand("mktemp", "-d", "./");

        // Created directories.
        string tmpPdfPath = Path.Combine(tmpOutPath, "latexmk");
        string tmpPngPath = Path.Combine(tmpOutPath, "png");

        RunTerminalCommand("mkdir", tmpPdfPath, tmpOutPath);
        RunTerminalCommand("mkdir", tmpPngPath, tmpOutPath);

        // Unfortunately needed (both of them).
        // ToDo: find better way to do that.
        RunTerminalCommand("mkdir", "tikzCache", tmpPdfPath);
        RunTerminalCommand("mkdir", "tikzCache", tmpTexPath);


        // TeX-file specified?
        if (args.Length < 2)
        {
            Console.WriteLine("No TeX-file specified.");
            return;
        }

        for (int i = commitIds.Count - 1, ver = 0; i >= 0; i--, ver++)
        {
            RunTerminalCommand("git", "checkout -q " + commitIds[i], tmpTexPath);

            if (!File.Exists(Path.Combine(tmpTexPath, args[1])))
            {
                Console.WriteLine("TeX-file not found.");
                return;
            }

            // Parameters for compiler.
            string lmkArgs =
                "-outdir='" + Path.Combine(tmpOutPath, "latexmk") + "' " +
                "-quiet -pdf -pdflatex='" +
                    "pdflatex -shell-escape -interaction=nonstopmode %O %S" +
                    "' " +
                args[1];

            // Compile.
            RunTerminalCommand("latexmk", lmkArgs, tmpTexPath);


            // Parameters to create images from pdf.
            string conArgs =
                "-density 25 " +
                args[1].Substring(0, args[1].Length - 4) + ".pdf " +
                "-alpha off " +
                "-quality 100 " +
                Path.Combine(tmpPngPath, ver.ToString("000") + ".png");

            // Create images.
            RunTerminalCommand("convert", conArgs, tmpPdfPath);
        }


        // --- Crop images. ---

        string tmpSmlPath = Path.Combine(tmpOutPath, "smlPng");
        RunTerminalCommand("mkdir", tmpSmlPath, tmpOutPath);

        DirectoryInfo pngDir = new DirectoryInfo(tmpPngPath);
        foreach (FileInfo fi in pngDir.GetFiles())
        {
            // Works for LNCS style.
            const int w = 140;
            const int h = 210;
            const int x = 35;
            const int y = 35;

            RunTerminalCommand
            (
                "convert",
                string.Format
                (
                    "{0} -crop {1}x{2}+{3}+{4} {5}",
                    new object[]
                    {
                        fi.Name,
                        w,
                        h,
                        x,
                        y,
                        Path.Combine(tmpSmlPath, fi.Name)
                    }
                ),
                tmpPngPath
            );
        }


        // --- Resize image of first page to use as canvas. ---

        string tmpCnvPath = Path.Combine(tmpOutPath, "cnvPng");
        RunTerminalCommand("mkdir", tmpCnvPath, tmpOutPath);

        for (int i = 0; i < commitIds.Count; i++)
        {
            string inFile = Path.Combine
            (
                tmpSmlPath,
                i.ToString("000") + "-0.png"
            );

            if (!File.Exists(inFile))
            {
                inFile = Path.Combine
                (
                    tmpSmlPath,
                    i.ToString("000") + ".png"
                );
            }

            string outFile = Path.Combine
            (
                tmpCnvPath,
                i.ToString("000") + ".png"
            );

            // ToDo: Make dynamic based on page numbers.
            const int w = 7 * 140;
            const int h = 4 * 210;
            const int x = 0;
            const int y = 0;

            RunTerminalCommand
            (
                "convert",
                string.Format
                (
                    "{0} -extent {1}x{2}+{3}+{4} {5}",
                    new object[]
                    {
                        inFile,
                        w,
                        h,
                        x,
                        y,
                        outFile
                    }
                ),
                tmpSmlPath
            );
        }


        // --- Add small images to "canvas". ---

        string lastHash = null;

        for (int i = 0; i < commitIds.Count; i++)
        {
            string canFile = Path.Combine
            (
                tmpCnvPath,
                i.ToString("000") + ".png"
            );

            for (int page = 1; ; page++)
            {
                string pgFile = Path.Combine
                (
                    tmpSmlPath,
                    string.Format("{0:000}-{1}.png", i, page)
                );

                // Out of pages.
                if (!File.Exists(pgFile)) break;

                // ToDo: make dynamic.
                int x = page / 4;
                int y = page % 4;

                RunTerminalCommand
                (
                    "composite",
                    string.Format
                    (
                        "-geometry +{0}+{1} {3} {2} {2}",
                        new object[]
                        {
                            // ToDo: make dynamic.
                            x * 140,
                            y * 210,
                            canFile,
                            pgFile
                        }
                    ),
                    tmpCnvPath
                );
            }

            // Determine SHA-1 hash of created file.
            string hash = RunTerminalCommand("sha1sum", canFile, "./");
            hash = hash.Substring(0, 40);

            if (hash == lastHash)
            {
                RunTerminalCommand("rm", canFile, "./");
            }

            lastHash = hash;
        }


        // --- Cleanup: Remove temporary folders. ---

        RunTerminalCommand("rm", tmpTexPath + " -r", "./");
        RunTerminalCommand("rm", tmpOutPath + " -r", "./");
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
