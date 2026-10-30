using System.Diagnostics;
using ShortcutMaster.Models;

namespace ShortcutMaster.Services;

/// <summary>
/// Runs the actions of a shortcut one after another as operating system processes.
/// </summary>
public static class ShortcutRunner
{
    /// <summary>
    /// Runs all actions in order and stops at the first command that fails.
    /// Extra arguments are added to the last action.
    /// </summary>
    public static int Run(Shortcut shortcut, IReadOnlyList<string> extraArguments, bool calledFromAlias)
    {
        Console.CancelKeyPress += LetChildHandleCtrlC;

        try
        {
            return RunActions(shortcut, extraArguments, calledFromAlias);
        }
        finally
        {
            Console.CancelKeyPress -= LetChildHandleCtrlC;
        }
    }

    /// <summary>
    /// Goes over the actions, keeping track of the folder set by a cd action.
    /// </summary>
    private static int RunActions(Shortcut shortcut, IReadOnlyList<string> extraArguments, bool calledFromAlias)
    {
        string? workingFolder = null;
        int lastIndex = shortcut.Actions.Count - 1;

        for (int i = 0; i <= lastIndex; i++)
        {
            ShortcutAction action = shortcut.Actions[i];

            if (action.Is(ShortcutTypes.Cd))
            {
                workingFolder = action.Target;

                if (!calledFromAlias)
                    Console.WriteLine($"Note: to move your terminal into {action.Target}, type '{shortcut.Name}' directly.");

                continue;
            }

            IReadOnlyList<string> arguments = i == lastIndex ? extraArguments : Array.Empty<string>();
            int exitCode = RunAction(action, arguments, workingFolder);

            if (exitCode != 0)
                return exitCode;
        }

        return 0;
    }

    /// <summary>
    /// Starts one action. Commands are awaited; apps, folders and URLs are opened and left running.
    /// </summary>
    private static int RunAction(ShortcutAction action, IReadOnlyList<string> extraArguments, string? workingFolder)
    {
        string type = action.Type.ToLowerInvariant();

        ProcessStartInfo startInfo = type switch
        {
            ShortcutTypes.Command => BuildShellCommand(action, extraArguments),
            ShortcutTypes.App or ShortcutTypes.Folder or ShortcutTypes.Url => BuildShellExecute(action, extraArguments),
            _ => throw new InvalidOperationException($"Unknown action type '{action.Type}'.")
        };

        if (workingFolder != null)
            startInfo.WorkingDirectory = workingFolder;

        using Process? process = Process.Start(startInfo);

        if (process == null || type != ShortcutTypes.Command)
            return 0;

        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>
    /// Prepares a terminal command to run through the system shell (cmd on Windows, sh elsewhere).
    /// </summary>
    private static ProcessStartInfo BuildShellCommand(ShortcutAction action, IReadOnlyList<string> extraArguments)
    {
        IEnumerable<string> parts = new[] { action.Target }.Concat(action.Arguments).Concat(extraArguments);
        string commandLine = JoinCommandLine(parts);
        var startInfo = new ProcessStartInfo { UseShellExecute = false };

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/d /s /c \"{commandLine}\"";
        }
        else
        {
            startInfo.FileName = "/bin/sh";
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(commandLine);
        }

        return startInfo;
    }

    /// <summary>
    /// Prepares an app, folder or URL to be opened by the operating system.
    /// </summary>
    private static ProcessStartInfo BuildShellExecute(ShortcutAction action, IReadOnlyList<string> extraArguments)
    {
        return new ProcessStartInfo
        {
            FileName = action.Target,
            Arguments = JoinCommandLine(action.Arguments.Concat(extraArguments)),
            UseShellExecute = true
        };
    }

    /// <summary>
    /// Joins parts into one command line, quoting any part that contains a space.
    /// </summary>
    private static string JoinCommandLine(IEnumerable<string> parts)
    {
        return string.Join(" ", parts.Select(part => part.Contains(' ') ? $"\"{part}\"" : part));
    }

    /// <summary>
    /// Keeps this program alive on Ctrl+C so the running command can stop by itself.
    /// </summary>
    private static void LetChildHandleCtrlC(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
    }
}
