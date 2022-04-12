using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AfterNow.PrezSDK
{
    /// <summary>
    /// Helper class which creates a proper Uri path
    /// </summary>
    internal static class UriBuilderExtension
    {
        /// <summary>
        /// Creates Uri path for the given <paramref name="path"/>
        /// </summary>
        /// <param name="path"> Asset path </param>
        /// <returns> Uri converted string of the <paramref name="path"/> </returns>
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