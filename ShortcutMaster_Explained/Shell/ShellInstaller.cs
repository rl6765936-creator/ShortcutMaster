// =====================================================================
// Shell/ShellInstaller.cs — עותק עם הסברים
// תפקיד הקובץ: לחבר פעם אחת את קבצי הכינויים לכל טרמינל, כך שייטענו
// אוטומטית בכל פתיחה של טרמינל חדש:
//   PowerShell — שורה בקובץ profile.ps1
//   Bash / Git Bash / Zsh — שורה בקבצים .bashrc / .zshrc
//   CMD — הגדרת AutoRun ברישום של Windows (Registry)
// השורה שלנו נכתבת בתוך "בלוק" מסומן, כדי שאפשר יהיה להחליף או למחוק
// רק אותה, בלי לגעת בשאר התוכן של המשתמש.
// סוגריים מסולסלים { } פותחים וסוגרים בלוק, ולא הוסבר עליהם בנפרד.
// =====================================================================

// מאפשר לסמן פונקציות כ"רק ל־Windows" ([SupportedOSPlatform]).
using System.Runtime.Versioning;
// ספרייה לקידודי טקסט (UTF-8, UTF-16).
using System.Text;
// ספרייה לגישה לרישום של Windows (Registry).
using Microsoft.Win32;
// מאפשר להשתמש ב־AppPaths וב־CommandLocator.
using ShortcutMaster.Services;

// הקובץ שייך לשכבת האינטגרציה עם הטרמינלים.
namespace ShortcutMaster.Shell;

/// <summary>
/// Connects the alias files to each terminal once: PowerShell and Bash/Zsh profiles, and the CMD AutoRun setting.
/// </summary>
// מחלקה סטטית שמתקינה ומסירה את החיבור לטרמינלים.
public static class ShellInstaller
{
    // שורת הפתיחה של הבלוק שלנו. # = הערה גם ב־PowerShell וגם ב־Bash.
    private const string StartMarker = "# >>> ShortcutMaster >>>";
    // שורת הסגירה של הבלוק.
    private const string EndMarker = "# <<< ShortcutMaster <<<";

    // המפתח ברישום שבו CMD מחפש את ההגדרות של המשתמש. @ = הלוכסנים ההפוכים נשארים כמו שהם.
    private const string CmdRegistryKey = @"Software\Microsoft\Command Processor";
    // שם הערך: AutoRun = פקודה ש־CMD מריץ בכל פתיחה.
    private const string CmdAutoRunValue = "AutoRun";
    // הפקודה שנוסיף: אם קובץ הכינויים קיים — טען אותו עם doskey.
    // %APPDATA% נשאר כטקסט, ו־CMD מחליף אותו בנתיב האמיתי — כך זה עובד גם כששם המשתמש בעברית.
    private const string CmdAutoRunCommand =
        // הטקסט עצמו. \" = מרכאה בתוך מחרוזת, \\ = לוכסן הפוך אחד.
        "if exist \"%APPDATA%\\ShortcutMaster\\aliases.doskey\" doskey /macrofile=\"%APPDATA%\\ShortcutMaster\\aliases.doskey\"";

    /// <summary>
    /// Adds the loading line to every supported shell and returns a list of what was changed.
    /// </summary>
    // מתקינה את החיבור לכל הטרמינלים ומחזירה רשימה של מה ששונה.
    public static List<string> Install()
    {
        // רשימת המקומות ששונו — תוצג למשתמש.
        var changed = new List<string>();

        // לכל קובץ פרופיל של PowerShell (רק טרמינלים שקיימים במחשב)...
        foreach (string profile in GetPowerShellProfiles(onlyExistingShells: true))
        {
            // ...כותבים את בלוק הטעינה. קובץ חדש ייווצר עם BOM.
            WriteBlock(profile, GetPowerShellLoadLine(), useBomForNewFile: true);
            // מוסיפים לרשימה.
            changed.Add($"PowerShell  {profile}");
        }

        // לכל קובץ הפעלה של Bash/Zsh...
        foreach (string rcFile in GetBashRcFiles(onlyExistingShells: true))
        {
            // ...כותבים את בלוק הטעינה. בלי BOM.
            WriteBlock(rcFile, GetBashLoadLine(), useBomForNewFile: false);
            // מוסיפים לרשימה.
            changed.Add($"Bash/Zsh    {rcFile}");
        }

        // CMD קיים רק ב־Windows...
        if (OperatingSystem.IsWindows())
        {
            // ...מוסיפים את הפקודה ל־AutoRun.
            InstallCmd();
            // מוסיפים לרשימה. $@"..." = מחרוזת עם משתנים ועם לוכסנים הפוכים רגילים.
            changed.Add($@"CMD         HKEY_CURRENT_USER\{CmdRegistryKey} -> {CmdAutoRunValue}");
        }

        // מחזירים את הרשימה.
        return changed;
    }

