using System.CommandLine;
using ShortcutMaster.Models;
using ShortcutMaster.Services;
using ShortcutMaster.Shell;

namespace ShortcutMaster;

/// <summary>
/// Entry point of the CLI. Defines the commands with System.CommandLine and routes them to the services.
/// </summary>
public static class Program
{
    private const string AliasRunCommand = "__run";

    /// <summary>
    /// Starts the program and returns an exit code (0 = success, anything else = error).
    /// </summary>
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length >= 2 && args[0] == AliasRunCommand)
                return RunShortcut(args[1], args.Skip(2).ToArray(), calledFromAlias: true);

            ParseResult parseResult = BuildRootCommand().Parse(args);
            return parseResult.Invoke(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Builds the root "shortcut" command with all its sub-commands.
    /// </summary>
    private static RootCommand BuildRootCommand()
    {
        return new RootCommand("ShortcutMaster - personal shortcuts for commands, apps, folders and websites.")
        {
            CreateAddCommand(),
            CreateUpdateCommand(),
            CreateRemoveCommand(),
            CreateListCommand(),
            CreateRunCommand(),
            CreateInstallCommand(),
            CreateUninstallCommand()
        };
    }

    /// <summary>
    /// Defines: shortcut add &lt;name&gt; "&lt;action&gt;" [--then "&lt;action&gt;"]... [--type &lt;type&gt;] [--force]
    /// </summary>
    private static Command CreateAddCommand()
    {
        Argument<string> nameArgument = CreateNameArgument();
        Argument<string> actionArgument = CreateActionArgument();
        Option<string[]> thenOption = CreateThenOption();
        Option<string> typeOption = CreateTypeOption();
        var forceOption = new Option<bool>("--force") { Description = "Allow a name that is already a command on this computer" };

        var command = new Command("add", "Create a new shortcut")
        {
            nameArgument, actionArgument, thenOption, typeOption, forceOption
        };

        command.SetAction(result => HandleAdd(
            result.GetValue(nameArgument)!,
            result.GetValue(actionArgument)!,
            result.GetValue(thenOption) ?? Array.Empty<string>(),
            result.GetValue(typeOption),
            result.GetValue(forceOption)));

        return command;
    }

    /// <summary>
    /// Defines: shortcut update &lt;name&gt; "&lt;action&gt;" [--then "&lt;action&gt;"]... [--type &lt;type&gt;]
    /// </summary>
    private static Command CreateUpdateCommand()
    {
        Argument<string> nameArgument = CreateNameArgument();
        Argument<string> actionArgument = CreateActionArgument();
        Option<string[]> thenOption = CreateThenOption();
        Option<string> typeOption = CreateTypeOption();

        var command = new Command("update", "Replace what an existing shortcut does")
        {
            nameArgument, actionArgument, thenOption, typeOption
        };

        command.SetAction(result => HandleUpdate(
            result.GetValue(nameArgument)!,
            result.GetValue(actionArgument)!,
            result.GetValue(thenOption) ?? Array.Empty<string>(),
            result.GetValue(typeOption)));

        return command;
    }

    /// <summary>
    /// Defines: shortcut remove &lt;name&gt;
    /// </summary>
    private static Command CreateRemoveCommand()
    {
        Argument<string> nameArgument = CreateNameArgument();
        var command = new Command("remove", "Delete a shortcut") { nameArgument };

        command.SetAction(result => HandleRemove(result.GetValue(nameArgument)!));
        return command;
    }

    /// <summary>
    /// Defines: shortcut list
    /// </summary>
    private static Command CreateListCommand()
    {
        var command = new Command("list", "Show all shortcuts");

        command.SetAction(_ => HandleList());
        return command;
    }

    /// <summary>
    /// Defines: shortcut run &lt;name&gt; [arguments...]
    /// </summary>
    private static Command CreateRunCommand()
    {
        Argument<string> nameArgument = CreateNameArgument();
        var argumentsArgument = new Argument<string[]>("arguments")
        {
            Description = "Extra arguments for the last action (put -- before arguments that start with -)",
            Arity = ArgumentArity.ZeroOrMore
        };

        var command = new Command("run", "Run a saved shortcut") { nameArgument, argumentsArgument };

        command.SetAction(result => RunShortcut(
            result.GetValue(nameArgument)!,
            result.GetValue(argumentsArgument) ?? Array.Empty<string>(),
            calledFromAlias: false));

        return command;
    }

    /// <summary>
    /// Defines: shortcut install
    /// </summary>
    private static Command CreateInstallCommand()
    {
        var command = new Command("install", "Let PowerShell, CMD and Bash/Zsh run shortcuts by their name alone");

        command.SetAction(_ => HandleInstall());
        return command;
    }

    /// <summary>
    /// Defines: shortcut uninstall
    /// </summary>
    private static Command CreateUninstallCommand()
    {
        var command = new Command("uninstall", "Remove the terminal integration (your shortcuts are kept)");

        command.SetAction(_ => HandleUninstall());
        return command;
    }

    /// <summary>
    /// Creates the "name" argument shared by several commands.
    /// </summary>
    private static Argument<string> CreateNameArgument()
    {
        return new Argument<string>("name") { Description = "The short name you will type, e.g. gs" };
    }

    /// <summary>
    /// Creates the "action" argument: what the shortcut runs, written in quotes.
    /// </summary>
    private static Argument<string> CreateActionArgument()
    {
        return new Argument<string>("action")
        {
            Description = "What to run, in quotes: a command, an app path, a folder or a URL"
        };
    }

    /// <summary>
    /// Creates the repeatable --then option for extra actions that run afterwards.
    /// </summary>
    private static Option<string[]> CreateThenOption()
    {
        return new Option<string[]>("--then")
        {
            Description = "Another action to run afterwards, in quotes (can be repeated)"
        };
    }

    /// <summary>
    /// Creates the --type option that forces the type of the first action.
    /// </summary>
    private static Option<string> CreateTypeOption()
    {
        var option = new Option<string>("--type", "-t")
        {
            Description = "Type of the first action. Detected automatically if omitted; use cd to change folder"
        };

        option.AcceptOnlyFromAmong(ShortcutTypes.All);
        return option;
    }

    /// <summary>
    /// Handles "shortcut add".
    /// </summary>
    private static int HandleAdd(string name, string action, string[] thenActions, string? type, bool force)
    {
        var service = new ShortcutService();
        List<ShortcutAction> actions = ShortcutService.BuildActions(action, thenActions, type);

        service.Add(name, actions, force);
        AliasScriptWriter.WriteAll(service.GetAll());

        Console.WriteLine($"Shortcut '{name}' was added: {service.Get(name).Describe()}");
        PrintHowToUse(name);
        return 0;
    }

    /// <summary>
    /// Handles "shortcut update".
    /// </summary>
    private static int HandleUpdate(string name, string action, string[] thenActions, string? type)
    {
        var service = new ShortcutService();
        List<ShortcutAction> actions = ShortcutService.BuildActions(action, thenActions, type);

        service.Update(name, actions);
        AliasScriptWriter.WriteAll(service.GetAll());

        Console.WriteLine($"Shortcut '{name}' was updated: {service.Get(name).Describe()}");
        PrintHowToUse(name);
        return 0;
    }

    /// <summary>
    /// Handles "shortcut remove".
    /// </summary>
    private static int HandleRemove(string name)
    {
        var service = new ShortcutService();

        service.Remove(name);
        AliasScriptWriter.WriteAll(service.GetAll());

        Console.WriteLine($"Shortcut '{name}' was removed.");
        return 0;
    }

    /// <summary>
    /// Handles "shortcut list" by printing every saved shortcut.
    /// </summary>
    private static int HandleList()
    {
        IReadOnlyList<Shortcut> shortcuts = new ShortcutService().GetAll();

        if (shortcuts.Count == 0)
        {
            Console.WriteLine("No shortcuts yet. Add one with: shortcut add <name> \"<action>\"");
            return 0;
        }

        foreach (Shortcut shortcut in shortcuts)
        {
            string types = string.Join(",", shortcut.Actions.Select(action => action.Type));
            Console.WriteLine($"{shortcut.Name,-15} [{types}] {shortcut.Describe()}");
        }

        Console.WriteLine();
        Console.WriteLine($"Saved in: {AppPaths.ShortcutsFile}");
        return 0;
    }

    /// <summary>
    /// Runs a shortcut by name, asking for approval first if it was not created through this tool.
    /// </summary>
    private static int RunShortcut(string name, string[] extraArguments, bool calledFromAlias)
    {
        var service = new ShortcutService();
        Shortcut shortcut = service.Get(name);

        if (!shortcut.Trusted)
        {
            if (!ConfirmUntrusted(shortcut))
            {
                Console.WriteLine("Cancelled.");
                return 1;
            }

            service.MarkTrusted(shortcut);
        }

        return ShortcutRunner.Run(shortcut, extraArguments, calledFromAlias);
    }

    /// <summary>
    /// Handles "shortcut install": writes the alias files and connects them to every terminal.
    /// </summary>
    private static int HandleInstall()
    {
        AliasScriptWriter.WriteAll(new ShortcutService().GetAll());
        List<string> changed = ShellInstaller.Install();

        Console.WriteLine("Terminal integration installed in:");

        foreach (string place in changed)
            Console.WriteLine("  " + place);

        Console.WriteLine();
        Console.WriteLine("Open a new terminal window, then type any shortcut name directly.");

        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("If PowerShell says that running scripts is disabled, run this once in PowerShell:");
            Console.WriteLine("  Set-ExecutionPolicy -Scope CurrentUser RemoteSigned");
        }

        return 0;
    }

    /// <summary>
    /// Handles "shortcut uninstall": disconnects the alias files from every terminal.
    /// </summary>
    private static int HandleUninstall()
    {
        List<string> changed = ShellInstaller.Uninstall();

        if (changed.Count == 0)
        {
            Console.WriteLine("The terminal integration was not installed.");
            return 0;
        }

        Console.WriteLine("Terminal integration removed from:");

        foreach (string place in changed)
            Console.WriteLine("  " + place);

        Console.WriteLine($"Your shortcuts are still saved in: {AppPaths.ShortcutsFile}");
        return 0;
    }

    /// <summary>
    /// Tells the user how to use a shortcut they just added or changed.
    /// </summary>
    private static void PrintHowToUse(string name)
    {
        if (ShellInstaller.IsInstalled())
            Console.WriteLine($"Open a new terminal window and type: {name}");
        else
            Console.WriteLine($"Run it with: shortcut run {name}   (run 'shortcut install' once to type just '{name}')");
    }

    /// <summary>
    /// Shows what an unapproved shortcut will do and asks the user to confirm.
    /// </summary>
    private static bool ConfirmUntrusted(Shortcut shortcut)
    {
        Console.WriteLine($"Shortcut '{shortcut.Name}' was not created by this tool and has not been approved yet.");
        Console.WriteLine($"It will run: {shortcut.Describe()}");
        Console.Write("Run it and trust it from now on? (y/N): ");

        string? answer = Console.ReadLine();
        return answer?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
    }
}
