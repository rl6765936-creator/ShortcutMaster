# ShortcutMaster

כלי CLI ב־C# שמאפשר לשמור פעם אחת כינויים קצרים לפקודות, תוכנות, תיקיות ואתרים — ולהפעיל אותם בכינוי בלבד מכל טרמינל במחשב: PowerShell, CMD, Git Bash, Bash, Zsh והטרמינל של VS Code.

```
shortcut add gs "git status"
gs
```

---

## התקנה מהירה ב־Windows עם shortcut.exe (בלי .NET)

1. מורידים את **`shortcut.exe`** מעמוד ה־[Releases](https://github.com/rl6765936-creator/ShortcutMaster/releases/latest).
2. מריצים פעם אחת ב־PowerShell. הפקודות מעבירות את הקובץ לתיקייה קבועה ומוסיפות אותה ל־PATH של המשתמש, בלי הרשאות מנהל:
```powershell
$dir = "$env:LOCALAPPDATA\ShortcutMaster\bin"
New-Item -ItemType Directory -Force $dir | Out-Null
Move-Item -Force "$HOME\Downloads\shortcut.exe" "$dir\shortcut.exe"
Unblock-File "$dir\shortcut.exe"
$path = [Environment]::GetEnvironmentVariable('Path', 'User')
if ($path -notlike "*$dir*") { [Environment]::SetEnvironmentVariable('Path', "$path;$dir", 'User') }
```
3. פותחים טרמינל **חדש** ומריצים פעם אחת:
```
shortcut install
```

> הקובץ לא חתום דיגיטלית, ולכן Windows עלול להציג אזהרה. כדי לוודא שהקובץ תקין, משווים אותו לקובץ `SHA256SUMS.txt` שבאותו Release.

---

## התקנה מקוד המקור (Windows, macOS, Linux)

### 1. התקנת .NET 8 SDK
מורידים ומתקינים מהאתר הרשמי של מיקרוסופט: https://dotnet.microsoft.com/download
בדיקה שההתקנה הצליחה:
```
dotnet --version
```

### 2. בניית הכלי והתקנתו כפקודה גלובלית
מתוך תיקיית הפרויקט (התיקייה שבה נמצא `ShortcutMaster.csproj`):
```
dotnet pack -c Release
dotnet tool install --global --add-source ./nupkg ShortcutMaster
```
מעכשיו הפקודה `shortcut` זמינה מכל תיקייה. אם היא לא מזוהה — סוגרים ופותחים את הטרמינל מחדש.

> ב־Linux/macOS ייתכן שצריך להוסיף את `~/.dotnet/tools` ל־PATH. ההודעה של `dotnet tool install` תציג את השורה המדויקת.

### 3. חיבור הכינויים לכל הטרמינלים
```
shortcut install
```
הפקודה מוסיפה שורת טעינה אחת לכל טרמינל:
- **PowerShell** — לקובץ `profile.ps1` (גם Windows PowerShell וגם PowerShell 7, כולל בתוך VS Code).
- **CMD** — להגדרת `AutoRun` ברישום של המשתמש (ללא הרשאות מנהל).
- **Git Bash / Bash** — לקובץ `~/.bashrc`.
- **Zsh** — לקובץ `~/.zshrc` (ב־Linux/macOS, אם Zsh מותקן).

התוכן הקיים בקבצים האלה נשמר; ShortcutMaster מוסיף רק בלוק מסומן משלו.

### 4. פותחים טרמינל חדש — וזהו.

> **PowerShell ב־Windows:** אם מופיעה הודעה שהרצת סקריפטים חסומה, מריצים פעם אחת ב־PowerShell:
> ```
> Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
> ```

---

## שימוש

| פקודה | מה היא עושה |
|---|---|
| `shortcut add <שם> "<פעולה>"` | יצירת קיצור חדש |
| `shortcut update <שם> "<פעולה>"` | החלפת מה שקיצור קיים עושה |
| `shortcut remove <שם>` | מחיקת קיצור |
| `shortcut list` | הצגת כל הקיצורים |
| `shortcut run <שם> [ארגומנטים]` | הרצת קיצור (גם בלי install) |
| `shortcut install` | חיבור הכינויים לכל הטרמינלים |
| `shortcut uninstall` | ניתוק מהטרמינלים (הקיצורים נשמרים) |
| `shortcut --help` | עזרה; גם `shortcut add --help` וכו' |

**חשוב:** את הפעולה כותבים תמיד בתוך מרכאות — `"git status"`.

### דוגמאות

```
# פקודת טרמינל
shortcut add gs "git status"
shortcut add dev "npm run dev"

# אתר — מזוהה אוטומטית ונפתח בדפדפן
shortcut add github "https://github.com"

# תיקייה קיימת — נפתחת בסייר הקבצים
shortcut add docs "C:\Users\me\Documents"

# תוכנה לפי נתיב מלא
shortcut add np "C:\Windows\notepad.exe"

# מעבר תיקייה בטרמינל עצמו (cd), ואחריו פעולות נוספות
shortcut add work --type cd "C:\Projects\MyApp" --then "code ."

# כמה פעולות ברצף
shortcut add start "git pull" --then "npm install" --then "npm run dev"
```

### העברת ארגומנטים
מה שכותבים אחרי הכינוי מתווסף לפעולה האחרונה:
```
gs --short          ← מריץ: git status --short
```
ב־`shortcut run` מוסיפים `--` לפני ארגומנטים שמתחילים במקף:
```
shortcut run gs -- --short
```

### סוגי פעולות
הסוג מזוהה אוטומטית. אפשר לכפות סוג לפעולה הראשונה עם `--type` (או `-t`):

| סוג | משמעות |
|---|---|
| `command` | פקודת טרמינל (ברירת המחדל) |
| `url` | כתובת אתר, נפתחת בדפדפן |
| `folder` | תיקייה, נפתחת בסייר הקבצים |
| `app` | קובץ תוכנה |
| `cd` | מעבר תיקייה בטרמינל עצמו — רק כפעולה ראשונה |

### הגנות מובנות
- **שם תפוס:** אם השם כבר קיים כפקודה במחשב (למשל `git` או `dir`), הכלי מסרב. אפשר לעקוף עם `--force`.
- **שמות שמורים:** אי אפשר לקרוא לקיצור `add`, `list`, `run` וכו'.
- **קיצור לא מהימן:** קיצור שנוסף ידנית לקובץ ה־JSON ולא דרך הכלי יציג מה הוא עומד להריץ, ויבקש אישור לפני ההרצה הראשונה.
- **שגיאה עוצרת רצף:** אם פעולה בקיצור מרובה־פעולות נכשלת, הפעולות שאחריה לא ירוצו.

---

## איפה נשמר המידע

| מערכת | תיקייה |
|---|---|
| Windows | `%APPDATA%\ShortcutMaster` |
| Linux / macOS | `~/.config/ShortcutMaster` |

בתיקייה:
- `shortcuts.json` — הקיצורים שלך (זה הקובץ החשוב לגיבוי).
- `aliases.ps1`, `aliases.doskey`, `aliases.sh` — קבצים שנוצרים אוטומטית לכל טרמינל. לא לערוך ידנית.

---

## עדכון גרסה

משנים את `<Version>` בקובץ `ShortcutMaster.csproj` (למשל ל־`1.0.1`), ואז:
```
dotnet pack -c Release
dotnet tool update --global --add-source ./nupkg ShortcutMaster
```

## פרסום גרסה חדשה של shortcut.exe

GitHub בונה את `shortcut.exe` ובודק אותו אוטומטית. כדי לפרסם גרסה:
```powershell
git tag v1.0.0
git push origin v1.0.0
```
אפשר גם להפעיל את הבנייה מהלשונית **Actions** ← **Release Windows exe** ← **Run workflow**.

## הסרה מלאה
```
shortcut uninstall
dotnet tool uninstall --global ShortcutMaster
```
הקיצורים עצמם נשארים בתיקיית הנתונים, ואפשר למחוק אותה ידנית.

---

## מגבלות ידועות
- קיצור חדש זמין בכינוי רק **בחלון טרמינל חדש**. בחלון הפתוח אפשר להשתמש ב־`shortcut run <שם>`.
- ב־CMD, מעבר לתיקייה (`cd`) ששמה בעברית עלול לא לעבוד. ב־PowerShell וב־Git Bash זה עובד.
- ב־macOS עם Bash (לא Zsh), Bash קורא את `~/.bash_profile` ולא את `~/.bashrc`. ב־macOS ברירת המחדל היא Zsh, שנתמך במלואו.
- כינוי בטרמינל מפעיל את תוכנית ה־.NET, ולכן לוקח לו בערך עשירית שנייה להתחיל.

---

## מבנה הפרויקט

```
ShortcutMaster/
├── ShortcutMaster.csproj        הגדרות הפרויקט וההפצה כ־dotnet tool
├── Program.cs                   הגדרת הפקודות (System.CommandLine) וניתוב
├── Models/
│   └── Shortcut.cs              המודלים: קיצור, פעולה, סוגי פעולות
├── Services/                    מנוע הקיצורים — לא תלוי בטרמינל
│   ├── AppPaths.cs              כל נתיבי הקבצים
│   ├── CommandLocator.cs        בדיקה אם שם כבר תפוס כפקודה
│   ├── ShortcutService.cs       הוספה, עדכון, מחיקה, שמירה ב־JSON
│   └── ShortcutRunner.cs        הרצת הפעולות
└── Shell/                       שכבת האינטגרציה עם הטרמינלים
    ├── AliasScriptWriter.cs     יצירת קבצי הכינויים לכל טרמינל
    └── ShellInstaller.cs        חיבור הקבצים ל־PowerShell, CMD, Bash, Zsh
```
