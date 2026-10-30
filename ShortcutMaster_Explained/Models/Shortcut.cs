// =====================================================================
// Models/Shortcut.cs — עותק עם הסברים
// תפקיד הקובץ: להגדיר איך נראה "קיצור" ואיך נראית "פעולה" בתוך קיצור.
// הערה כללית: סוגריים מסולסלים { } פותחים וסוגרים בלוק (גוף של מחלקה,
// פונקציה או תנאי), ולא נכתב עליהם הסבר נפרד בכל פעם.
// הערות /// <summary> הן התיעוד באנגלית שמופיע ב־IntelliSense.
// =====================================================================

// הקובץ שייך למרחב השמות של המודלים.
namespace ShortcutMaster.Models;

/// <summary>
/// Represents a single user-defined shortcut: a name and one or more actions that run in order.
/// </summary>
// מחלקה שמייצגת קיצור אחד: שם, ורשימת פעולות שרצות לפי הסדר.
public class Shortcut
{
    // שם הקיצור שמקלידים (למשל gs). ערך התחלתי: מחרוזת ריקה.
    public string Name { get; set; } = string.Empty;

    // רשימת הפעולות. כך קיצור אחד יכול להריץ כמה דברים ברצף (שלב 4).
    public List<ShortcutAction> Actions { get; set; } = new();

    // האם הקיצור מהימן: true אם נוצר דרך הכלי או שהמשתמש אישר אותו.
    // קיצור שנכתב ידנית לקובץ ה־JSON יגיע כ־false ויבקש אישור לפני הרצה.
    public bool Trusted { get; set; }

    /// <summary>
    /// Builds a readable one-line description of all the actions.
    /// </summary>
    // מחזירה תיאור טקסטואלי של כל הפעולות, לצורך הצגה למשתמש.
    public string Describe()
    {
        // לוקחים את התיאור של כל פעולה ומחברים אותם עם חץ ביניהם: "git pull  ->  npm install".
        return string.Join("  ->  ", Actions.Select(action => action.Describe()));
    }
}

/// <summary>
/// One step of a shortcut: what kind of thing to run, its target and its arguments.
/// </summary>
// מחלקה שמייצגת פעולה אחת בתוך קיצור.
public class ShortcutAction
{
    // סוג הפעולה: command / app / folder / url / cd. ברירת המחדל: פקודת טרמינל.
    public string Type { get; set; } = ShortcutTypes.Command;

    // היעד: שם פקודה (git), נתיב לתוכנה או תיקייה, או כתובת אתר.
    public string Target { get; set; } = string.Empty;

    // הארגומנטים שנשלחים ליעד (למשל ["status"]).
    public List<string> Arguments { get; set; } = new();

    /// <summary>
    /// Returns true if this action has the given type, ignoring upper/lower case.
    /// </summary>
    // בודקת אם הפעולה מסוג מסוים. כך "URL" ו־"url" ייחשבו אותו דבר.
    public bool Is(string type)
    {
        // השוואת מחרוזות שמתעלמת מאותיות גדולות/קטנות.
        return string.Equals(Type, type, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds a readable description of this action.
    /// </summary>
    // מחזירה תיאור של פעולה אחת.
    public string Describe()
    {
        // אם אין ארגומנטים — רק היעד; אחרת — היעד, רווח, והארגומנטים מחוברים ברווחים.
        string text = Arguments.Count == 0 ? Target : Target + " " + string.Join(" ", Arguments);
        // בפעולת cd מוסיפים "cd " לפני הנתיב, כדי שיהיה ברור מה היא עושה.
        return Is(ShortcutTypes.Cd) ? "cd " + text : text;
    }
}

/// <summary>
/// Holds the supported action type names as constants.
/// </summary>
// מחלקה סטטית שמרכזת את שמות הסוגים במקום אחד, כדי לא לטעות באיות בשאר הקוד.
public static class ShortcutTypes
{
    // פקודת טרמינל.
    public const string Command = "command";
    // קובץ תוכנה.
    public const string App = "app";
    // תיקייה שנפתחת בסייר הקבצים.
    public const string Folder = "folder";
    // אתר שנפתח בדפדפן.
    public const string Url = "url";
    // מעבר תיקייה בתוך הטרמינל עצמו.
    public const string Cd = "cd";

    // מערך של כל הסוגים — משמש לבדיקה שהמשתמש הקליד סוג חוקי ב־--type.
    public static readonly string[] All = { Command, App, Folder, Url, Cd };
}
