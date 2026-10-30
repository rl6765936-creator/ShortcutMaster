// =====================================================================
// Services/ShortcutRunner.cs — עותק עם הסברים
// תפקיד הקובץ: להריץ את הפעולות של קיצור, אחת אחרי השנייה.
// סוגריים מסולסלים { } פותחים וסוגרים בלוק, ולא הוסבר עליהם בנפרד.
// =====================================================================

// ספרייה להפעלת תהליכים — Process ו־ProcessStartInfo.
using System.Diagnostics;
// מאפשר להשתמש במודלים.
using ShortcutMaster.Models;

// הקובץ שייך למרחב השמות של השירותים.
namespace ShortcutMaster.Services;

/// <summary>
/// Runs the actions of a shortcut one after another as operating system processes.
/// </summary>
// מחלקה סטטית שמריצה קיצורים.
public static class ShortcutRunner
{
    /// <summary>
    /// Runs all actions in order and stops at the first command that fails.
    /// Extra arguments are added to the last action.
    /// </summary>
    // הפונקציה הראשית. extraArguments = מה שהמשתמש כתב אחרי הכינוי.
    // calledFromAlias = האם הופעלנו מכינוי בטרמינל (true) או מ־shortcut run (false).
    public static int Run(Shortcut shortcut, IReadOnlyList<string> extraArguments, bool calledFromAlias)
    {
        // נרשמים לאירוע Ctrl+C, כדי שהתוכנית שלנו לא תיסגר לפני הפקודה שהיא מריצה.
        Console.CancelKeyPress += LetChildHandleCtrlC;

        // try/finally — מה שב־finally ירוץ תמיד, גם אם הייתה שגיאה.
        try
        {
            // מריצים את הפעולות.
            return RunActions(shortcut, extraArguments, calledFromAlias);
        }
        finally
        {
            // מבטלים את ההרשמה לאירוע.
            Console.CancelKeyPress -= LetChildHandleCtrlC;
        }
    }

    /// <summary>
    /// Goes over the actions, keeping track of the folder set by a cd action.
    /// </summary>
    // עוברת על הפעולות לפי הסדר.
    private static int RunActions(Shortcut shortcut, IReadOnlyList<string> extraArguments, bool calledFromAlias)
    {
        // התיקייה שבה ירוצו הפעולות. null = התיקייה הנוכחית.
        string? workingFolder = null;
        // המיקום של הפעולה האחרונה ברשימה.
        int lastIndex = shortcut.Actions.Count - 1;

        // לולאה על כל הפעולות, מהראשונה עד האחרונה.
        for (int i = 0; i <= lastIndex; i++)
        {
            // הפעולה הנוכחית.
            ShortcutAction action = shortcut.Actions[i];

            // אם זו פעולת מעבר תיקייה...
            if (action.Is(ShortcutTypes.Cd))
            {
                // ...הפעולות הבאות ירוצו בתוך התיקייה הזו.
                workingFolder = action.Target;

                // תוכנית חיצונית לא יכולה להזיז את הטרמינל שהפעיל אותה. לכן, אם הופעלנו דרך shortcut run...
                if (!calledFromAlias)
                    // ...מסבירים שכדי שהטרמינל עצמו יעבור תיקייה, צריך להקליד את הכינוי.
                    Console.WriteLine($"Note: to move your terminal into {action.Target}, type '{shortcut.Name}' directly.");

                // ממשיכים לפעולה הבאה (אין מה "להריץ" כאן).
                continue;
            }

            // ארגומנטים נוספים הולכים רק לפעולה האחרונה; לשאר — רשימה ריקה.
            IReadOnlyList<string> arguments = i == lastIndex ? extraArguments : Array.Empty<string>();
            // מריצים את הפעולה ומקבלים קוד יציאה.
            int exitCode = RunAction(action, arguments, workingFolder);

            // אם הפעולה נכשלה (קוד שונה מ־0)...
            if (exitCode != 0)
                // ...עוצרים, ולא מריצים את הפעולות שאחריה.
                return exitCode;
        }

        // הכל עבר בהצלחה.
        return 0;
    }

