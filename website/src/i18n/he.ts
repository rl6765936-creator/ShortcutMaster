// Hebrew text. Must have exactly the same shape as en.ts.
import type { Dict } from './en.ts';

export const he: Dict = {
  a11y: {
    skip: 'דלג לתוכן',
    menu: 'תפריט',
    language: 'שפה',
    copy: 'העתק',
    copied: 'הועתק',
  },
  nav: {
    features: 'יכולות',
    demo: 'נסו בדפדפן',
    install: 'התקנה',
    faq: 'שאלות',
    github: 'GitHub',
  },
  hero: {
    titleA: 'הפקודות שלך,',
    titleB: 'במרחק מילה',
    titleC: 'אחת.',
    lead:
      'ShortcutMaster הוא כלי שורת פקודה קטן. נותנים פעם אחת שם קצר לכל פקודה, תוכנה, תיקייה או אתר — ומאז מקלידים רק את השם ב־PowerShell, ב־CMD, ב־Git Bash, ב־Bash או ב־Zsh, והוא רץ.',
    primary: 'להורדה מ־GitHub',
    secondary: 'נסו בדפדפן',
    ticks: ['חינמי וקוד פתוח', 'בכל טרמינל שאתם עובדים בו', 'שומרים פעם אחת, עובד בכל מקום'],
    artLabel: 'הקיצורים נשמרים בקובץ אחד ומגיעים לכל הטרמינלים',
  },
  features: {
    title: 'מה הוא עושה',
    lead: 'כל כרטיס כאן הוא אפשרות אמיתית של הפקודה המותקנת, shortcut. אפשר לשלב ביניהן בחופשיות.',
    anything: {
      title: 'שם קצר לכל דבר',
      body: 'הסוג מזוהה לבד: פקודת טרמינל, תוכנה, תיקייה, אתר — או תיקייה שהטרמינל עצמו צריך לעבור אליה.',
      types: {
        command: 'פקודה',
        app: 'תוכנה',
        folder: 'תיקייה',
        url: 'אתר',
        cd: 'מעבר תיקייה',
      },
    },
    shells: {
      title: 'עובד בכל טרמינל',
      body: 'פקודת ⁦shortcut install⁩ אחת מחברת את PowerShell, CMD, Git Bash, Bash ו־Zsh — כולל הטרמינל שבתוך VS Code.',
    },
    chain: {
      title: 'כמה שלבים, מילה אחת',
      body: 'מוסיפים ⁦--then⁩ כדי להריץ עוד פעולות לפי הסדר. אם שלב נכשל — השאר לא רצים.',
    },
    args: {
      title: 'ארגומנטים עוברים הלאה',
      body: 'מה שכותבים אחרי השם מתווסף לפעולה האחרונה.',
    },
    cd: {
      title: 'באמת עובר תיקייה',
      body: 'תוכנה לא יכולה להזיז את הטרמינל שהפעיל אותה — לכן קיצור cd מורץ על ידי הטרמינל עצמו.',
    },
    safe: {
      title: 'בטוח מההתחלה',
      body: 'שם שכבר מריץ משהו (git, dir, cd…) נדחה, אלא אם מוסיפים ⁦--force⁩. קיצור שנוסף לקובץ ידנית מבקש אישור לפני ההרצה הראשונה.',
      taken: "'git' is already a command on this computer.",
      ask: 'Run it and trust it from now on? (y/N)',
    },
    saved: {
      title: 'נשמר פעם אחת, לתמיד',
      body: 'הכול נשמר בקובץ JSON קטן אחד בתיקייה האישית שלך — קל לגבות וקל לקרוא.',
    },
    notesTitle: 'כדאי לדעת',
    notes: [
      'קיצור חדש עובד לפי שם בחלונות טרמינל חדשים. בחלון שכבר פתוח — משתמשים ב־shortcut run.',
      'את הפעולה כותבים במרכאות: ⁦shortcut add gs "git status"⁩.',
      'קובצי הפרופיל שלך נשמרים כמו שהם — ShortcutMaster מוסיף רק בלוק מסומן אחד, ש־uninstall מסיר.',
      'לא צריך הרשאות מנהל, גם לא בשביל CMD.',
    ],
  },
  demo: {
    badge: 'סימולציה בדפדפן',
    title: 'נסו כאן ועכשיו',
    lead:
      'מחשב Windows מדומה עם שלושה טרמינלים, שרץ בתוך הדפדפן. שום דבר לא נוגע במחשב שלכם — הכלי המותקן עובד בדיוק כך על המחשב האמיתי.',
    shell: 'טרמינל',
    presetsLabel: 'דוגמאות מהירות',
    presets: {
      git: 'Git status',
      web: 'פתיחת אתר',
      work: 'קפיצה לפרויקט',
      chain: 'פתיחת יום עבודה',
    },
    builder: {
      name: 'שם',
      nameHint: 'מה שתקלידו. אותיות באנגלית, ספרות, - ו־_.',
      action: 'פעולה',
      actionHint: 'פקודה, נתיב לתוכנה, תיקייה או כתובת אתר.',
      type: 'סוג',
      typeHint: 'השאירו על auto, אלא אם השלב הראשון צריך להעביר את הטרמינל לתיקייה (cd).',
      auto: 'auto',
      then: 'ואז',
      thenHint: 'פעולות נוספות, שירוצו לפי הסדר אחרי הראשונה.',
      addStep: 'הוספת שלב',
      removeStep: 'הסרת שלב',
      force: 'לאפשר שם שכבר קיים כפקודה',
    },
    command: 'פקודה',
    add: 'הוספה',
    tabs: {
      terminal: 'טרמינל',
      json: 'shortcuts.json',
      alias: 'קובץ הכינויים',
    },
    newWindow: 'חלון חדש',
    reset: 'איפוס',
    inputLabel: 'הקלידו פקודה ולחצו Enter',
    placeholder: 'הקלידו כאן — נסו: shortcut --help',
    welcome: 'טרמינל מדומה. בנו קיצור בצד ולחצו "הוספה", או הקלידו פקודות כאן.',
    steps: ['מוסיפים קיצור', 'מריצים shortcut install', 'פותחים חלון חדש', 'מקלידים את השם'],
    emptyJson: '{}  (עדיין אין קיצורים)',
    hints: {
      'hint.newWindow': 'טרמינל טוען את הקיצורים כשהוא נפתח — לחצו "חלון חדש" ואז הקלידו את השם.',
      'hint.install': 'הקיצור קיים, אבל הטרמינל עוד לא מכיר אותו. הריצו ⁦shortcut install⁩ ואז פתחו חלון חדש.',
      'hint.quotes': 'שימו את הפעולה במרכאות, למשל: ⁦shortcut add gs "git status"⁩',
    },
    footnote: 'פלט מדומה. התוצאות של git, npm וכו׳ הן דוגמאות; ההודעות של ShortcutMaster זהות לכלי האמיתי.',
  },
  install: {
    title: 'התקנה והפעלה',
    lead: 'ההתקנה לוקחת כמה דקות, דורשת את ⁦.NET 8 SDK⁩ החינמי, ולא דורשת הרשאות מנהל.',
    cardTitle: 'ShortcutMaster',
    cardBody: 'Windows, macOS ו־Linux. מותקן ככלי ⁦.NET⁩ גלובלי, כך ש־shortcut עובד מכל תיקייה.',
    source: 'לקוד ב־GitHub',
    zip: 'הורדת ZIP',
    readme: 'README',
    dotnet: 'להורדת ⁦.NET 8⁩',
    stepsTitle: 'ארבעה שלבים',
    steps: [
      { title: 'מורידים את הקוד', body: 'משכפלים את המאגר, או מורידים את ה־ZIP ומחלצים.' },
      { title: 'בונים ומתקינים את הפקודה', body: 'מתוך התיקייה ShortcutMaster. מעכשיו shortcut עובד בכל תיקייה.' },
      { title: 'מחברים את הטרמינלים', body: 'פעם אחת. מוסיף טעינה ל־PowerShell, CMD, Bash ו־Zsh.' },
      { title: 'מוסיפים קיצור ומשתמשים בו', body: 'פותחים חלון טרמינל חדש ומקלידים רק את השם.' },
    ],
    examplesTitle: 'עוד דוגמאות',
    examples: [
      { label: 'פקודת טרמינל', cmd: 'shortcut add gs "git status"' },
      { label: 'אתר', cmd: 'shortcut add github "https://github.com"' },
      { label: 'מעבר לפרויקט ופתיחת VS Code', cmd: 'shortcut add work --type cd "C:\\Projects\\MyApp" --then "code ."' },
      { label: 'כמה שלבים ברצף', cmd: 'shortcut add start "git pull" --then "npm install" --then "npm run dev"' },
      { label: 'שינוי מה שקיצור עושה', cmd: 'shortcut update gs "git status --short"' },
      { label: 'הצגת כל מה ששמרתם', cmd: 'shortcut list' },
    ],
    troubleTitle: 'פתרון בעיות',
    trouble: [
      {
        q: '“shortcut is not recognized…”',
        a: 'תיקיית הכלים הגלובליים עוד לא ב־PATH. סגרו ופתחו את הטרמינל מחדש. ב־Linux/macOS מוסיפים את ⁦~/.dotnet/tools⁩ ל־PATH — הודעת ההתקנה מציגה את השורה המדויקת.',
      },
      {
        q: 'PowerShell: “running scripts is disabled on this system”',
        a: 'במחשבים מסוימים PowerShell חוסם סקריפטים של פרופיל. מריצים פעם אחת: ⁦Set-ExecutionPolicy -Scope CurrentUser RemoteSigned⁩ — ופותחים חלון חדש.',
      },
      {
        q: 'הוספתי קיצור, אבל הקלדת השם לא עושה כלום',
        a: 'טרמינל קורא את הקיצורים כשהוא נפתח. פתחו חלון חדש — או השתמשו ב־⁦shortcut run <שם>⁩ בחלון הנוכחי. ודאו גם ש־⁦shortcut install⁩ הורץ פעם אחת.',
      },
      {
        q: '“… is already a command on this computer”',
        a: 'השם היה מסתיר פקודה קיימת. בחרו שם אחר, או הוסיפו ⁦--force⁩ אם באמת רוצים לעקוף אותה.',
      },
      {
        q: 'בהוספה: ⁦“Unrecognized command or argument …”⁩',
        a: 'הפעולה צריכה מרכאות: ⁦shortcut add gs "git status"⁩. בלעדיהן, status נקרא כארגומנט מיותר.',
      },
    ],
    uninstallTitle: 'הסרה',
    uninstallBody: '⁦shortcut uninstall⁩ מנתק את הטרמינלים; ⁦dotnet tool uninstall --global ShortcutMaster⁩ מסיר את הפקודה. קובץ הקיצורים נשאר עד שמוחקים אותו.',
  },
  faq: {
    title: 'שאלות ותשובות',
    items: [
      {
        q: 'אילו מערכות וטרמינלים נתמכים?',
        a: 'Windows, macOS ו־Linux עם ⁦.NET 8⁩. טרמינלים: Windows PowerShell 5.1, PowerShell 7, CMD, Git Bash, Bash ו־Zsh — וגם הטרמינל של VS Code, שמשתמש באחד מהם.',
      },
      {
        q: 'איך עובדת הקלדה של השם בלבד?',
        a: '⁦shortcut install⁩ מוסיף שורה אחת לקובץ ההפעלה של כל טרמינל (ולהגדרת AutoRun של CMD). השורה טוענת קובץ קטן שנוצר אוטומטית, עם פונקציה לכל קיצור — gs הופך לפונקציה שקוראת ל־⁦shortcut __run gs⁩.',
      },
      {
        q: 'איפה הקיצורים נשמרים?',
        a: 'בקובץ shortcuts.json — בתיקייה ⁦%APPDATA%\\ShortcutMaster⁩ ב־Windows, או ⁦~/.config/ShortcutMaster⁩ ב־Linux וב־macOS. כדי לשמור עליהם, מגבים את הקובץ הזה.',
      },
      {
        q: 'למה צריך חלון טרמינל חדש אחרי הוספת קיצור?',
        a: 'טרמינל טוען את הפונקציות שלו פעם אחת, כשהוא נפתח. חלונות שכבר פתוחים נשארים עם הרשימה הישנה. ⁦shortcut run <שם>⁩ עובד מיד בכל מקום.',
      },
      {
        q: 'קיצור יכול להעביר אותי לתיקייה אחרת?',
        a: 'כן. מוסיפים אותו עם ⁦--type cd⁩ ותיקייה. הפונקציה שנוצרת מעבירה את הטרמינל עצמו, ואז מריצה שם את שלבי ה־⁦--then⁩.',
      },
      {
        q: 'צריך הרשאות מנהל?',
        a: 'לא. הכול נכתב לפרופיל המשתמש שלך ולענף הרישום של המשתמש שלך.',
      },
      {
        q: 'הדמו באתר הוא הכלי האמיתי?',
        a: 'לא — זו סימולציה שרצה בדפדפן. היא משחזרת את ההודעות האמיתיות ואת הקבצים שהכלי כותב, אבל לא נוגעת במחשב שלכם.',
      },
      {
        q: 'איך מסירים הכול?',
        a: 'מריצים ⁦shortcut uninstall⁩, ואז ⁦dotnet tool uninstall --global ShortcutMaster⁩. אם רוצים למחוק גם את הקיצורים — מוחקים את תיקיית הנתונים ShortcutMaster.',
      },
    ],
  },
  footer: {
    tagline: 'מילה אחת. כל פקודה.',
    body: 'ShortcutMaster הוא תוכנה חינמית בקוד פתוח, שנכתבה ב־⁦C# / .NET 8⁩.',
    links: { github: 'GitHub', install: 'התקנה', faq: 'שאלות', readme: 'README' },
    langBtn: 'English',
  },
  fab: 'נסו את הדמו',
};
