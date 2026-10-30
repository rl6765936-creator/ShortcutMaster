using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShortcutMaster.Models;

namespace ShortcutMaster.Services;

/// <summary>
/// Manages shortcuts: adding, updating, removing, listing and saving them to JSON.
/// </summary>
public class ShortcutService
{
    private static readonly string[] ReservedNames =
    {
        "shortcut", "add", "update", "remove", "list", "run", "install", "uninstall", "help"
    };

    private static readonly Regex ValidName = new("^[A-Za-z0-9_-]+$");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Dictionary<string, Shortcut> _shortcuts;

    /// <summary>
    /// Creates the service and loads the user's shortcuts from disk.
    /// </summary>
    public ShortcutService()
    {
        _shortcuts = Load();
    }

    /// <summary>
    /// Adds a new shortcut and saves it. Refuses names that already run a command unless force is true.
    /// </summary>
    public void Add(string name, List<ShortcutAction> actions, bool force)
    {
        ValidateName(name);

        if (_shortcuts.ContainsKey(name))
            throw new InvalidOperationException($"Shortcut '{name}' already exists. Use 'shortcut update' to change it.");

        if (!force && CommandLocator.Exists(name))
            throw new InvalidOperationException(
                $"'{name}' is already a command on this computer. Choose another name, or add --force to override it.");

        _shortcuts[name] = new Shortcut { Name = name, Actions = actions, Trusted = true };
        Save();
    }

    /// <summary>
    /// Replaces the actions of an existing shortcut and saves it.
    /// </summary>
    public void Update(string name, List<ShortcutAction> actions)
    {
        Shortcut shortcut = Get(name);
        shortcut.Actions = actions;
        shortcut.Trusted = true;
        Save();
    }

    /// <summary>
    /// Removes a shortcut by name and saves the change.
    /// </summary>
    public void Remove(string name)
    {
        if (!_shortcuts.Remove(name))
            throw new KeyNotFoundException($"Shortcut '{name}' was not found.");

        Save();
    }

