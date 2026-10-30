// =====================================================================
// Services/CommandLocator.cs — עותק עם הסברים
// תפקיד הקובץ: לבדוק אם שם כבר "תפוס" במחשב — כדי שקיצור לא יסתיר
// בטעות פקודה קיימת כמו git או dir (טיפול בהתנגשויות שמות, שלב 4).
// סוגריים מסולסלים { } פותחים וסוגרים בלוק, ולא הוסבר עליהם בנפרד.
// =====================================================================

// הקובץ שייך למרחב השמות של השירותים.
namespace ShortcutMaster.Services;

/// <summary>
/// Checks whether a name is already taken by a program on the PATH or by a built-in shell command.
/// </summary>
// מחלקה סטטית שבודקת אם שם כבר קיים כפקודה.
public static class CommandLocator
{
    // פקודות שמובנות בתוך הטרמינל עצמו (לא קבצים בדיסק), ולכן לא יימצאו בחיפוש ב־PATH.
    private static readonly string[] ShellBuiltIns =
    {
        // רשימה של פקודות כאלה מ־CMD, PowerShell ו־Bash.
        "alias", "call", "cd", "clear", "cls", "copy", "del", "dir", "echo", "exit", "export",
        // המשך הרשימה.
        "history", "md", "mkdir", "move", "popd", "pushd", "pwd", "rd", "ren", "set", "source", "start", "type"
    };

    /// <summary>
    /// Returns true if typing this name in a terminal already runs something.
    /// </summary>
    // מחזירה true אם הקלדת השם בטרמינל כבר מריצה משהו.
    public static bool Exists(string name)
    {
        // אם זו פקודה מובנית (בלי תלות באותיות גדולות/קטנות)...
        if (ShellBuiltIns.Contains(name, StringComparer.OrdinalIgnoreCase))
            // ...השם תפוס.
            return true;

        // עוברים על כל תיקייה ברשימת ה־PATH (התיקיות שבהן הטרמינל מחפש תוכנות).
        foreach (string folder in GetPathFolders())
        {
            // ועל כל סיומת אפשרית של קובץ הרצה (.exe, .cmd...).
            foreach (string extension in GetExecutableExtensions())
            {
                // אם קיים בתיקייה קובץ בשם הזה עם הסיומת הזו...
                if (File.Exists(Path.Combine(folder, name + extension)))
                    // ...השם תפוס.
                    return true;
            }
        }

        // לא נמצא — השם פנוי.
        return false;
    }

    /// <summary>
    /// Returns the folders listed in the PATH environment variable.
    /// </summary>
    // מחזירה את רשימת התיקיות שבמשתנה הסביבה PATH.
    private static IEnumerable<string> GetPathFolders()
    {
        // קוראים את PATH. אם הוא לא קיים — משתמשים במחרוזת ריקה.
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        // מפצלים את PATH לתיקיות.
        return path
            // Path.PathSeparator הוא ';' ב־Windows ו־':' ב־Linux/macOS. מסירים ריקים ורווחים.
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // ב־Windows לפעמים תיקייה ב־PATH עטופה במרכאות — מסירים אותן.
            .Select(folder => folder.Trim('"'));
    }

    /// <summary>
    /// Returns the file extensions of runnable files: .exe, .cmd and so on on Windows, none elsewhere.
    /// </summary>
    // מחזירה את סיומות קבצי ההרצה.
    private static string[] GetExecutableExtensions()
    {
        // ב־Linux/macOS לתוכנות אין סיומת (git, ls)...
        if (!OperatingSystem.IsWindows())
            // ...לכן מחפשים את השם בלבד.
            return new[] { string.Empty };

        // ב־Windows הרשימה נמצאת במשתנה PATHEXT. אם הוא חסר — משתמשים ברשימה הבסיסית.
        string pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        // מפצלים לפי ';' למערך של סיומות.
        return pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
