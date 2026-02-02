using System.Globalization;

#pragma warning disable IDE0060

namespace gAPI.Extentions;

public static class GuidToStringExtention
{
    public static string ToString(this Guid guid, CultureInfo cultureInfo)
    {
        return guid.ToString();
    }
}
