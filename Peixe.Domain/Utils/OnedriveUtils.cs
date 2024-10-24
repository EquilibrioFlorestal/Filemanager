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
    public static readonly String CaminhoOnedrive = 
        Environment.GetEnvironmentVariable("ManualOneDrive", EnvironmentVariableTarget.Machine) ??
        Environment.GetEnvironmentVariable("ManualOneDrive", EnvironmentVariableTarget.User) ??
        String.Empty; 

    public static Boolean CheckProcessOnedrive()
    {
        Process[] pname = Process.GetProcessesByName("OneDrive");
        return pname.Length != 0;
    }

    public static List<String> GetDownloadedFiles(String caminho, String modulo, String extensao)
    {
        extensao = extensao.Replace(".", String.Empty);
        return Directory.GetFiles(caminho, $"{modulo}_*.{extensao}", SearchOption.AllDirectories).Where(OnedriveUtils.IsDownloaded).ToList();
    }

    private static Boolean IsDownloaded(String arquivo)
    {
        if (!File.Exists(arquivo)) return false;
        FileAttributes currentAttributes = File.GetAttributes(arquivo);
        Boolean isDownloaded = ( currentAttributes & FileAttributesOnedrive.Downloaded ) == FileAttributesOnedrive.Downloaded;
        return !isDownloaded;
    }

    public static void SetOffline(String arquivo)
    {
        if (!File.Exists(arquivo)) return;
        FileAttributes currentAttributes = File.GetAttributes(arquivo);
        currentAttributes &= ~FileAttributesOnedrive.Online;
        currentAttributes |= FileAttributesOnedrive.Offline;
        File.SetAttributes(arquivo, currentAttributes);
    }

    public static void SetOnline(String arquivo)
    {
        if (!File.Exists(arquivo)) return;
        FileAttributes currentAttributes = File.GetAttributes(arquivo);
        currentAttributes &= ~FileAttributesOnedrive.Offline;
        currentAttributes |= FileAttributesOnedrive.Online;
        File.SetAttributes(arquivo, currentAttributes);
    }
}