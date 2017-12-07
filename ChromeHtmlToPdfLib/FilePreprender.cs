using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeHtmlToPdfLib
{
    public class FilePrepender
    {
        private readonly string _file;

        public FilePrepender(string filePath)
        {
            _file = filePath;
        }

        public void Prependline(string line)
        {
            Prepend(line + Environment.NewLine);
        }

        private static void ShiftSection(byte[] chunk, FileStream readStream, FileStream writeStream)
        {
            var initialOffsetRead = readStream.Position;
            var initialOffsetWrite = writeStream.Position;
            var offset = 0;
            var remaining = chunk.Length;

            do
            {
                var read = readStream.Read(chunk, offset, remaining);
                offset += read;
                remaining -= read;
            } while (remaining > 0);

            writeStream.Write(chunk, 0, chunk.Length);
            writeStream.Seek(initialOffsetWrite, SeekOrigin.Begin);
            readStream.Seek(initialOffsetRead, SeekOrigin.Begin);
        }

        public void Prepend(string text)
        {
            var bytes = Encoding.Default.GetBytes(text);
            var chunk = new byte[bytes.Length];

            using (var readStream = File.Open(_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var writeStream = File.Open(_file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    readStream.Seek(0, SeekOrigin.End);
                    writeStream.Seek(chunk.Length, SeekOrigin.End);
                    var size = readStream.Position;

                    while (readStream.Position - chunk.Length >= 0)
                    {
                        readStream.Seek(-chunk.Length, SeekOrigin.Current);
                        writeStream.Seek(-chunk.Length, SeekOrigin.Current);
                        ShiftSection(chunk, readStream, writeStream);
                    }

                    readStream.Seek(0, SeekOrigin.Begin);
                    writeStream.Seek(Math.Min(size, chunk.Length), SeekOrigin.Begin);
                    ShiftSection(chunk, readStream, writeStream);
                    writeStream.Seek(0, SeekOrigin.Begin);
                    writeStream.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
