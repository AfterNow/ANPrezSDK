using System;

namespace AfterNow.PrezSDK
{
    internal static class UriBuilderExtension
    {
        internal static string UriPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                UriBuilder builder = new UriBuilder(path)
                {
                    Scheme = "file"
                };
                return builder.ToString();
            }
            return null;
        }
    }
}
