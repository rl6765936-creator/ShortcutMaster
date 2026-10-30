// =====================================================================
// Services/AppPaths.cs — עותק עם הסברים
// תפקיד הקובץ: לרכז במקום אחד את כל נתיבי הקבצים שהכלי משתמש בהם.
// סוגריים מסולסלים { } פותחים וסוגרים בלוק, ולא הוסבר עליהם בנפרד.
// =====================================================================

// הקובץ שייך למרחב השמות של השירותים.
namespace ShortcutMaster.Services;

/// <summary>
/// Central place for all file paths the tool uses inside the user's personal data folder.
/// </summary>
// מחלקה סטטית — אין צורך ליצור ממנה אובייקט, ניגשים ישירות: AppPaths.ShortcutsFile.
public static class AppPaths
{
    // תיקיית הנתונים של הכלי. => אומר שהערך מחושב בכל פעם שניגשים אליו.
    // ב־Windows: C:\Users\<שם>\AppData\Roaming\ShortcutMaster. ב־Linux/macOS: ~/.config/ShortcutMaster.
    public static string DataFolder => Path.Combine(GetFolder(Environment.SpecialFolder.ApplicationData), "ShortcutMaster");

    // הקובץ שבו נשמרים הקיצורים עצמם.
    public static string ShortcutsFile => Path.Combine(DataFolder, "shortcuts.json");

    // קובץ הכינויים שנוצר עבור PowerShell.
    public static string PowerShellScript => Path.Combine(DataFolder, "aliases.ps1");

    // קובץ הכינויים שנוצר עבור Bash, Zsh ו־Git Bash.
    public static string BashScript => Path.Combine(DataFolder, "aliases.sh");

    // קובץ ה"מאקרו" שנוצר עבור CMD (פקודת doskey).
    public static string CmdMacroFile => Path.Combine(DataFolder, "aliases.doskey");

    /// <summary>
    /// Returns a system folder path even if that folder does not exist yet (for example ~/.config on a new Linux user).
    /// </summary>
    // מחזירה נתיב של תיקיית מערכת (תיקיית בית, מסמכים וכו').
    public static string GetFolder(Environment.SpecialFolder folder)
    {
        // DoNotVerify = החזר את הנתיב גם אם התיקייה עוד לא קיימת.
        // בלי זה, .NET מחזיר מחרוזת ריקה כשהתיקייה חסרה — והקבצים היו נשמרים במקום הלא נכון.
        return Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
    }
}
