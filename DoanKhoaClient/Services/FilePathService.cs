using System;
using System.IO;
using System.Diagnostics;

namespace DoanKhoaClient.Services
{
    public static class FilePathService
    {
        // URL cơ sở của server
        private static readonly string BaseServerUrl = "http://localhost:5299";

        public static string GetFullUrl(string partialUrl)
        {
            if (string.IsNullOrEmpty(partialUrl))
                return string.Empty;

            // Nếu đã là URL đầy đủ, trả về nguyên bản
            if (Uri.IsWellFormedUriString(partialUrl, UriKind.Absolute))
                return partialUrl;

            // Nếu bắt đầu bằng /Uploads/
            if (partialUrl.StartsWith("/Uploads/"))
                return $"{BaseServerUrl}{partialUrl}";

            // Nếu là đường dẫn local file
            if (System.IO.File.Exists(partialUrl))
            {
                return partialUrl;
            }

            // Trường hợp còn lại, thêm tiền tố
            string fileName = Path.GetFileName(partialUrl);
            return $"{BaseServerUrl}/Uploads/{fileName}";
        }

        // Thêm phương thức GetServerFilePath
        public static string GetServerFilePath(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return null;

                // Nếu đã là đường dẫn local file
                if (System.IO.File.Exists(url))
                    return url;

                // Chỉ xử lý URL từ server của chúng ta
                if (url.StartsWith(BaseServerUrl))
                {
                    // Tách phần đường dẫn từ URL đầy đủ
                    string relativePath = url.Substring(BaseServerUrl.Length);

                    // Đảm bảo bắt đầu với /Uploads/
                    if (relativePath.StartsWith("/Uploads/"))
                    {
                        // Lấy tên file
                        string fileName = Path.GetFileName(relativePath);

                        // Trả về tên file để client có thể yêu cầu file này từ server
                        return fileName;
                    }
                }

                // Nếu là URL bên ngoài, chỉ trả về tên file
                return Path.GetFileName(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetServerFilePath: {ex.Message}");
                return null;
            }
        }
    }
}