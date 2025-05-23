using System.Collections.Generic;

namespace FB2Reader
{
    public class BookChapter
    {
        public string Title { get; set; }
        public string FullRtfContent { get; set; } // Original RTF for the whole chapter
        public List<string> PagesRtf { get; set; } // Paginated RTF content for this chapter

        public BookChapter(string title, string fullRtfContent)
        {
            Title = title;
            FullRtfContent = fullRtfContent;
            PagesRtf = new List<string>();
        }
    }
}