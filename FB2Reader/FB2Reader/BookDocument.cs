using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FB2Reader
{
    public class BookDocument
    {
        public List<BookChapter> Chapters { get; private set; }
        public int TotalPagesInBook { get; private set; }
        public Size PageDimensions { get; private set; }

        public BookDocument()
        {
            Chapters = new List<BookChapter>();
            TotalPagesInBook = 0;
        }

        public bool LoadAndProcessFile(string filePath, Size pageDimensions)
        {
            this.PageDimensions = pageDimensions;
            FB2Parser parser = new FB2Parser();

            List<BookChapter> parsedChapters = parser.ParseFb2File(filePath);
            if (parsedChapters == null || !parsedChapters.Any())
            {
                return false;
            }
            this.Chapters = parsedChapters;

            return PaginateBook();
        }

        private bool PaginateBook()
        {
            TotalPagesInBook = 0;
            if (this.PageDimensions.Width <= 0 || this.PageDimensions.Height <= 0) return false;

            using (RichTextBox tempRtb = new RichTextBox())
            {
                tempRtb.Size = this.PageDimensions;
                tempRtb.WordWrap = true;
                // tempRtb.Font = new Font("Segoe UI", 14F);  // Font controlled by RTF

                foreach (var chapter in Chapters)
                {
                    PaginateChapter(chapter, tempRtb);
                    TotalPagesInBook += chapter.PagesRtf.Count;
                }
            }
            return TotalPagesInBook > 0;
        }

        private void PaginateChapter(BookChapter chapter, RichTextBox tempRtb)
        {
            chapter.PagesRtf.Clear();
            if (string.IsNullOrEmpty(chapter.FullRtfContent))
            {
                chapter.PagesRtf.Add(FB2Parser.GetRtfHeader() + " " + FB2Parser.GetRtfFooter());
                return;
            }

            using (RichTextBox workRtb = new RichTextBox())
            {
                workRtb.Size = tempRtb.Size;
                workRtb.WordWrap = true;
                workRtb.Rtf = chapter.FullRtfContent;

                List<string> pagesRtf = new List<string>();
                int startCharIndex = 0;
                float pageFillThreshold = this.PageDimensions.Height * 0.8f;

                while (startCharIndex < workRtb.TextLength)
                {
                    int endCharIndex = FindNextPageEnd(workRtb, tempRtb, startCharIndex, pageFillThreshold);
                    workRtb.Select(startCharIndex, endCharIndex - startCharIndex);
                    pagesRtf.Add(workRtb.SelectedRtf);
                    startCharIndex = endCharIndex;
                }

                chapter.PagesRtf = pagesRtf;
            }

            if (chapter.PagesRtf.Count == 0 && !string.IsNullOrEmpty(chapter.FullRtfContent))
            {
                chapter.PagesRtf.Add(chapter.FullRtfContent);
            }
            else if (chapter.PagesRtf.Count == 0 && string.IsNullOrEmpty(chapter.FullRtfContent))
            {
                chapter.PagesRtf.Add(FB2Parser.GetRtfHeader() + " " + FB2Parser.GetRtfFooter());
            }
        }

        private int FindNextPageEnd(RichTextBox workRtb, RichTextBox tempRtb, int startCharIndex, float pageFillThreshold)
        {
            int endCharIndex = startCharIndex;
            int lastGoodBreak = startCharIndex;

            while (endCharIndex < workRtb.TextLength)
            {
                endCharIndex++;
                workRtb.Select(startCharIndex, endCharIndex - startCharIndex);
                tempRtb.Rtf = workRtb.SelectedRtf;

                float selectedHeight = tempRtb.GetPositionFromCharIndex(tempRtb.TextLength).Y;

                if (selectedHeight > pageFillThreshold)
                {
                    endCharIndex = FindBreakPoint(workRtb, startCharIndex, endCharIndex);
                    return endCharIndex;
                }
            }
            return endCharIndex;
        }

        private int FindBreakPoint(RichTextBox rtb, int startCharIndex, int endCharIndex)
        {
            // Search backward for a whitespace or hyphen
            for (int i = endCharIndex - 1; i >= startCharIndex; i--)
            {
                if (char.IsWhiteSpace(rtb.Text[i]) || rtb.Text[i] == '-')
                {
                    return i + 1; // Break after the whitespace or hyphen
                }
            }

            return endCharIndex; // No good break point found - force break
        }

        public (int chapterIndex, int pageInChapterIndex) GetChapterAndPageIndices(int absolutePageNumber)
        {
            if (absolutePageNumber < 1) absolutePageNumber = 1;
            if (TotalPagesInBook == 0) return (0, 0); // No pages, return first chapter/page
            if (absolutePageNumber > TotalPagesInBook) absolutePageNumber = TotalPagesInBook;

            int pagesTraversed = 0;
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (!Chapters[i].PagesRtf.Any() && Chapters.Count == 1) return (i, 0); // single empty chapter
                if (!Chapters[i].PagesRtf.Any()) continue; // Skip chapters with no pages if there are others

                if (absolutePageNumber <= pagesTraversed + Chapters[i].PagesRtf.Count)
                {
                    return (i, absolutePageNumber - pagesTraversed - 1);
                }
                pagesTraversed += Chapters[i].PagesRtf.Count;
            }

            if (Chapters.Any())
            {
                int lastChapterIdx = Chapters.Count - 1;
                int lastPageInLastChapterIdx = Chapters[lastChapterIdx].PagesRtf.Any() ? Chapters[lastChapterIdx].PagesRtf.Count - 1 : 0;
                return (lastChapterIdx, lastPageInLastChapterIdx);
            }
            return (0, 0);
        }

        public int GetAbsolutePageNumber(int chapterIndex, int pageInChapterIndex)
        {
            if (TotalPagesInBook == 0) return 1;
            int absolutePage = 0;
            for (int i = 0; i < chapterIndex; i++)
            {
                absolutePage += Chapters[i].PagesRtf.Count;
            }
            absolutePage += pageInChapterIndex + 1;
            return absolutePage;
        }
    }
}