    /// <summary>
    /// Removes the loading line from every shell and returns a list of what was changed.
    /// </summary>
    // מסירה את החיבור מכל הטרמינלים.
    public static List<string> Uninstall()
    {
        // רשימת המקומות ששונו.
        var changed = new List<string>();
        // כל הקבצים האפשריים — גם של טרמינלים שאולי כבר הוסרו מהמחשב.
        IEnumerable<string> files = GetPowerShellProfiles(onlyExistingShells: false)
            // מחברים לרשימה אחת עם קבצי Bash/Zsh.
            .Concat(GetBashRcFiles(onlyExistingShells: false));

        // לכל קובץ...
        foreach (string file in files)
        {
            // ...מנסים להסיר את הבלוק. אם הוא היה שם...
            if (RemoveBlockFromFile(file))
                // ...מוסיפים לרשימה.
                changed.Add(file);
        }

        // ב־Windows, אם הפקודה הייתה ב־AutoRun והוסרה...
        if (OperatingSystem.IsWindows() && UninstallCmd())
            // ...מוסיפים לרשימה.
            changed.Add($@"HKEY_CURRENT_USER\{CmdRegistryKey} -> {CmdAutoRunValue}");

        // מחזירים את הרשימה.
        return changed;
    }

    /// <summary>
    /// Returns true if at least one shell is already set up to load the aliases.
    /// </summary>
    // בודקת אם install כבר בוצע — כדי לתת למשתמש את ההוראה הנכונה אחרי add.
    public static bool IsInstalled()
    {
        // כל הקבצים האפשריים.
        IEnumerable<string> files = GetPowerShellProfiles(onlyExistingShells: false)
            // יחד עם קבצי Bash/Zsh.
            .Concat(GetBashRcFiles(onlyExistingShells: false));

        // אם יש קובץ קיים שמכיל את שורת הפתיחה שלנו...
        if (files.Any(file => File.Exists(file) && File.ReadAllText(file).Contains(StartMarker)))
            // ...מותקן.
            return true;

        // אחרת — בודקים את CMD (רק ב־Windows).
        return OperatingSystem.IsWindows() && IsCmdInstalled();
    }

    /// <summary>
    /// Returns the PowerShell profile files: Windows PowerShell and PowerShell 7 on Windows, pwsh elsewhere.
    /// </summary>
    // מחזירה את קבצי הפרופיל של PowerShell.
    private static List<string> GetPowerShellProfiles(bool onlyExistingShells)
    {
        // ב־Windows...
        if (OperatingSystem.IsWindows())
        {
            // ...תיקיית "המסמכים" של המשתמש (מתחשב גם בהעברה ל־OneDrive).
            string documents = AppPaths.GetFolder(Environment.SpecialFolder.MyDocuments);
            // מחזירים שני קבצים.
            return new List<string>
            {
                // Windows PowerShell 5.1 (המובנה ב־Windows). profile.ps1 נטען בכל חלון, כולל VS Code.
                Path.Combine(documents, "WindowsPowerShell", "profile.ps1"),
                // PowerShell 7 (pwsh).
                Path.Combine(documents, "PowerShell", "profile.ps1")
            };
        }

        // ב־Linux/macOS — התיקייה ~/.config.
        string configFolder = AppPaths.GetFolder(Environment.SpecialFolder.ApplicationData);
        // קובץ הפרופיל של pwsh שם.
        string profile = Path.Combine(configFolder, "powershell", "profile.ps1");

        // אם ביקשו רק טרמינלים קיימים, ו־pwsh לא מותקן...
        if (onlyExistingShells && !CommandLocator.Exists("pwsh"))
            // ...רשימה ריקה (לא יוצרים קבצים מיותרים).
            return new List<string>();

        // אחרת — מחזירים את הקובץ.
        return new List<string> { profile };
    }

