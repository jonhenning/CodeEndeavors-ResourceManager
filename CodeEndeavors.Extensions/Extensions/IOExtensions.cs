using System;
using System.IO;
using System.Web;
using System.Web.Caching;

namespace CodeEndeavors.Extensions
{
    public static class IOExtensions
    {
        public static T GetFileJSONObject<T>(this string fileName, bool cacheFile = false)
        {
            T o = default(T);
            if (cacheFile)
                o = HttpContext.Current.Cache.GetSetting<T>(fileName, default(T));
            if (o == null)
            {
                var json = fileName.GetFileContents();
                o = json.ToObject<T>();
                if (cacheFile)
                    HttpContext.Current.Cache.Add(fileName, o, new CacheDependency(fileName), System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
            }
            return o;
        }

        public static string GetFileContents(this string fileName, bool cacheFile)
        {
            if (cacheFile == false)
                return GetFileContents(fileName);

            var contents = HttpContext.Current.Cache.GetSetting<string>(fileName, "");
            if (String.IsNullOrEmpty(contents))
            {
                contents = GetFileContents(fileName);
                HttpContext.Current.Cache.Add(fileName, contents, new CacheDependency(fileName), System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
            }
            return contents;
        }

        public static string GetFileContents(this string fileName)
        {
            string contents = null;
            using (TextReader stream = new StreamReader(fileName))
            {
                contents = stream.ReadToEnd();
            }
            return contents;
        }

        //todo:  buffer???
        public static string GetFileBase64(this string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
            return Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
        }

        public static void Base64ToFile(this string base64, string fileName)
        {
            byte[] filebytes = Convert.FromBase64String(base64);
            var fs = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            fs.Write(filebytes, 0, filebytes.Length);
            fs.Close();
        }

        public static Stream GetStream(this string path)
        {
            // read input etx
            FileInfo file = new FileInfo(path);
            var len = file.Length;
            int bytes;
            byte[] buffer = new byte[1024];
            var ms = new MemoryStream();
            using (var stream = File.OpenRead(path))
            {
                while (len > 0 && (bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytes);
                    len -= bytes;
                }
            }
            return ms;
        }

        //public static void WriteStream(this string FileName)
        //{
        //    using (var stream = new FileStream(FileName, FileMode.Open))
        //    {
        //        WriteStream(stream, FileName);
        //    }
        //}

        public static long WriteStream(this Stream fileStream, string fileName, long? size = null)
        {
            int length = 1024;
            if (!size.HasValue)
                size = fileStream.Length;
            Byte[] buffer = new Byte[length];
            int bytesRead = fileStream.Read(buffer, 0, length);
            using (var writeStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                // write the required bytes
                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = fileStream.Read(buffer, 0, length);
                }
            }
            return size.Value;
        }

        public static void WriteText(this string text, string fileName)
        {
            System.IO.File.WriteAllBytes(fileName, System.Text.Encoding.UTF8.GetBytes(text));
        }

        public static string ResolvePath(this string path, bool ensureExists = false)
        {
            var ret = path;
            if (path.StartsWith("~"))
            {
                if (System.Web.HttpContext.Current != null)
                    ret = System.Web.HttpContext.Current.Server.MapPath(path);
                else
                    ret = path.Replace(@"~\", Environment.CurrentDirectory + @"\");
            }
            if (ensureExists && !Directory.Exists(ret))
                Directory.CreateDirectory(ret);

            return ret;
        }

        //public static Directory Ensure(this DirectoryInfo

    }

}
