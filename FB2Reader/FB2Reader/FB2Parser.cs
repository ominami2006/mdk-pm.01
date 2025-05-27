using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FB2Reader
{
    public class FB2Parser
    {
        private List<BookChapter> _chapters;
        private XNamespace _fb2Ns;

        public List<BookChapter> ParseFb2File(string filePath)
        {
            _chapters = new List<BookChapter>();
            _fb2Ns = "http://www.gribuser.ru/xml/fictionbook/2.0";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found at {filePath}");
                return null;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading FB2 XML: {ex.Message}");
                return null;
            }

            XElement fictionBook = doc.Element(_fb2Ns + "FictionBook");
            if (fictionBook == null) return null;

            XElement body = fictionBook.Element(_fb2Ns + "body");
            if (body == null) return null;

            ProcessBodyElement(body);

            return _chapters;
        }

        private void ProcessBodyElement(XElement element)
        {
            foreach (XElement sectionElement in element.Elements(_fb2Ns + "section"))
            {
                ProcessSection(sectionElement);
            }
            if (!_chapters.Any() && element.Elements(_fb2Ns + "p").Any())
            {
                StringBuilder chapterRtfBuilder = new StringBuilder();
                chapterRtfBuilder.Append(GetRtfHeader());
                ProcessElementsRecursive(element, chapterRtfBuilder);
                chapterRtfBuilder.Append(GetRtfFooter());
                _chapters.Add(new BookChapter("Начало книги", chapterRtfBuilder.ToString()));
            }
        }

        private void ProcessSection(XElement sectionElement)
        {
            string title = "Без названия";
            XElement titleElement = sectionElement.Element(_fb2Ns + "title");
            if (titleElement != null)
            {
                title = GetPlainText(titleElement).Trim();
            }
            else
            {
                XElement firstP = sectionElement.Descendants(_fb2Ns + "p").FirstOrDefault();
                if (firstP != null)
                {
                    title = GetPlainText(firstP).Trim();
                    if (title.Length > 80) title = title.Substring(0, 80) + "...";
                }
            }

            StringBuilder chapterRtfBuilder = new StringBuilder();
            chapterRtfBuilder.Append(GetRtfHeader());

            if (titleElement != null)
            {
                // For chapter titles, let's use a slightly larger font than the body, e.g., 16pt bold
                chapterRtfBuilder.Append(@"\par\pard\qc\b\fs32 ");
                chapterRtfBuilder.Append(GetElementRtf(titleElement));
                chapterRtfBuilder.Append(@"\b0\fs28\par\pard\ql "); // Reset to default body font size (14pt)
            }

            ProcessElementsRecursive(sectionElement, chapterRtfBuilder, skipTitle: true);

            chapterRtfBuilder.Append(GetRtfFooter());
            _chapters.Add(new BookChapter(title, chapterRtfBuilder.ToString()));
        }

        private void ProcessElementsRecursive(XElement parentElement, StringBuilder rtfBuilder, bool skipTitle = false)
        {
            foreach (XNode node in parentElement.Nodes())
            {
                if (node is XElement element)
                {
                    if (skipTitle && element.Name.LocalName == "title") continue;
                    if (element.Name.LocalName == "section")
                    {
                        ProcessElementsRecursive(element, rtfBuilder);
                        continue;
                    }
                    rtfBuilder.Append(GetElementRtf(element));
                }
                else if (node is XText textNode)
                {
                    rtfBuilder.Append(EscapeRtfText(textNode.Value));
                }
            }
        }

        private string GetElementRtf(XElement element)
        {
            StringBuilder sb = new StringBuilder();
            string tagName = element.Name.LocalName;

            bool isBlock = new[] { "p", "subtitle", "epigraph", "text-author", "poem", "stanza", "v" }.Contains(tagName);
            if (isBlock) sb.Append(@"\par ");

            switch (tagName)
            {
                case "p":
                    break;
                case "strong":
                case "b":
                    sb.Append(@"\b ");
                    break;
                case "emphasis":
                case "i":
                    sb.Append(@"\i ");
                    break;
                case "subtitle":
                    // Subtitles might be smaller than main titles but larger than body, e.g., 14pt bold
                    sb.Append(@"\pard\qc\b\fs28 "); // Centered, Bold, 14pt
                    break;
                case "epigraph":
                    sb.Append(@"\pard\li720\ri720\i ");
                    break;
                case "text-author":
                    sb.Append(@"\pard\qr\i ");
                    break;
                case "poem":
                    sb.Append(@"\pard\li360 ");
                    break;
                case "stanza": break;
                case "v": break;
                case "title": break;
                default: break;
            }

            foreach (XNode node in element.Nodes())
            {
                if (node is XElement childElement) sb.Append(GetElementRtf(childElement));
                else if (node is XText textNode) sb.Append(EscapeRtfText(textNode.Value));
            }

            switch (tagName)
            {
                case "strong": case "b": sb.Append(@"\b0 "); break;
                case "emphasis": case "i": sb.Append(@"\i0 "); break;
                case "subtitle":
                case "epigraph":
                case "text-author":
                case "poem":
                    sb.Append(@"\pard\ql ");
                    break;
            }
            return sb.ToString();
        }

        private string GetPlainText(XElement element)
        {
            StringBuilder sb = new StringBuilder();
            foreach (XNode node in element.Nodes())
            {
                if (node is XElement childElement)
                {
                    if (childElement.Name.LocalName == "p" && sb.Length > 0) sb.Append(" ");
                    sb.Append(GetPlainText(childElement));
                }
                else if (node is XText textNode) sb.Append(textNode.Value);
            }
            return sb.ToString().Replace("\n", " ").Replace("\r", "").Trim();
        }

        private string EscapeRtfText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
            return text.Replace("\n", @"\line ").Replace("\r", "");
        }

        public static string GetRtfHeader()
        {
            // Default Segoe UI 14pt (fs28)
            return @"{\rtf1\ansi\deff0\ansicpg1251\nouicompat{\fonttbl{\f0\fnil\fcharset204 Segoe UI;}{\f1\fnil\fcharset204 Calibri;}}\pard\sa200\sl276\slmult1\f0\fs28\lang1049 ";
        }

        public static string GetRtfFooter()
        {
            return "}";
        }
    }
}