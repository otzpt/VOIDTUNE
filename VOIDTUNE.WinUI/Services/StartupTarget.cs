using System;
using System.IO;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Decides whether a startup entry still points at something that exists.</summary>
public static class StartupTarget
{
    // Only absolute paths can be judged: a bare name like rundll32.exe resolves through PATH.
    public static bool Exists(string command)
    {
        string cmd = Environment.ExpandEnvironmentVariables(command).Trim();
        if (cmd.Length == 0) return true;

        if (cmd[0] == '"')
        {
            int end = cmd.IndexOf('"', 1);
            if (end <= 1) return true;
            string quoted = cmd[1..end];
            return !Path.IsPathFullyQualified(quoted) || File.Exists(quoted);
        }

        if (!Path.IsPathFullyQualified(cmd)) return true;

        // Unquoted paths can contain spaces, so try every space as the end of the path.
        for (int sp = cmd.IndexOf(' '); sp > 0; sp = cmd.IndexOf(' ', sp + 1))
            if (File.Exists(cmd[..sp])) return true;
        return File.Exists(cmd);
    }

    /// <summary>False only for a .lnk whose target is confirmed gone.</summary>
    public static bool LinkResolves(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return true;
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return true;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic link = shell.CreateShortcut(path);
            string target = link.TargetPath;
            return target.Length == 0 || Exists($"\"{target}\"");
        }
        catch { return true; }
    }
}
