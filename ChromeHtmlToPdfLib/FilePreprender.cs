using System.IO;

namespace ChromeHtmlToPdfLib
{
    public class PreWrapper
    {
        /*
        <html>
        <head>
        <style>
        pre { white-space: pre-wrap; }
        </style>
        </head>
        <body>
        <pre>
        </pre>
        </html>
        */

        static void PreWrapFile(string inputFile, string outputFile)
        {
            using (var writer = new StreamWriter(outputFile))
            using (var reader = new StreamReader(inputFile))
            {
                //writer.WriteLine("A,B,C");
                while (!reader.EndOfStream)
                    writer.WriteLine(reader.ReadLine());
            }

            File.Copy(outputFile, inputFile, true);
        }
    }
}
