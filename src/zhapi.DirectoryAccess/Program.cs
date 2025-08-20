#pragma warning disable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using ZhApi.Configs;
using ZhApi.Services;
using static ZhApi.FileAccessHelper;

var service = new AppBuilder().Build();
var dirs = service.Service
    .GetRequiredService<IOptionsSnapshot<AppConfig>>()
    .Value.GetDirectorys();

var ex = SetDirectorysAccess(dirs);
if (ex is null)
{
    Console.WriteLine("执行结束");
    await Task.Delay(2000);
}
else
{
    Console.WriteLine($"执行失败 : {ex.Message}");
    Console.ReadLine();
}

if (args.Contains("del"))
    DelDirectoryAccess(dirs);

static Exception? SetDirectorysAccess(params IEnumerable<string> directorys)
{
    foreach (var dir in GetTargetDirectorys(directorys))
    {
        var ex = SetDirectoryAccess(dir);
        if (ex != null) return ex;
    }
    return null;
}

static Exception? SetDirectoryAccess(string directorys)
{
    Console.Write($"更新权限: {directorys} ");
    try
    {
        SetAccess(directorys);
        Console.WriteLine("成功");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine("-----失败-----");
        return ex;
    }
}

static void DelDirectoryAccess(params IEnumerable<string> directorys)
{
    foreach (var dir in directorys)
    {
        Console.Write($"删除目录权限: {dir} ");
        DelAccess(dir);
        Console.WriteLine("成功");
    }
}