    /// <summary>
    /// Returns the Bash and Zsh startup files: .bashrc always (also used by Git Bash), .zshrc where Zsh exists.
    /// </summary>
    // מחזירה את קבצי ההפעלה של Bash ו־Zsh.
    private static List<string> GetBashRcFiles(bool onlyExistingShells)
    {
        // תיקיית הבית.
        string home = AppPaths.GetFolder(Environment.SpecialFolder.UserProfile);
        // הקובץ של Bash (ושל Git Bash ב־Windows).
        string bashrc = Path.Combine(home, ".bashrc");
        // הקובץ של Zsh.
        string zshrc = Path.Combine(home, ".zshrc");
        // תמיד כוללים את .bashrc.
        var files = new List<string> { bashrc };

        // ב־Windows אין Zsh (רק Git Bash)...
        if (OperatingSystem.IsWindows())
            // ...מסיימים.
            return files;

        // מוסיפים את .zshrc: אם לא ביקשו רק קיימים, או ש־zsh מותקן, או שהקובץ כבר קיים.
        if (!onlyExistingShells || CommandLocator.Exists("zsh") || File.Exists(zshrc))
            // מוסיפים.
            files.Add(zshrc);

        // מחזירים.
        return files;
    }

    /// <summary>
    /// Builds the line that PowerShell runs at startup to load aliases.ps1.
    /// </summary>
    // בונה את השורה ש־PowerShell יריץ בכל פתיחה.
    private static string GetPowerShellLoadLine()
    {
        // הנתיב לקובץ הכינויים:
        string path = OperatingSystem.IsWindows()
            // ב־Windows — דרך המשתנה $env:APPDATA, כך שהשורה כולה באנגלית (חשוב אם שם המשתמש בעברית).
            ? "\"$env:APPDATA\\ShortcutMaster\\aliases.ps1\""
            // ב־Linux/macOS — הנתיב המלא, במרכאות בודדות.
            : "'" + AppPaths.PowerShellScript.Replace("'", "''") + "'";

        // השורה: אם הקובץ קיים — טען אותו. ". קובץ" (נקודה ורווח) = טעינה לתוך הטרמינל הנוכחי.
        return $"if (Test-Path {path}) {{ . {path} }}";
    }

    /// <summary>
    /// Builds the line that Bash/Zsh run at startup to load aliases.sh.
    /// </summary>
    // בונה את השורה ש־Bash/Zsh יריצו בכל פתיחה.
    private static string GetBashLoadLine()
    {
        // הנתיב לקובץ, בצורה ש־Bash מבין, במרכאות בודדות.
        string path = "'" + AliasScriptWriter.ToBashPath(AppPaths.BashScript).Replace("'", "'\\''") + "'";
        // [ -f קובץ ] = האם הקובץ קיים. && = רק אם כן. . קובץ = טען אותו.
        return $"[ -f {path} ] && . {path}";
    }

    /// <summary>
    /// Adds (or replaces) the ShortcutMaster block at the end of a startup file, keeping its encoding.
    /// </summary>
    // מוסיפה (או מחליפה) את הבלוק שלנו בסוף קובץ.
    private static void WriteBlock(string filePath, string line, bool useBomForNewFile)
    {
        // קוראים את הקובץ ואת הקידוד שלו, כדי לכתוב אותו בחזרה באותו קידוד.
        (string text, Encoding encoding) = ReadText(filePath, useBomForNewFile);
        // שומרים על סוג ירידת השורה של הקובץ: \r\n (Windows) או \n (Linux).
        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        // התוכן הקיים בלי הבלוק הישן שלנו (אם היה), ובלי ירידות שורה מיותרות בסוף.
        string kept = RemoveBlock(text).TrimEnd('\r', '\n');
        // הבלוק החדש: פתיחה, השורה, סגירה.
        string block = StartMarker + newLine + line + newLine + EndMarker + newLine;
        // אם הקובץ היה ריק — רק הבלוק; אחרת — התוכן, שורה ריקה, והבלוק.
        string result = kept.Length == 0 ? block : kept + newLine + newLine + block;

        // יוצרים את התיקייה אם חסרה (למשל WindowsPowerShell). ! = "אני בטוח שזה לא null".
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        // כותבים, באותו קידוד.
        File.WriteAllText(filePath, result, encoding);
    }

