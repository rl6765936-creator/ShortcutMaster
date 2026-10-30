// =====================================================================
// Program.cs — עותק עם הסברים
// תפקיד הקובץ: נקודת הכניסה של התוכנית. מגדיר את כל הפקודות בעזרת
// System.CommandLine (שלב 2), ומפנה כל פקודה לשירות המתאים.
// System.CommandLine נותן לנו בחינם: עזרה (--help), בדיקת ארגומנטים חסרים,
// בדיקת ערכים חוקיים, והודעות שגיאה מסודרות.
// סוגריים מסולסלים { } פותחים וסוגרים בלוק, ולא הוסבר עליהם בנפרד.
// =====================================================================

// הספרייה לניהול פקודות: RootCommand, Command, Argument, Option, ParseResult.
using System.CommandLine;
// המודלים.
using ShortcutMaster.Models;
// השירותים: ShortcutService, ShortcutRunner, AppPaths.
using ShortcutMaster.Services;
// שכבת הטרמינלים: AliasScriptWriter, ShellInstaller.
using ShortcutMaster.Shell;

// מרחב השמות הראשי.
namespace ShortcutMaster;

/// <summary>
/// Entry point of the CLI. Defines the commands with System.CommandLine and routes them to the services.
/// </summary>
// מחלקה סטטית — רק פונקציות סטטיות.
public static class Program
{
    // פקודה פנימית שהכינויים בטרמינל קוראים לה (למשל: shortcut __run gs). לא מופיעה בעזרה.
    private const string AliasRunCommand = "__run";

