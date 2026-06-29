namespace VOIDTUNE.WinUI.Models;

/// <summary>A registry backup folder created before applying tweaks.</summary>
public sealed class BackupItem
{
    public string FolderName { get; init; } = "";
    public string FullPath { get; init; } = "";
    public string Created { get; init; } = "";
    public string Note { get; init; } = "";
    public int KeyCount { get; init; }

    public string KeyCountText => $"{KeyCount} keys";
}