    /// <summary>
    /// Returns all shortcuts sorted by name.
    /// </summary>
    public IReadOnlyList<Shortcut> GetAll()
    {
        return _shortcuts.Values
            .OrderBy(shortcut => shortcut.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Finds a shortcut by name or throws if it does not exist.
    /// </summary>
    public Shortcut Get(string name)
    {
        if (!_shortcuts.TryGetValue(name, out Shortcut? shortcut))
            throw new KeyNotFoundException($"Shortcut '{name}' was not found. See all shortcuts with: shortcut list");

        return shortcut;
    }

    /// <summary>
    /// Marks a shortcut as approved by the user and saves it.
    /// </summary>
    public void MarkTrusted(Shortcut shortcut)
    {
        shortcut.Trusted = true;
        Save();
    }

    /// <summary>
    /// Returns true if the name contains only letters, digits, '-' and '_'.
    /// </summary>
    public static bool IsValidName(string name)
    {
        return ValidName.IsMatch(name);
    }

    /// <summary>
    /// Turns the text the user typed into a list of actions: the first action plus any "--then" actions.
    /// The type of the first action can be forced; all others are detected automatically.
    /// </summary>
    public static List<ShortcutAction> BuildActions(string firstAction, IEnumerable<string> nextActions, string? firstType)
    {
        var actions = new List<ShortcutAction> { ParseAction(firstAction, firstType) };
        actions.AddRange(nextActions.Select(text => ParseAction(text, null)));
        return actions;
    }

    /// <summary>
    /// Loads shortcuts from the JSON file, or returns an empty collection if the file does not exist.
    /// </summary>
    private static Dictionary<string, Shortcut> Load()
    {
        if (!File.Exists(AppPaths.ShortcutsFile))
            return new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase);

        string json = File.ReadAllText(AppPaths.ShortcutsFile);
        var data = JsonSerializer.Deserialize<Dictionary<string, Shortcut>>(json, JsonOptions);

        return new Dictionary<string, Shortcut>(
            data ?? new Dictionary<string, Shortcut>(),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes all shortcuts to the JSON file, creating the folder if needed.
    /// </summary>
    private void Save()
    {
        Directory.CreateDirectory(AppPaths.DataFolder);
        string json = JsonSerializer.Serialize(_shortcuts, JsonOptions);
        File.WriteAllText(AppPaths.ShortcutsFile, json);
    }

    /// <summary>
    /// Checks that a shortcut name is valid and is not a reserved word.
    /// </summary>
    private static void ValidateName(string name)
    {
        if (!IsValidName(name))
            throw new ArgumentException("Name may contain only English letters, digits, '-' and '_'.");

        if (ReservedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"'{name}' is a reserved word and cannot be used as a shortcut name.");
    }

    /// <summary>
    /// Turns one piece of text into an action, detecting or applying its type.
    /// </summary>
    private static ShortcutAction ParseAction(string text, string? forcedType)
    {
        List<string> parts = ParseTarget(text);

        if (parts.Count == 0)
            throw new ArgumentException("An action cannot be empty.");

        string type = forcedType?.ToLowerInvariant() ?? DetectType(parts[0]);
        string target = parts[0];

        if (type == ShortcutTypes.Folder || type == ShortcutTypes.Cd)
            target = Path.GetFullPath(target);

        if (type == ShortcutTypes.Cd && !Directory.Exists(target))
            throw new DirectoryNotFoundException($"Folder '{target}' does not exist.");

        if (type == ShortcutTypes.Cd && parts.Count > 1)
            throw new ArgumentException("A cd action takes only a folder path. Put other steps in --then.");

        return new ShortcutAction
        {
            Type = type,
            Target = target,
            Arguments = parts.Skip(1).ToList()
        };
    }

    /// <summary>
    /// Splits the user's text into a target and arguments.
    /// An existing path or a URL is kept whole, even if it contains spaces.
    /// </summary>
    private static List<string> ParseTarget(string text)
    {
        string trimmed = ExpandHomeFolder(text.Trim());

        if (IsUrl(trimmed) || Directory.Exists(trimmed) || File.Exists(trimmed))
            return new List<string> { trimmed };

        return SplitCommandLine(trimmed);
    }

    /// <summary>
    /// Replaces a leading "~" with the user's home folder, as Bash does.
    /// </summary>
    private static string ExpandHomeFolder(string text)
    {
        if (text != "~" && !text.StartsWith("~/") && !text.StartsWith("~\\"))
            return text;

        string home = AppPaths.GetFolder(Environment.SpecialFolder.UserProfile);
        return home + text.Substring(1);
    }

    /// <summary>
    /// Decides the action type from its target: URL, folder, app file or terminal command.
    /// </summary>
    private static string DetectType(string target)
    {
        if (IsUrl(target))
            return ShortcutTypes.Url;

        if (Directory.Exists(target))
            return ShortcutTypes.Folder;

        if (File.Exists(target))
            return ShortcutTypes.App;

        return ShortcutTypes.Command;
    }

    /// <summary>
    /// Returns true if the text is an absolute http or https address.
    /// </summary>
    private static bool IsUrl(string text)
    {
        return Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Splits a command line by spaces, keeping text inside double quotes together.
    /// </summary>
    private static List<string> SplitCommandLine(string commandLine)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in commandLine)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (char.IsWhiteSpace(c) && !inQuotes)
                AddPart(parts, current);
            else
                current.Append(c);
        }

        AddPart(parts, current);
        return parts;
    }

    /// <summary>
    /// Moves the collected characters into the parts list, if there are any.
    /// </summary>
    private static void AddPart(List<string> parts, StringBuilder current)
    {
        if (current.Length == 0)
            return;

        parts.Add(current.ToString());
        current.Clear();
    }
}