    /// <summary>
    /// Starts the program and returns an exit code (0 = success, anything else = error).
    /// </summary>
    // Main — כאן התוכנית מתחילה. args = המילים שאחרי "shortcut". מחזירה קוד יציאה.
    public static int Main(string[] args)
    {
        // try — אם משהו נכשל, נקפוץ ל־catch במקום שהתוכנית תקרוס עם הודעה טכנית.
        try
        {
            // "מסלול מהיר" לכינויים: אם המילה הראשונה היא __run ויש אחריה שם...
            if (args.Length >= 2 && args[0] == AliasRunCommand)
                // ...מריצים ישירות, בלי System.CommandLine. כך כל מה שהמשתמש כתב אחרי הכינוי
                // (גם דברים כמו --short) עובר כמו שהוא לפקודה, ולא מתפרש כאפשרות של הכלי.
                return RunShortcut(args[1], args.Skip(2).ToArray(), calledFromAlias: true);

            // בונים את מבנה הפקודות ומפרקים (Parse) את מה שהמשתמש הקליד.
            ParseResult parseResult = BuildRootCommand().Parse(args);
            // מריצים את הפעולה המתאימה. EnableDefaultExceptionHandler = false → השגיאות שלנו
            // יגיעו ל־catch למטה ויוצגו כהודעה קצרה, במקום פירוט טכני ארוך.
            return parseResult.Invoke(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
        }
        // תופס כל שגיאה.
        catch (Exception ex)
        {
            // מדפיס הודעה קצרה לערוץ השגיאות.
            Console.Error.WriteLine($"Error: {ex.Message}");
            // קוד שגיאה.
            return 1;
        }
    }

    /// <summary>
    /// Builds the root "shortcut" command with all its sub-commands.
    /// </summary>
    // בונה את הפקודה הראשית shortcut עם כל תתי־הפקודות.
    private static RootCommand BuildRootCommand()
    {
        // RootCommand = הפקודה הראשית. הטקסט הוא התיאור שמופיע ב־shortcut --help.
        return new RootCommand("ShortcutMaster - personal shortcuts for commands, apps, folders and websites.")
        {
            // shortcut add
            CreateAddCommand(),
            // shortcut update
            CreateUpdateCommand(),
            // shortcut remove
            CreateRemoveCommand(),
            // shortcut list
            CreateListCommand(),
            // shortcut run
            CreateRunCommand(),
            // shortcut install
            CreateInstallCommand(),
            // shortcut uninstall
            CreateUninstallCommand()
        };
    }

    /// <summary>
    /// Defines: shortcut add &lt;name&gt; "&lt;action&gt;" [--then "&lt;action&gt;"]... [--type &lt;type&gt;] [--force]
    /// </summary>
    // מגדירה את הפקודה add.
    private static Command CreateAddCommand()
    {
        // ארגומנט חובה ראשון: השם.
        Argument<string> nameArgument = CreateNameArgument();
        // ארגומנט חובה שני: הפעולה.
        Argument<string> actionArgument = CreateActionArgument();
        // אפשרות: --then (פעולות נוספות).
        Option<string[]> thenOption = CreateThenOption();
        // אפשרות: --type.
        Option<string> typeOption = CreateTypeOption();
        // אפשרות: --force. מסוג bool — אם כתבו אותה, הערך true.
        var forceOption = new Option<bool>("--force") { Description = "Allow a name that is already a command on this computer" };

        // יוצרים את הפקודה עם שם ותיאור, ומוסיפים לה את כל החלקים.
        var command = new Command("add", "Create a new shortcut")
        {
            // הארגומנטים והאפשרויות שהוגדרו למעלה.
            nameArgument, actionArgument, thenOption, typeOption, forceOption
        };

        // SetAction = מה לעשות כשהמשתמש מריץ את הפקודה. result = תוצאת הפירוק.
        command.SetAction(result => HandleAdd(
            // הערך של השם. ! = "בטוח לא null" (System.CommandLine כבר וידא שהוא קיים).
            result.GetValue(nameArgument)!,
            // הערך של הפעולה.
            result.GetValue(actionArgument)!,
            // ערכי --then. אם לא נכתבו — מערך ריק.
            result.GetValue(thenOption) ?? Array.Empty<string>(),
            // ערך --type, או null אם לא נכתב.
            result.GetValue(typeOption),
            // האם נכתב --force.
            result.GetValue(forceOption)));

        // מחזירים את הפקודה.
        return command;
    }

    /// <summary>
    /// Defines: shortcut update &lt;name&gt; "&lt;action&gt;" [--then "&lt;action&gt;"]... [--type &lt;type&gt;]
    /// </summary>
    // מגדירה את הפקודה update — כמו add, בלי --force.
    private static Command CreateUpdateCommand()
    {
        // השם.
        Argument<string> nameArgument = CreateNameArgument();
        // הפעולה החדשה.
        Argument<string> actionArgument = CreateActionArgument();
        // --then.
        Option<string[]> thenOption = CreateThenOption();
        // --type.
        Option<string> typeOption = CreateTypeOption();

        // יוצרים את הפקודה.
        var command = new Command("update", "Replace what an existing shortcut does")
        {
            // החלקים שלה.
            nameArgument, actionArgument, thenOption, typeOption
        };

        // מה לעשות בהרצה.
        command.SetAction(result => HandleUpdate(
            // השם.
            result.GetValue(nameArgument)!,
            // הפעולה.
            result.GetValue(actionArgument)!,
            // --then או מערך ריק.
            result.GetValue(thenOption) ?? Array.Empty<string>(),
            // --type או null.
            result.GetValue(typeOption)));

        // מחזירים.
        return command;
    }

    /// <summary>
    /// Defines: shortcut remove &lt;name&gt;
    /// </summary>
    // מגדירה את הפקודה remove.
    private static Command CreateRemoveCommand()
    {
        // השם.
        Argument<string> nameArgument = CreateNameArgument();
        // יוצרים את הפקודה עם הארגומנט.
        var command = new Command("remove", "Delete a shortcut") { nameArgument };

        // מה לעשות בהרצה.
        command.SetAction(result => HandleRemove(result.GetValue(nameArgument)!));
        // מחזירים.
        return command;
    }

    /// <summary>
    /// Defines: shortcut list
    /// </summary>
    // מגדירה את הפקודה list.
    private static Command CreateListCommand()
    {
        // פקודה בלי ארגומנטים.
        var command = new Command("list", "Show all shortcuts");

        // _ = "לא צריך את הפרמטר הזה".
        command.SetAction(_ => HandleList());
        // מחזירים.
        return command;
    }

    /// <summary>
    /// Defines: shortcut run &lt;name&gt; [arguments...]
    /// </summary>
    // מגדירה את הפקודה run.
    private static Command CreateRunCommand()
    {
        // השם.
        Argument<string> nameArgument = CreateNameArgument();
        // ארגומנט שמקבל כמה ערכים (מערך) — הארגומנטים הנוספים לפקודה.
        var argumentsArgument = new Argument<string[]>("arguments")
        {
            // תיאור לעזרה.
            Description = "Extra arguments for the last action (put -- before arguments that start with -)",
            // ZeroOrMore = אפשר לא לכתוב אף ערך, או כמה שרוצים.
            Arity = ArgumentArity.ZeroOrMore
        };

        // יוצרים את הפקודה.
        var command = new Command("run", "Run a saved shortcut") { nameArgument, argumentsArgument };

        // מה לעשות בהרצה.
        command.SetAction(result => RunShortcut(
            // השם.
            result.GetValue(nameArgument)!,
            // הארגומנטים, או מערך ריק.
            result.GetValue(argumentsArgument) ?? Array.Empty<string>(),
            // לא הופעלנו מכינוי.
            calledFromAlias: false));

        // מחזירים.
        return command;
    }

    /// <summary>
    /// Defines: shortcut install
    /// </summary>
    // מגדירה את הפקודה install.
    private static Command CreateInstallCommand()
    {
        // יוצרים.
        var command = new Command("install", "Let PowerShell, CMD and Bash/Zsh run shortcuts by their name alone");

        // מה לעשות בהרצה.
        command.SetAction(_ => HandleInstall());
        // מחזירים.
        return command;
    }

    /// <summary>
    /// Defines: shortcut uninstall
    /// </summary>
    // מגדירה את הפקודה uninstall.
    private static Command CreateUninstallCommand()
    {
        // יוצרים.
        var command = new Command("uninstall", "Remove the terminal integration (your shortcuts are kept)");

        // מה לעשות בהרצה.
        command.SetAction(_ => HandleUninstall());
        // מחזירים.
        return command;
    }

    /// <summary>
    /// Creates the "name" argument shared by several commands.
    /// </summary>
    // יוצרת את ארגומנט השם (משמש בכמה פקודות, לכן בפונקציה אחת).
    private static Argument<string> CreateNameArgument()
    {
        // "name" = השם שמופיע בעזרה. Description = ההסבר.
        return new Argument<string>("name") { Description = "The short name you will type, e.g. gs" };
    }

    /// <summary>
    /// Creates the "action" argument: what the shortcut runs, written in quotes.
    /// </summary>
    // יוצרת את ארגומנט הפעולה.
    private static Argument<string> CreateActionArgument()
    {
        // ארגומנט אחד בלבד — לכן צריך מרכאות. אם כותבים git status בלי מרכאות,
        // System.CommandLine יציג שגיאה על המילה המיותרת status.
        return new Argument<string>("action")
        {
            // ההסבר בעזרה.
            Description = "What to run, in quotes: a command, an app path, a folder or a URL"
        };
    }

    /// <summary>
    /// Creates the repeatable --then option for extra actions that run afterwards.
    /// </summary>
    // יוצרת את האפשרות --then. מסוג מערך, כך שאפשר לכתוב אותה כמה פעמים.
    private static Option<string[]> CreateThenOption()
    {
        // יוצרים ומחזירים.
        return new Option<string[]>("--then")
        {
            // ההסבר בעזרה.
            Description = "Another action to run afterwards, in quotes (can be repeated)"
        };
    }

    /// <summary>
    /// Creates the --type option that forces the type of the first action.
    /// </summary>
    // יוצרת את האפשרות --type (או בקיצור -t).
    private static Option<string> CreateTypeOption()
    {
        // השם המלא --type, והכינוי הקצר -t.
        var option = new Option<string>("--type", "-t")
        {
            // ההסבר בעזרה.
            Description = "Type of the first action. Detected automatically if omitted; use cd to change folder"
        };

        // מגבילים לערכים החוקיים בלבד. ערך אחר → שגיאה אוטומטית עם רשימת האפשרויות.
        option.AcceptOnlyFromAmong(ShortcutTypes.All);
        // מחזירים.
        return option;
    }

    /// <summary>
    /// Handles "shortcut add".
    /// </summary>
    // מבצעת את add.
    private static int HandleAdd(string name, string action, string[] thenActions, string? type, bool force)
    {
        // יוצרים את השירות (טוען את הקיצורים).
        var service = new ShortcutService();
        // הופכים את הטקסט לרשימת פעולות.
        List<ShortcutAction> actions = ShortcutService.BuildActions(action, thenActions, type);

        // מוסיפים ושומרים.
        service.Add(name, actions, force);
        // כותבים מחדש את קבצי הכינויים, כדי שהקיצור החדש יעבוד בטרמינלים.
        AliasScriptWriter.WriteAll(service.GetAll());

        // הודעת הצלחה עם תיאור מה נשמר.
        Console.WriteLine($"Shortcut '{name}' was added: {service.Get(name).Describe()}");
        // הסבר איך משתמשים בקיצור.
        PrintHowToUse(name);
        // הצלחה.
        return 0;
    }

    /// <summary>
    /// Handles "shortcut update".
    /// </summary>
    // מבצעת את update.
    private static int HandleUpdate(string name, string action, string[] thenActions, string? type)
    {
        // יוצרים את השירות.
        var service = new ShortcutService();
        // רשימת הפעולות החדשה.
        List<ShortcutAction> actions = ShortcutService.BuildActions(action, thenActions, type);

        // מעדכנים ושומרים.
        service.Update(name, actions);
        // כותבים מחדש את קבצי הכינויים.
        AliasScriptWriter.WriteAll(service.GetAll());

        // הודעת הצלחה.
        Console.WriteLine($"Shortcut '{name}' was updated: {service.Get(name).Describe()}");
        // הסבר איך משתמשים.
        PrintHowToUse(name);
        // הצלחה.
        return 0;
    }

    /// <summary>
    /// Handles "shortcut remove".
    /// </summary>
    // מבצעת את remove.
    private static int HandleRemove(string name)
    {
        // יוצרים את השירות.
        var service = new ShortcutService();

        // מוחקים ושומרים.
        service.Remove(name);
        // כותבים מחדש את קבצי הכינויים — הכינוי יוסר מהטרמינלים.
        AliasScriptWriter.WriteAll(service.GetAll());

        // הודעת הצלחה.
        Console.WriteLine($"Shortcut '{name}' was removed.");
        // הצלחה.
        return 0;
    }

    /// <summary>
    /// Handles "shortcut list" by printing every saved shortcut.
    /// </summary>
    // מבצעת את list.
    private static int HandleList()
    {
        // כל הקיצורים, ממוינים.
        IReadOnlyList<Shortcut> shortcuts = new ShortcutService().GetAll();

        // אם אין אף אחד...
        if (shortcuts.Count == 0)
        {
            // ...מסבירים איך מוסיפים.
            Console.WriteLine("No shortcuts yet. Add one with: shortcut add <name> \"<action>\"");
            // זו לא שגיאה.
            return 0;
        }

        // לכל קיצור...
        foreach (Shortcut shortcut in shortcuts)
        {
            // ...הסוגים של כל הפעולות, מופרדים בפסיקים (למשל cd,command).
            string types = string.Join(",", shortcut.Actions.Select(action => action.Type));
            // שורה: שם (מרופד ל־15 תווים), סוגים בסוגריים, ותיאור.
            Console.WriteLine($"{shortcut.Name,-15} [{types}] {shortcut.Describe()}");
        }

        // שורה ריקה.
        Console.WriteLine();
        // איפה הקובץ נשמר.
        Console.WriteLine($"Saved in: {AppPaths.ShortcutsFile}");
        // הצלחה.
        return 0;
    }

    /// <summary>
    /// Runs a shortcut by name, asking for approval first if it was not created through this tool.
    /// </summary>
    // מריצה קיצור — משמשת גם את shortcut run וגם את הכינויים (__run).
    private static int RunShortcut(string name, string[] extraArguments, bool calledFromAlias)
    {
        // יוצרים את השירות.
        var service = new ShortcutService();
        // מוצאים את הקיצור (או שגיאה אם לא קיים).
        Shortcut shortcut = service.Get(name);

        // אם הקיצור לא מהימן...
        if (!shortcut.Trusted)
        {
            // ...שואלים. אם המשתמש לא אישר...
            if (!ConfirmUntrusted(shortcut))
            {
                // ...מודיעים שבוטל.
                Console.WriteLine("Cancelled.");
                // ולא מריצים.
                return 1;
            }

            // המשתמש אישר — מסמנים ושומרים.
            service.MarkTrusted(shortcut);
        }

        // מריצים ומחזירים את קוד היציאה.
        return ShortcutRunner.Run(shortcut, extraArguments, calledFromAlias);
    }

    /// <summary>
    /// Handles "shortcut install": writes the alias files and connects them to every terminal.
    /// </summary>
    // מבצעת את install.
    private static int HandleInstall()
    {
        // כותבים את קבצי הכינויים (למקרה שעוד לא נוצרו).
        AliasScriptWriter.WriteAll(new ShortcutService().GetAll());
        // מחברים לכל הטרמינלים ומקבלים רשימה של מה ששונה.
        List<string> changed = ShellInstaller.Install();

        // כותרת.
        Console.WriteLine("Terminal integration installed in:");

        // לכל מקום ששונה...
        foreach (string place in changed)
            // ...מדפיסים אותו, עם הזחה.
            Console.WriteLine("  " + place);

        // שורה ריקה.
        Console.WriteLine();
        // הוראה: צריך טרמינל חדש, כי טרמינל פתוח כבר טען את ההגדרות שלו.
        Console.WriteLine("Open a new terminal window, then type any shortcut name directly.");

        // ב־Windows...
        if (OperatingSystem.IsWindows())
        {
            // ...PowerShell חוסם לפעמים הרצת סקריפטים כברירת מחדל, ואז הפרופיל לא ייטען. מסבירים...
            Console.WriteLine("If PowerShell says that running scripts is disabled, run this once in PowerShell:");
            // ...ונותנים את הפקודה שמתירה סקריפטים מקומיים, למשתמש הנוכחי בלבד.
            Console.WriteLine("  Set-ExecutionPolicy -Scope CurrentUser RemoteSigned");
        }

        // הצלחה.
        return 0;
    }

    /// <summary>
    /// Handles "shortcut uninstall": disconnects the alias files from every terminal.
    /// </summary>
    // מבצעת את uninstall.
    private static int HandleUninstall()
    {
        // מנתקים ומקבלים רשימה של מה ששונה.
        List<string> changed = ShellInstaller.Uninstall();

        // אם לא שונה כלום...
        if (changed.Count == 0)
        {
            // ...לא היה מותקן.
            Console.WriteLine("The terminal integration was not installed.");
            // זו לא שגיאה.
            return 0;
        }

        // כותרת.
        Console.WriteLine("Terminal integration removed from:");

        // לכל מקום ששונה...
        foreach (string place in changed)
            // ...מדפיסים.
            Console.WriteLine("  " + place);

        // מרגיעים: הקיצורים עצמם לא נמחקו.
        Console.WriteLine($"Your shortcuts are still saved in: {AppPaths.ShortcutsFile}");
        // הצלחה.
        return 0;
    }

    /// <summary>
    /// Tells the user how to use a shortcut they just added or changed.
    /// </summary>
    // מסבירה למשתמש איך להשתמש בקיצור.
    private static void PrintHowToUse(string name)
    {
        // אם install כבר בוצע...
        if (ShellInstaller.IsInstalled())
            // ...מספיק לפתוח טרמינל חדש ולהקליד את השם.
            Console.WriteLine($"Open a new terminal window and type: {name}");
        // אחרת...
        else
            // ...מסבירים איך להריץ עכשיו, ואיך להגיע להקלדת השם בלבד.
            Console.WriteLine($"Run it with: shortcut run {name}   (run 'shortcut install' once to type just '{name}')");
    }

    /// <summary>
    /// Shows what an unapproved shortcut will do and asks the user to confirm.
    /// </summary>
    // מבקשת אישור לקיצור לא מהימן. מחזירה true רק אם הוקלד y.
    private static bool ConfirmUntrusted(Shortcut shortcut)
    {
        // מסבירים שהקיצור לא נוצר דרך הכלי.
        Console.WriteLine($"Shortcut '{shortcut.Name}' was not created by this tool and has not been approved yet.");
        // מראים בדיוק מה ירוץ.
        Console.WriteLine($"It will run: {shortcut.Describe()}");
        // שואלים באותה שורה. N גדולה = ברירת המחדל היא "לא".
        Console.Write("Run it and trust it from now on? (y/N): ");

        // קוראים תשובה (יכולה להיות null אם הקלט נסגר).
        string? answer = Console.ReadLine();
        // true רק אם התשובה היא y (בלי רווחים, בלי תלות באות גדולה/קטנה).
        return answer?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
    }
}
