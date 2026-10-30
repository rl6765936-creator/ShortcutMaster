namespace ShortcutMaster.Models;

/// <summary>
/// Represents a single user-defined shortcut: a name and one or more actions that run in order.
/// </summary>
public class Shortcut
{
    public string Name { get; set; } = string.Empty;

    public List<ShortcutAction> Actions { get; set; } = new();

    public bool Trusted { get; set; }

    /// <summary>
    /// Builds a readable one-line description of all the actions.
    /// </summary>
    public string Describe()
    {
        return string.Join("  ->  ", Actions.Select(action => action.Describe()));
    }
}

/// <summary>
/// One step of a shortcut: what kind of thing to run, its target and its arguments.
/// </summary>
public class ShortcutAction
{
    public string Type { get; set; } = ShortcutTypes.Command;

    public string Target { get; set; } = string.Empty;

    public List<string> Arguments { get; set; } = new();

    /// <summary>
    /// Returns true if this action has the given type, ignoring upper/lower case.
    /// </summary>
    public bool Is(string type)
    {
        return string.Equals(Type, type, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds a readable description of this action.
    /// </summary>
    public string Describe()
    {
        string text = Arguments.Count == 0 ? Target : Target + " " + string.Join(" ", Arguments);
        return Is(ShortcutTypes.Cd) ? "cd " + text : text;
    }
}

/// <summary>
/// Holds the supported action type names as constants.
/// </summary>
public static class ShortcutTypes
{
    public const string Command = "command";
    public const string App = "app";
    public const string Folder = "folder";
    public const string Url = "url";
    public const string Cd = "cd";

    public static readonly string[] All = { Command, App, Folder, Url, Cd };
}
