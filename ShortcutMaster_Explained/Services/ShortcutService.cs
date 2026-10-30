// =====================================================================
// Services/ShortcutService.cs — עותק עם הסברים
// תפקיד הקובץ: ניהול הקיצורים — הוספה, עדכון, מחיקה, הצגה, ושמירה ל־JSON.
// הקובץ לא יודע כלום על הטרמינלים — זו ההפרדה שנדרשה במסמך הפרויקט.
// סוגריים מסולסלים { } פותחים וסוגרים בלוק, ולא הוסבר עליהם בנפרד.
// =====================================================================

// ספרייה שמכילה StringBuilder — לבניית מחרוזת תו אחר תו.
using System.Text;
// ספרייה לשמירה וטעינה של JSON.
using System.Text.Json;
// ספרייה לביטויים רגולריים (Regex) — לבדיקת תקינות שם.
using System.Text.RegularExpressions;
// מאפשר להשתמש ב־Shortcut, ShortcutAction ו־ShortcutTypes.
using ShortcutMaster.Models;

// הקובץ שייך למרחב השמות של השירותים.
namespace ShortcutMaster.Services;

/// <summary>
/// Manages shortcuts: adding, updating, removing, listing and saving them to JSON.
/// </summary>
// המחלקה שמנהלת את הקיצורים.
public class ShortcutService
{
    // מילים שאסור להשתמש בהן כשם קיצור, כי הן פקודות של הכלי עצמו.
    private static readonly string[] ReservedNames =
    {
        // shortcut עצמו, וכל הפקודות שלו.
        "shortcut", "add", "update", "remove", "list", "run", "install", "uninstall", "help"
    };

    // תבנית לשם תקין: ^ התחלה, רק אותיות אנגליות, ספרות, _ ו־-, + לפחות תו אחד, $ סוף.
    // ההגבלה חשובה גם לאבטחה: השם נכתב לתוך קבצי הסקריפט של הטרמינלים.
    private static readonly Regex ValidName = new("^[A-Za-z0-9_-]+$");

    // הגדרות לשמירת JSON (נוצרות פעם אחת).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // JSON מסודר עם ירידות שורה — קריא לבני אדם.
        WriteIndented = true,
        // שמות שדות באות קטנה בהתחלה (Name → name).
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // המילון שמחזיק את כל הקיצורים בזיכרון: מפתח = שם, ערך = הקיצור.
    private readonly Dictionary<string, Shortcut> _shortcuts;

    /// <summary>
    /// Creates the service and loads the user's shortcuts from disk.
    /// </summary>
    // בנאי — רץ כשכותבים new ShortcutService().
    public ShortcutService()
    {
        // טוענים את הקיצורים מהקובץ.
        _shortcuts = Load();
    }

    /// <summary>
    /// Adds a new shortcut and saves it. Refuses names that already run a command unless force is true.
    /// </summary>
    // מוסיפה קיצור חדש. force = האם המשתמש ביקש לעקוף התנגשות עם פקודה קיימת.
    public void Add(string name, List<ShortcutAction> actions, bool force)
    {
        // בודקים שהשם תקין ולא שמור.
        ValidateName(name);

        // אם כבר יש קיצור בשם הזה...
        if (_shortcuts.ContainsKey(name))
            // ...שגיאה, ומפנים לפקודת update.
            throw new InvalidOperationException($"Shortcut '{name}' already exists. Use 'shortcut update' to change it.");

        // אם לא ביקשו force, והשם כבר קיים כפקודה במחשב (למשל git)...
        if (!force && CommandLocator.Exists(name))
            // ...זורקים שגיאה עם הסבר. הודעה ארוכה, לכן מפוצלת לשתי שורות.
            throw new InvalidOperationException(
                // המשך ההודעה.
                $"'{name}' is already a command on this computer. Choose another name, or add --force to override it.");

        // יוצרים קיצור חדש — מהימן, כי נוצר דרך הכלי — ומכניסים למילון.
        _shortcuts[name] = new Shortcut { Name = name, Actions = actions, Trusted = true };
        // שומרים לקובץ.
        Save();
    }

