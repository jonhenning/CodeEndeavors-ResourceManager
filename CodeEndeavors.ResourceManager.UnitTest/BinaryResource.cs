using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CodeEndeavors.ResourceManager.UnitTest
{
    public class BinaryResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string MimeType { get; set; }
        public long? Size { get; set; }

        public Stream GetStream()
        {
            // read input etx
            FileInfo file = new FileInfo(Path);
            var len = file.Length;
            int bytes;
            byte[] buffer = new byte[1024];
            var ms = new MemoryStream();
            using(var stream = System.IO.File.OpenRead(Path)) 
            {
                while (len > 0 && (bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytes);
                    len -= bytes;
                }
            }
            return ms;
        }

        public void WriteStream(string FileName)
        {
            using (var stream = new FileStream(FileName, FileMode.Open))
            {
                WriteStream(stream);
            }
        }

        public void WriteStream(Stream FileStream)
        {
            int Length = 1024;
            Size = FileStream.Length;
            Byte[] buffer = new Byte[Length];
            int bytesRead = FileStream.Read(buffer, 0, Length);
            using (var writeStream = new FileStream(Path, FileMode.Create, FileAccess.Write))
            {
                // write the required bytes
                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = FileStream.Read(buffer, 0, Length);
                }
            }
        }

    }
}
