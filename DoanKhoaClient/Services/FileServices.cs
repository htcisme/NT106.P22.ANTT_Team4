using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DoanKhoaClient.Services
{
    public static class FileService
    {
        // Base server directory where files are stored
        private static readonly string ServerUploadsPath = "D:\\Study\\UIT\\Mang\\DOANMANG\\NT106.P22.ANTT_Team4\\DoanKhoaServer\\Uploads";

        // HTTP base URL for fallback
        private static readonly string ApiBaseUrl = "http://localhost:5299";

        /// <summary>
        /// Gets the physical file path from a server relative URL
        /// </summary>
        public static string GetServerFilePath(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return null;

            return Path.Combine(ServerUploadsPath, Path.GetFileName(fileUrl));
        }

        /// <summary>
        /// Gets full HTTP URL from a relative URL
        /// </summary>
        public static string GetFullUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return null;

            if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
                return relativeUrl;

            return ApiBaseUrl + (relativeUrl.StartsWith("/") ? "" : "/") + relativeUrl;
        }

        /// <summary>
        /// Downloads a file using direct file access when possible, falling back to HTTP
        /// </summary>
        public static async Task<bool> DownloadFileAsync(string fileUrl, string savePath)
        {
            // Try direct file access first
            string serverPath = GetServerFilePath(fileUrl);

            if (File.Exists(serverPath))
            {
                File.Copy(serverPath, savePath, true);
                return true;
            }

            // Fall back to HTTP download
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string fullUrl = GetFullUrl(fileUrl);
                    using (var response = await client.GetAsync(fullUrl))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                await response.Content.CopyToAsync(fileStream);
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}