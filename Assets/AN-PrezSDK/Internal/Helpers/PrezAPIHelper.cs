using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AfterNow.PrezSDK.Internal.Helpers
{
    public static class PrezAPIHelper
    {
        internal static List<WebClient> downloadHandler = new List<WebClient>();

        internal static async Task<string> Get(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception e)
            {
                PrezDebugger.Exception(e);
                return null;
            }
        }

        internal static async Task Download(string url, string path)
        {
            var client = new WebClient();
            downloadHandler.Add(client);
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0);
            client.DownloadFileCompleted += (x, y) =>
            {
                if (y.Cancelled)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                semaphoreSlim.Release();
                downloadHandler.Remove(client);
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                _ = client.DownloadFileTaskAsync(url, path);
                await semaphoreSlim.WaitAsync();

                //OnDownload(true);
            }
            catch (Exception e)
            {
                PrezDebugger.Error($"Failed to download asset from url : {url}\n{e}");
                semaphoreSlim.Release();
                //PrezDebugger.Exception(e);
                //OnDownload(false);
            }
        }

        internal static async Task<string> Post(string url, string data)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.ContentLength = dataBytes.Length;
                request.ContentType = "application/json";
                request.Method = "POST";

                using (Stream requestBody = request.GetRequestStream())
                {
                    await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception e)
            {
                PrezDebugger.Exception(e);
                return null;
            }
        }

        public static void StopDownload()
        {
            foreach (var item in downloadHandler)
            {
                item.CancelAsync();
            }

            downloadHandler.Clear();
        }
    }
}