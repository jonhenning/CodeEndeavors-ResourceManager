using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
//using System.Net.Http;
using System.Net;

namespace CodeEndeavors.Extensions
{
    public static class HttpExtensions
    {
        public static void DownloadFile(this string url, string fileName, int timeOut = int.MaxValue, string contentType = null, string method = "GET", bool overwrite = true)
        {
            var file = new FileInfo(fileName);
            if (!file.Directory.Exists)
                file.Directory.Create();

            var request = HttpWebRequest.Create(url);
            request.Timeout = timeOut;
            request.ContentType = contentType;
            request.Method = method;
            var response = (HttpWebResponse)request.GetResponse();
            if (overwrite && File.Exists(fileName))
                File.Delete(fileName);

            response.GetResponseStream().WriteStream(fileName, response.ContentLength);
            //using (var reader = new BinaryReader(response.GetResponseStream()))
            //{
            //    reader.Read(
            //}
        }

        //public static Task ReadAsFileAsync(this HttpContent content, string filename, bool overwrite)
        //{
        //    var pathname = Path.GetFullPath(filename);
        //    if (!overwrite && File.Exists(filename))
        //    {
        //        throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
        //    }

        //    FileStream fileStream = null;
        //    try
        //    {
        //        fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
        //        return content.CopyToAsync(fileStream).ContinueWith(
        //            (copyTask) =>
        //            {
        //                fileStream.Close();
        //            });
        //    }
        //    catch
        //    {
        //        if (fileStream != null)
        //        {
        //            fileStream.Close();
        //        }

        //        throw;
        //    }
        //}
    }
}
