using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfterNow.PrezSDK
{
    public static class UriBuilderExtension
    {
        public static string UriPath(string path)
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