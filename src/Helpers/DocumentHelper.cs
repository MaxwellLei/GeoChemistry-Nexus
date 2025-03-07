using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace GeoChemistryNexus.Helpers
{
    public static class DocumentHelper
    {
        // 压缩 RichTextBox.Document 内容
        public static string CompressRichTextBoxContent(RichTextBox richTextBox)
        {
            string xaml = XamlWriter.Save(richTextBox.Document);
            return CompressString(xaml);
        }

        // 解压缩并设置 RichTextBox.Document 内容
        public static void DecompressRichTextBoxContent(RichTextBox richTextBox, string compressedContent)
        {
            string xaml = DecompressString(compressedContent);
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                FlowDocument document = XamlReader.Load(stream) as FlowDocument;
                richTextBox.Document = document;
            }
        }

        // 压缩字符串
        private static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gZipStream.Write(buffer, 0, buffer.Length);
                }
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        // 解压缩字符串
        private static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (MemoryStream memoryStream = new MemoryStream(gZipBuffer))
            {
                using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (StreamReader reader = new StreamReader(gZipStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