    /// <summary>
    /// Replaces the actions of an existing shortcut and saves it.
    /// </summary>
    // מעדכנת קיצור קיים (שלב 4).
    public void Update(string name, List<ShortcutAction> actions)
    {
        // מוצאים את הקיצור. אם לא קיים — Get זורקת שגיאה.
        Shortcut shortcut = Get(name);
        // מחליפים את הפעולות.
        shortcut.Actions = actions;
        // המשתמש עדכן בעצמו — לכן מהימן.
        shortcut.Trusted = true;
        // שומרים.
        Save();
    }

    /// <summary>
    /// Removes a shortcut by name and saves the change.
    /// </summary>
    // מוחקת קיצור.
    public void Remove(string name)
    {
        // Remove מחזירה false אם השם לא היה קיים...
        if (!_shortcuts.Remove(name))
            // ...ואז שגיאה.
            throw new KeyNotFoundException($"Shortcut '{name}' was not found.");

        // שומרים את המצב החדש.
        Save();
    }

    /// <summary>
    /// Returns all shortcuts sorted by name.
    /// </summary>
    // מחזירה את כל הקיצורים כרשימה לקריאה בלבד.
    public IReadOnlyList<Shortcut> GetAll()
    {
        // לוקחים את כל הערכים מהמילון...
        return _shortcuts.Values
            // ...ממיינים לפי שם, בלי הבדל בין אותיות גדולות לקטנות...
            .OrderBy(shortcut => shortcut.Name, StringComparer.OrdinalIgnoreCase)
            // ...ומחזירים כרשימה.
            .ToList();
    }

    /// <summary>
    /// Finds a shortcut by name or throws if it does not exist.
    /// </summary>
    // מחפשת קיצור לפי שם.
    public Shortcut Get(string name)
    {
        // TryGetValue מנסה למצוא, ואם הצליח שם את התוצאה במשתנה shortcut.
        if (!_shortcuts.TryGetValue(name, out Shortcut? shortcut))
            // לא נמצא — שגיאה עם רמז איך לראות את כל הקיצורים.
            throw new KeyNotFoundException($"Shortcut '{name}' was not found. See all shortcuts with: shortcut list");

        // מחזירים את הקיצור.
        return shortcut;
    }

    /// <summary>
    /// Marks a shortcut as approved by the user and saves it.
    /// </summary>
    // מסמנת קיצור כמהימן אחרי אישור המשתמש.
    public void MarkTrusted(Shortcut shortcut)
    {
        // משנים את השדה.
        shortcut.Trusted = true;
        // שומרים, כדי שלא נשאל שוב.
        Save();
    }

    /// <summary>
    /// Returns true if the name contains only letters, digits, '-' and '_'.
    /// </summary>
    // ציבורית, כי גם AliasScriptWriter משתמש בה כדי לא לכתוב לסקריפטים שם לא בטוח.
    public static bool IsValidName(string name)
    {
        // בודקים מול התבנית.
        return ValidName.IsMatch(name);
    }

    /// <summary>
    /// Turns the text the user typed into a list of actions: the first action plus any "--then" actions.
    /// The type of the first action can be forced; all others are detected automatically.
    /// </summary>
    // הופכת את מה שהמשתמש הקליד לרשימת פעולות.
    public static List<ShortcutAction> BuildActions(string firstAction, IEnumerable<string> nextActions, string? firstType)
    {
        // יוצרים רשימה שמתחילה בפעולה הראשונה (עם הסוג שנכפה ב־--type, אם יש).
        var actions = new List<ShortcutAction> { ParseAction(firstAction, firstType) };
        // מוסיפים את כל פעולות ה־--then, עם זיהוי סוג אוטומטי (null = אין סוג כפוי).
        actions.AddRange(nextActions.Select(text => ParseAction(text, null)));
        // מחזירים את הרשימה.
        return actions;
    }

    /// <summary>
    /// Loads shortcuts from the JSON file, or returns an empty collection if the file does not exist.
    /// </summary>
    // טוענת את הקיצורים מהקובץ.
    private static Dictionary<string, Shortcut> Load()
    {
        // אם הקובץ עוד לא קיים (שימוש ראשון)...
        if (!File.Exists(AppPaths.ShortcutsFile))
            // ...מילון ריק, שבו "GS" ו־"gs" נחשבים אותו שם.
            return new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase);