    /// <summary>
    /// Removes the ShortcutMaster block from a startup file. Returns true if the file was changed.
    /// </summary>
    // מסירה את הבלוק שלנו מקובץ.
    private static bool RemoveBlockFromFile(string filePath)
    {
        // אם הקובץ לא קיים...
        if (!File.Exists(filePath))
            // ...אין מה לעשות.
            return false;

        // קוראים את התוכן והקידוד.
        (string text, Encoding encoding) = ReadText(filePath, useBomForNewFile: false);

        // אם הבלוק שלנו לא שם...
        if (!text.Contains(StartMarker))
            // ...לא משנים כלום.
            return false;

        // כותבים בחזרה בלי הבלוק.
        File.WriteAllText(filePath, RemoveBlock(text), encoding);
        // הקובץ שונה.
        return true;
    }

    /// <summary>
    /// Returns the text without the ShortcutMaster block (from the start marker to the end marker).
    /// </summary>
    // מחזירה את הטקסט בלי הבלוק שלנו.
    private static string RemoveBlock(string text)
    {
        // מחפשים את שורת הפתיחה. Ordinal = השוואה מדויקת, תו מול תו.
        int start = text.IndexOf(StartMarker, StringComparison.Ordinal);

        // לא נמצאה...
        if (start < 0)
            // ...מחזירים כמו שהוא.
            return text;

        // מחפשים את שורת הסגירה, מהפתיחה והלאה.
        int end = text.IndexOf(EndMarker, start, StringComparison.Ordinal);

        // לא נמצאה (מישהו מחק אותה ידנית)...
        if (end < 0)
            // ...לא נוגעים, כדי לא למחוק בטעות תוכן של המשתמש.
            return text;

        // מזיזים את הסוף לאחרי שורת הסגירה.
        end += EndMarker.Length;

        // ממשיכים לדלג על ירידות השורה שאחריה...
        while (end < text.Length && (text[end] == '\r' || text[end] == '\n'))
            // ...תו אחר תו.
            end++;

        // מוחקים את הקטע מ־start ובאורך end - start.
        return text.Remove(start, end - start);
    }

