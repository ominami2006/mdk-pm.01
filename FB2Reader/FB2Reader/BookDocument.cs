using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms; // Required for RichTextBox, Point

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
                tempRtb.WordWrap = true; // Ensure WordWrap is enabled for measurement
                                         // tempRtb.Font = new Font("Segoe UI", 14F); // Set default font to match Form3 and RTF header
                                         // This helps if RTF is very basic, but RTF content usually dictates font.

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

            using (RichTextBox workRtb = new RichTextBox()) // Use a separate RTB for manipulation
            {
                workRtb.Size = tempRtb.Size; // Same dimensions as the measuring one
                workRtb.WordWrap = true;     // Critical for word wrapping behavior
                // workRtb.Font = tempRtb.Font; // Match font
                workRtb.Rtf = chapter.FullRtfContent; // Load full chapter RTF

                while (workRtb.TextLength > 0)
                {
                    Application.DoEvents();

                    int lastCharVisibleIndex = workRtb.GetCharIndexFromPosition(new Point(workRtb.ClientSize.Width - 1, workRtb.ClientSize.Height - 1));
                    if (lastCharVisibleIndex == 0 && workRtb.TextLength > 1) // If first char is the only one visible but there's more text
                    {
                        // This might happen if content is too large or font makes first char take whole space.
                        // Try to get at least some content for the page.
                        // Fallback to a small portion if the first char itself is huge.
                        // Or if the first word itself is longer than the line.
                        int testidx = workRtb.GetCharIndexFromPosition(new Point(5, 5)); // some char near beginning
                        if (testidx > 0) lastCharVisibleIndex = testidx;
                        else lastCharVisibleIndex = Math.Min(workRtb.TextLength - 1, 10); // take a few chars if all else fails
                    }


                    if (lastCharVisibleIndex >= workRtb.TextLength - 1) // All remaining text fits
                    {
                        chapter.PagesRtf.Add(workRtb.Rtf);
                        workRtb.Clear();
                        break;
                    }

                    // Find a good break point (space or end of paragraph)
                    int breakCharIndex = lastCharVisibleIndex;
                    bool breakFound = false;
                    // Search backwards from lastCharVisibleIndex for a space or start of line
                    for (int i = lastCharVisibleIndex; i >= 0; i--)
                    {
                        char currentChar = workRtb.Text[i];
                        if (char.IsWhiteSpace(currentChar)) // Found a whitespace character
                        {
                            breakCharIndex = i; // Break *after* this whitespace if it's not a leading one for next page
                            // If it's a newline, we want to include it in the current page usually.
                            // If it's a space, the space itself might go to current or next page depending on where word starts.
                            // Let's try breaking *at* the space (so space might be end of current page, or start of next selection)
                            breakFound = true;
                            break;
                        }
                        // RTF \par is more complex to detect directly from plain text here.
                        // RichTextBox's own rendering handles \par for line breaks.
                    }

                    if (!breakFound && lastCharVisibleIndex > 0)
                    {
                        // No space found backward, means a very long word is being split by RichTextBox's character wrap.
                        // We take the original lastCharVisibleIndex.
                        // Hyphenation here would be ideal but is complex.
                        breakCharIndex = lastCharVisibleIndex;
                    }
                    else if (!breakFound && workRtb.TextLength > 0)
                    { // No space, and it's the start of the text
                        breakCharIndex = lastCharVisibleIndex; // take what RTB gives
                    }


                    // Select text for the current page
                    workRtb.Select(0, breakCharIndex + 1);
                    chapter.PagesRtf.Add(workRtb.SelectedRtf);

                    // Prepare remaining RTF
                    if (breakCharIndex + 1 < workRtb.TextLength)
                    {
                        workRtb.Select(breakCharIndex + 1, workRtb.TextLength - (breakCharIndex + 1));
                        string remainingRtf = workRtb.SelectedRtf;
                        workRtb.Rtf = remainingRtf;
                    }
                    else
                    {
                        workRtb.Clear();
                        break;
                    }
                    Application.DoEvents();
                }
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