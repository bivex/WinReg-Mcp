namespace WinRegMcp.Domain.Models;

/// <summary>
/// Represents the root registry hives in Windows.
/// </summary>
public enum RegistryHive
{
    LocalMachine,
    CurrentUser,
    ClassesRoot,
    Users,
    CurrentConfig
}