    /// <summary>
    /// Starts one action. Commands are awaited; apps, folders and URLs are opened and left running.
    /// </summary>
    // מריצה פעולה אחת.
    private static int RunAction(ShortcutAction action, IReadOnlyList<string> extraArguments, string? workingFolder)
    {
        // הסוג באותיות קטנות.
        string type = action.Type.ToLowerInvariant();

        // בוחרים איך להכין את התהליך לפי הסוג.
        ProcessStartInfo startInfo = type switch
        {
            // פקודת טרמינל → דרך ה־Shell של המערכת.
            ShortcutTypes.Command => BuildShellCommand(action, extraArguments),
            // תוכנה, תיקייה או אתר → מערכת ההפעלה פותחת בתוכנה המתאימה.
            ShortcutTypes.App or ShortcutTypes.Folder or ShortcutTypes.Url => BuildShellExecute(action, extraArguments),
            // סוג לא מוכר → שגיאה.
            _ => throw new InvalidOperationException($"Unknown action type '{action.Type}'.")
        };

        // אם הייתה פעולת cd קודם...
        if (workingFolder != null)
            // ...התהליך יתחיל בתוך התיקייה הזו.
            startInfo.WorkingDirectory = workingFolder;

        // מפעילים את התהליך. using משחרר את המשאבים בסוף. Process? = עלול להיות null.
        using Process? process = Process.Start(startInfo);

        // אם אין תהליך, או שזו לא פקודת טרמינל (לא מחכים לדפדפן או לסייר)...
        if (process == null || type != ShortcutTypes.Command)
            // ...הצלחה.
            return 0;

        // מחכים שהפקודה תסתיים.
        process.WaitForExit();
        // מחזירים את קוד היציאה שלה.
        return process.ExitCode;
    }

    /// <summary>
    /// Prepares a terminal command to run through the system shell (cmd on Windows, sh elsewhere).
    /// </summary>
    // מכינה הרצה דרך ה־Shell. ככה עובדות גם פקודות כמו npm (שב־Windows הן בעצם npm.cmd) וגם && ו־|.
    private static ProcessStartInfo BuildShellCommand(ShortcutAction action, IReadOnlyList<string> extraArguments)
    {
        // רצף של כל החלקים: היעד, הארגומנטים השמורים, והארגומנטים הנוספים.
        IEnumerable<string> parts = new[] { action.Target }.Concat(action.Arguments).Concat(extraArguments);
        // מחברים לשורה אחת.
        string commandLine = JoinCommandLine(parts);
        // UseShellExecute = false → הפלט מופיע באותו טרמינל.
        var startInfo = new ProcessStartInfo { UseShellExecute = false };

        // ב־Windows...
        if (OperatingSystem.IsWindows())
        {
            // ...מריצים דרך cmd.exe.
            startInfo.FileName = "cmd.exe";
            // /d = בלי AutoRun (כדי לא לטעון את הכינויים שוב בכל הרצה), /c = הרץ וצא,
            // /s + מרכאות חיצוניות = cmd מסיר רק אותן ומשאיר את המרכאות שבתוך הפקודה תקינות.
            startInfo.Arguments = $"/d /s /c \"{commandLine}\"";
        }
        // ב־Linux / macOS...
        else
        {
            // ...מריצים דרך sh.
            startInfo.FileName = "/bin/sh";
            // -c = "הרץ את המחרוזת הבאה".
            startInfo.ArgumentList.Add("-c");
            // הפקודה, כארגומנט אחד.
            startInfo.ArgumentList.Add(commandLine);
        }

        // מחזירים את ההגדרות.
        return startInfo;
    }

    /// <summary>
    /// Prepares an app, folder or URL to be opened by the operating system.
    /// </summary>
    // מכינה פתיחה של תוכנה, תיקייה או אתר.
    private static ProcessStartInfo BuildShellExecute(ShortcutAction action, IReadOnlyList<string> extraArguments)
    {
        // יוצרים ומחזירים הגדרות.
        return new ProcessStartInfo
        {
            // מה לפתוח.
            FileName = action.Target,
            // הארגומנטים השמורים והנוספים, כשורה אחת.
            Arguments = JoinCommandLine(action.Arguments.Concat(extraArguments)),
            // true → מערכת ההפעלה בוחרת במה לפתוח (דפדפן ברירת מחדל, סייר קבצים...).
            UseShellExecute = true
        };
    }

    /// <summary>
    /// Joins parts into one command line, quoting any part that contains a space.
    /// </summary>
    // מחברת חלקים לשורה אחת; חלק עם רווח נעטף במרכאות.
    private static string JoinCommandLine(IEnumerable<string> parts)
    {
        // לכל חלק: אם יש בו רווח — עם מרכאות, אחרת כמו שהוא. ואז חיבור ברווחים.
        return string.Join(" ", parts.Select(part => part.Contains(' ') ? $"\"{part}\"" : part));
    }

    /// <summary>
    /// Keeps this program alive on Ctrl+C so the running command can stop by itself.
    /// </summary>
    // נקראת כשהמשתמש לוחץ Ctrl+C.
    private static void LetChildHandleCtrlC(object? sender, ConsoleCancelEventArgs e)
    {
        // Cancel = true → התוכנית שלנו לא נסגרת. הפקודה שרצה (למשל npm run dev) נעצרת לבד,
        // ואז אנחנו מסיימים בצורה מסודרת ומחזירים את קוד היציאה שלה.
        e.Cancel = true;
    }
}
