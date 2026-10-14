namespace TaskFlow.Application.Common;

public static class RowVersionCodec
{
    public static byte[]? Decode(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion)) return null;
        try
        {
            return Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
