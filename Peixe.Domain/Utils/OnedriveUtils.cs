namespace Domain.Utils;

public static class OnedriveUtils
{
    public static readonly string CaminhoOnedrive = Environment.GetEnvironmentVariable("OneDrive", EnvironmentVariableTarget.User) ?? string.Empty;
}