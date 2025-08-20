#pragma warning disable
using System;
using System.Diagnostics;
using System.Security.AccessControl;

namespace ZhApi;
public static class FileAccessHelper
{
    const string UserName = "Authenticated Users";

    public static IEnumerable<string> GetAccessList(string directory)
    {
        var di = new DirectoryInfo(directory);
        var ds = di.GetAccessControl(AccessControlSections.Access);
        foreach (FileSystemAccessRule rule in ds.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
        {
            var fsr = rule.FileSystemRights;
            if (fsr.HasFlag(FileSystemRights.Modify) &&
                fsr.HasFlag(FileSystemRights.Synchronize))
                yield return $"{rule.FileSystemRights} {rule.IdentityReference.Value}";
        }
    }

    public static bool ExistUser(string directory, string userName = UserName)
    {
        var targat = $"\\{userName}";
        return GetAccessList(directory).Any(name => name.EndsWith(targat));
    }

    public static bool SetAccess(string directory, string userName = UserName)
    {
        var di = new DirectoryInfo(directory);
        var security = di.GetAccessControl(AccessControlSections.Access);
        var fileRule = CreateFileRule(userName);
        security.ModifyAccessRule(AccessControlModification.Reset, fileRule, out bool res);
        di.SetAccessControl(security);
        return res;
    }

    public static bool DelAccess(string directory, string userName = UserName)
    {
        var di = new DirectoryInfo(directory);
        var security = di.GetAccessControl(AccessControlSections.Access);
        var fileRule = CreateFileRule(userName);
        security.ModifyAccessRule(AccessControlModification.RemoveAll, fileRule, out bool res);
        di.SetAccessControl(security);
        return res;
    }

    private static FileSystemAccessRule CreateFileRule(string userName = UserName)
    {
        var fileSystemRights = FileSystemRights.Modify | FileSystemRights.Synchronize;
        var inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        return new FileSystemAccessRule(userName,
            fileSystemRights,
            inherits, PropagationFlags.None,
            AccessControlType.Allow);
    }

    public static IEnumerable<string> GetTargetDirectorys(params IEnumerable<string> directorys) =>
        directorys.Where(dir => !ExistUser(dir));

    public static void TestAccessRun(params IEnumerable<string> directorys)
    {
        var dirs = GetTargetDirectorys(directorys).ToArray();
        if (dirs.Length > 0) RunExe();
    }

    private static void RunExe()
    {
        var app = typeof(FileAccessHelper).Assembly.FullName
            ?? throw new InvalidOperationException("找不到路径");

        var dir = Path.GetDirectoryName(app);
        var exePath = Path.Combine(dir, "zhapi.DirectoryAccess.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
        };
        Process.Start(startInfo).WaitForExit();
    }
}