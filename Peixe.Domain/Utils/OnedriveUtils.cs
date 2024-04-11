using System.Diagnostics;

namespace Domain.Utils;

internal static class FileAttributesOnedrive
{
    public const FileAttributes Offline = (FileAttributes)0x00100000;
    public const FileAttributes Online = (FileAttributes)0x00080000;
    public const FileAttributes Downloaded = (FileAttributes)0x00400000;
}

public static class OnedriveUtils
{
    public static readonly string CaminhoOnedrive = Environment.GetEnvironmentVariable("OneDrive", EnvironmentVariableTarget.User) ?? string.Empty;

    public static bool CheckProcessOnedrive()
    {
        Process[] pname = Process.GetProcessesByName("OneDrive");
        return (pname.Length != 0);
    }
    
    public static List<string> GetDownloadedFiles(string caminho, string modulo, string extensao)
    {
        extensao = extensao.Replace(".", string.Empty);
        return Directory.GetFiles(caminho, $"{modulo}_*.{extensao}", SearchOption.AllDirectories).Where(OnedriveUtils.IsDownloaded).ToList();
    }

    private static bool IsDownloaded(string arquivo)
    {
        if (!File.Exists(arquivo)) return false;
        FileAttributes currentAttributes = File.GetAttributes(arquivo);
        bool isDownloaded = (currentAttributes & FileAttributesOnedrive.Downloaded) == FileAttributesOnedrive.Downloaded;
        return !isDownloaded;
    }

    public static void SetOffline(string arquivo)
    {
        if (!File.Exists(arquivo)) return;
        FileAttributes currentAttributes = File.GetAttributes(arquivo);
        currentAttributes &= ~FileAttributesOnedrive.Online;
        currentAttributes |= FileAttributesOnedrive.Offline;
        File.SetAttributes(arquivo, currentAttributes);
    }

    public static void SetOnline(string arquivo)
    {
        if (!File.Exists(arquivo)) return;
        FileAttributes currentAttributes = File.GetAttributes(arquivo);
        currentAttributes &= ~FileAttributesOnedrive.Offline;
        currentAttributes |= FileAttributesOnedrive.Online;
        File.SetAttributes(arquivo, currentAttributes);
    }
}