    /// <summary>
    /// Reads a text file and detects its encoding (UTF-16, UTF-8 with or without BOM).
    /// A missing file is returned as empty text with the requested default encoding.
    /// </summary>
    // קוראת קובץ ומזהה את הקידוד שלו, כדי לא להרוס קבצים קיימים של המשתמש.
    private static (string Text, Encoding Encoding) ReadText(string filePath, bool useBomForNewFile)
    {
        // קובץ שלא קיים...
        if (!File.Exists(filePath))
            // ...טקסט ריק, וקידוד UTF-8 (עם או בלי BOM, לפי הבקשה).
            return (string.Empty, new UTF8Encoding(useBomForNewFile));

        // קוראים את הקובץ כבייטים גולמיים.
        byte[] bytes = File.ReadAllBytes(filePath);

        // אם הוא מתחיל ב־FF FE — זה UTF-16 (נפוץ בפרופילים שנוצרו ב־PowerShell 5.1)...
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            // ...מפענחים בלי שני הבייטים הראשונים, ומחזירים קידוד UTF-16.
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), Encoding.Unicode);

        // האם הקובץ מתחיל ב־EF BB BF (סימן BOM של UTF-8)?
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        // כמה בייטים לדלג בהתחלה.
        int skip = hasBom ? 3 : 0;

        // מפענחים כ־UTF-8, ומחזירים קידוד שישמור על אותו מצב BOM.
        return (Encoding.UTF8.GetString(bytes, skip, bytes.Length - skip), new UTF8Encoding(hasBom));
    }

    /// <summary>
    /// Adds the doskey loading command to CMD's AutoRun setting, keeping anything already there.
    /// </summary>
    // מוסיפה את הפקודה שלנו ל־AutoRun של CMD.
    // [SupportedOSPlatform("windows")] = מסמן לקומפיילר שזה קוד ל־Windows בלבד.
    [SupportedOSPlatform("windows")]
    private static void InstallCmd()
    {
        // פותחים (או יוצרים) את המפתח ברישום של המשתמש הנוכחי. לא דורש הרשאות מנהל.
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(CmdRegistryKey);
        // הערך הקיים, בלי הפקודה שלנו (אם כבר הייתה — כדי לא להכפיל).
        string current = RemoveCmdCommand(ReadAutoRun(key));
        // אם לא היה כלום — רק הפקודה שלנו; אחרת — הקיים, " & " (= ואחר כך), והפקודה שלנו.
        string updated = current.Length == 0 ? CmdAutoRunCommand : current + " & " + CmdAutoRunCommand;

        // שומרים. ExpandString = ערך שבו %APPDATA% מוחלף בנתיב האמיתי.
        key.SetValue(CmdAutoRunValue, updated, RegistryValueKind.ExpandString);
    }

    /// <summary>
    /// Removes the doskey loading command from CMD's AutoRun setting. Returns true if it was there.
    /// </summary>
    // מסירה את הפקודה שלנו מ־AutoRun.
    [SupportedOSPlatform("windows")]
    private static bool UninstallCmd()
    {
        // פותחים את המפתח לכתיבה. אם הוא לא קיים — null.
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(CmdRegistryKey, writable: true);

        // אין מפתח...
        if (key == null)
            // ...אין מה להסיר.
            return false;

        // הערך הנוכחי.
        string current = ReadAutoRun(key);

        // אם הפקודה שלנו לא שם...
        if (!current.Contains(CmdAutoRunCommand))
            // ...אין מה להסיר.
            return false;

        // מה שנשאר אחרי הסרת הפקודה שלנו.
        string remaining = RemoveCmdCommand(current);

        // אם לא נשאר כלום...
        if (remaining.Length == 0)
            // ...מוחקים את הערך לגמרי.
            key.DeleteValue(CmdAutoRunValue, throwOnMissingValue: false);
        // אחרת...
        else
            // ...שומרים את מה שנשאר (פקודות של המשתמש עצמו).
            key.SetValue(CmdAutoRunValue, remaining, RegistryValueKind.ExpandString);

        // הוסר.
        return true;
    }

    /// <summary>
    /// Returns true if CMD's AutoRun setting already loads the aliases.
    /// </summary>
    // בודקת אם הפקודה שלנו כבר ב־AutoRun.
    [SupportedOSPlatform("windows")]
    private static bool IsCmdInstalled()
    {
        // פותחים את המפתח לקריאה בלבד.
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(CmdRegistryKey);
        // מותקן אם המפתח קיים והערך מכיל את הפקודה שלנו.
        return key != null && ReadAutoRun(key).Contains(CmdAutoRunCommand);
    }

    /// <summary>
    /// Reads the AutoRun value as written, without expanding %VARIABLES%.
    /// </summary>
    // קוראת את ערך AutoRun כמו שהוא כתוב.
    [SupportedOSPlatform("windows")]
    private static string ReadAutoRun(RegistryKey key)
    {
        // DoNotExpandEnvironmentNames = להשאיר %APPDATA% כטקסט, כדי שנוכל למצוא את הפקודה שלנו בדיוק.
        object? value = key.GetValue(CmdAutoRunValue, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
        // אם זו מחרוזת — מחזירים אותה; אחרת — מחרוזת ריקה.
        return value as string ?? string.Empty;
    }

    /// <summary>
    /// Returns the AutoRun text without our command and the "&amp;" that joined it to other commands.
    /// </summary>
    // מסירה את הפקודה שלנו מטקסט ה־AutoRun, יחד עם ה־" & " שחיבר אותה לפקודות אחרות.
    private static string RemoveCmdCommand(string autoRun)
    {
        // מתחילים מהטקסט...
        return autoRun
            // ...מסירים " & פקודה" (כשהיא אחרי פקודה אחרת)...
            .Replace(" & " + CmdAutoRunCommand, string.Empty)
            // ...מסירים "פקודה & " (כשהיא לפני פקודה אחרת)...
            .Replace(CmdAutoRunCommand + " & ", string.Empty)
            // ...מסירים את הפקודה כשהיא לבד...
            .Replace(CmdAutoRunCommand, string.Empty)
            // ...ומנקים רווחים בקצוות.
            .Trim();
    }
}