        // קוראים את תוכן הקובץ.
        string json = File.ReadAllText(AppPaths.ShortcutsFile);
        // ממירים את ה־JSON למילון. אם הקובץ פגום — תיזרק שגיאה, והקובץ לא יידרס.
        var data = JsonSerializer.Deserialize<Dictionary<string, Shortcut>>(json, JsonOptions);

        // יוצרים מילון חדש שמתעלם מאותיות גדולות/קטנות ומעתיקים אליו את מה שנטען.
        return new Dictionary<string, Shortcut>(
            // אם הקובץ הכיל null — מילון ריק במקום.
            data ?? new Dictionary<string, Shortcut>(),
            // השוואת מפתחות ללא תלות באותיות גדולות/קטנות.
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes all shortcuts to the JSON file, creating the folder if needed.
    /// </summary>
    // שומרת את כל הקיצורים לקובץ.
    private void Save()
    {
        // יוצרים את התיקייה (אם כבר קיימת — לא קורה כלום).
        Directory.CreateDirectory(AppPaths.DataFolder);
        // ממירים את המילון ל־JSON.
        string json = JsonSerializer.Serialize(_shortcuts, JsonOptions);
        // כותבים לקובץ.
        File.WriteAllText(AppPaths.ShortcutsFile, json);
    }

    /// <summary>
    /// Checks that a shortcut name is valid and is not a reserved word.
    /// </summary>
    // בודקת תקינות שם.
    private static void ValidateName(string name)
    {
        // אם השם לא תואם לתבנית...
        if (!IsValidName(name))
            // ...שגיאה.
            throw new ArgumentException("Name may contain only English letters, digits, '-' and '_'.");

        // אם השם הוא מילה שמורה...
        if (ReservedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            // ...שגיאה.
            throw new ArgumentException($"'{name}' is a reserved word and cannot be used as a shortcut name.");
    }

    /// <summary>
    /// Turns one piece of text into an action, detecting or applying its type.
    /// </summary>
    // הופכת טקסט אחד לפעולה אחת.
    private static ShortcutAction ParseAction(string text, string? forcedType)
    {
        // מפרקים את הטקסט ליעד וארגומנטים.
        List<string> parts = ParseTarget(text);

        // אם לא נשאר כלום...
        if (parts.Count == 0)
            // ...שגיאה.
            throw new ArgumentException("An action cannot be empty.");

        // הסוג: אם נכפה — משתמשים בו (באותיות קטנות); אחרת (??) — מזהים אוטומטית.
        string type = forcedType?.ToLowerInvariant() ?? DetectType(parts[0]);
        // החלק הראשון הוא היעד.
        string target = parts[0];

        // בתיקייה ובמעבר תיקייה...
        if (type == ShortcutTypes.Folder || type == ShortcutTypes.Cd)
            // ...הופכים נתיב יחסי (כמו ".") לנתיב מלא, כדי שיעבוד מכל מקום.
            target = Path.GetFullPath(target);

        // במעבר תיקייה, אם התיקייה לא קיימת...
        if (type == ShortcutTypes.Cd && !Directory.Exists(target))
            // ...שגיאה כבר עכשיו, ולא בזמן ההרצה.
            throw new DirectoryNotFoundException($"Folder '{target}' does not exist.");

        // אם במעבר תיקייה יש יותר מחלק אחד (למשל רווח בנתיב לא קיים)...
        if (type == ShortcutTypes.Cd && parts.Count > 1)
            // ...שגיאה עם הסבר.
            throw new ArgumentException("A cd action takes only a folder path. Put other steps in --then.");

        // יוצרים ומחזירים את הפעולה.
        return new ShortcutAction
        {
            // הסוג.
            Type = type,
            // היעד.
            Target = target,
            // כל השאר (Skip(1) = בלי הראשון) הם ארגומנטים.
            Arguments = parts.Skip(1).ToList()
        };
    }

    /// <summary>
    /// Splits the user's text into a target and arguments.
    /// An existing path or a URL is kept whole, even if it contains spaces.
    /// </summary>
    // מפרקת טקסט ליעד וארגומנטים.
    private static List<string> ParseTarget(string text)
    {
        // מסירים רווחים בקצוות, ומחליפים ~ בתיקיית הבית.
        string trimmed = ExpandHomeFolder(text.Trim());

        // אם כל הטקסט הוא אתר, תיקייה קיימת או קובץ קיים (למשל "C:\My Projects")...
        if (IsUrl(trimmed) || Directory.Exists(trimmed) || File.Exists(trimmed))
            // ...שומרים אותו כיחידה אחת, בלי לפרק ברווחים.
            return new List<string> { trimmed };

        // אחרת — מפרקים לפי רווחים.
        return SplitCommandLine(trimmed);
    }

    /// <summary>
    /// Replaces a leading "~" with the user's home folder, as Bash does.
    /// </summary>
    // מחליפה ~ בתחילת נתיב בתיקיית הבית (כי מתוך מרכאות הטרמינל לא עושה את זה בעצמו).
    private static string ExpandHomeFolder(string text)
    {
        // אם הטקסט לא מתחיל ב־~ לבד, ב־~/ או ב־~\ ...
        if (text != "~" && !text.StartsWith("~/") && !text.StartsWith("~\\"))
            // ...מחזירים אותו כמו שהוא.
            return text;

        // תיקיית הבית של המשתמש.
        string home = AppPaths.GetFolder(Environment.SpecialFolder.UserProfile);
        // תיקיית הבית + כל מה שאחרי ה־~ (Substring(1) = מהתו השני והלאה).
        return home + text.Substring(1);
    }

    /// <summary>
    /// Decides the action type from its target: URL, folder, app file or terminal command.
    /// </summary>
    // מזהה את סוג הפעולה לפי היעד.
    private static string DetectType(string target)
    {
        // כתובת אתר?
        if (IsUrl(target))
            // סוג url.
            return ShortcutTypes.Url;

        // תיקייה קיימת?
        if (Directory.Exists(target))
            // סוג folder.
            return ShortcutTypes.Folder;

        // קובץ קיים?
        if (File.Exists(target))
            // סוג app.
            return ShortcutTypes.App;

        // אחרת — פקודת טרמינל.
        return ShortcutTypes.Command;
    }

    /// <summary>
    /// Returns true if the text is an absolute http or https address.
    /// </summary>
    // בודקת אם הטקסט הוא כתובת אתר.
    private static bool IsUrl(string text)
    {
        // מנסים לפרש כ־Uri מלא...
        return Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            // ...ובודקים שהפרוטוקול הוא http או https.
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Splits a command line by spaces, keeping text inside double quotes together.
    /// </summary>
    // מפרקת שורה לחלקים לפי רווחים; טקסט בתוך מרכאות נשאר חלק אחד.
    private static List<string> SplitCommandLine(string commandLine)
    {
        // רשימת החלקים.
        var parts = new List<string>();
        // החלק שנבנה כרגע.
        var current = new StringBuilder();
        // האם אנחנו בתוך מרכאות.
        bool inQuotes = false;

        // עוברים על כל תו.
        foreach (char c in commandLine)
        {
            // מרכאה...
            if (c == '"')
                // ...הופכת את המצב (נכנסים או יוצאים). המרכאה עצמה לא נשמרת.
                inQuotes = !inQuotes;
            // רווח מחוץ למרכאות...
            else if (char.IsWhiteSpace(c) && !inQuotes)
                // ...סוגר את החלק הנוכחי.
                AddPart(parts, current);
            // כל תו אחר...
            else
                // ...מצטרף לחלק הנוכחי.
                current.Append(c);
        }

        // מוסיפים את החלק האחרון.
        AddPart(parts, current);
        // מחזירים.
        return parts;
    }

    /// <summary>
    /// Moves the collected characters into the parts list, if there are any.
    /// </summary>
    // מעבירה את מה שנאסף לרשימה ומאפסת.
    private static void AddPart(List<string> parts, StringBuilder current)
    {
        // אם לא נאסף כלום (למשל שני רווחים ברצף)...
        if (current.Length == 0)
            // ...אין מה להוסיף.
            return;

        // מוסיפים לרשימה.
        parts.Add(current.ToString());
        // מרוקנים לקראת החלק הבא.
        current.Clear();
    }
}
