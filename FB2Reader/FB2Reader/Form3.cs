using System;
using System.Drawing;
using System.Linq; // Required for .Any()
using System.Windows.Forms;

namespace FB2Reader
{
    public partial class Form3 : Form
    {
        private BookDocument _book;
        private RichTextBox richTextBoxDisplay;
        private Panel readerPanel;
        private Button nextButton, prevButton, tocButton;
        private TrackBar pageTrackBar;
        private Label pageInfoLabel;
        private Size _readerPageDimensions; // Dimensions used for pagination

        private int _currentChapterIndex = 0;
        private int _currentPageInChapterIndex = 0;
        private readonly string _fileName; // To store the file name

        public Form3(BookDocument book, string fileName)
        {
            InitializeComponent();
            _book = book;
            _fileName = fileName;
            _readerPageDimensions = new Size(860, 580);
            SetupUI();
            LoadInitialPage();
            this.KeyPreview = true;
        }

        private void SetupUI()
        {
            this.Text = "Читалка";
            this.ClientSize = new Size(800, 730);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            readerPanel = new Panel
            {
                Size = _readerPageDimensions,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };
            this.Controls.Add(readerPanel);

            richTextBoxDisplay = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                Font = new Font("Segoe UI", 14F),
                WordWrap = true,
                BackColor = Color.WhiteSmoke
            };
            readerPanel.Controls.Add(richTextBoxDisplay);

            prevButton = new Button { Text = "<", Size = new Size(70, 35), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            prevButton.Click += PrevButton_Click;
            this.Controls.Add(prevButton);

            tocButton = new Button { Text = "Оглавление", Size = new Size(130, 35), Font = new Font("Segoe UI", 11F) };
            tocButton.Click += TocButton_Click;
            this.Controls.Add(tocButton);

            nextButton = new Button { Text = ">", Size = new Size(70, 35), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            nextButton.Click += NextButton_Click;
            this.Controls.Add(nextButton);

            pageInfoLabel = new Label { Text = "1 / 1", AutoSize = false, TextAlign = ContentAlignment.MiddleRight, Size = new Size(130, 30), Font = new Font("Segoe UI", 11F) };
            this.Controls.Add(pageInfoLabel);

            pageTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = Math.Max(1, _book.TotalPagesInBook),
                Value = 1,
                TickFrequency = Math.Max(1, _book.TotalPagesInBook / 20),
                AutoSize = false,
                Height = 45
            };
            pageTrackBar.Scroll += PageTrackBar_Scroll;
            this.Controls.Add(pageTrackBar);

            this.Resize += Form3_Resize;
            Form3_Resize(this, EventArgs.Empty);
        }

        private void Form3_Resize(object sender, EventArgs e)
        {
            readerPanel.Location = new Point((this.ClientSize.Width - readerPanel.Width) / 2, 20);
            int controlsY = readerPanel.Bottom + 20;

            prevButton.Location = new Point(readerPanel.Left, controlsY);
            tocButton.Location = new Point((this.ClientSize.Width - tocButton.Width) / 2, controlsY);
            nextButton.Location = new Point(readerPanel.Right - nextButton.Width, controlsY);

            pageTrackBar.Location = new Point(readerPanel.Left, controlsY + prevButton.Height + 15);
            pageTrackBar.Width = readerPanel.Width - pageInfoLabel.Width - 10;

            pageInfoLabel.Location = new Point(pageTrackBar.Right + 5, pageTrackBar.Top + (pageTrackBar.Height - pageInfoLabel.Height) / 2);
        }

        private void LoadInitialPage()
        {
            _currentChapterIndex = 0;
            _currentPageInChapterIndex = 0;
            if (_book.TotalPagesInBook == 0)
            {
                richTextBoxDisplay.Text = "Не удалось загрузить страницы книги.";
                pageInfoLabel.Text = "0 / 0";
                pageTrackBar.Enabled = false;
                prevButton.Enabled = false;
                nextButton.Enabled = false;
                tocButton.Enabled = false;
                return;
            }
            DisplayCurrentPage();
        }

        private void DisplayCurrentPage()
        {
            if (_book == null || _book.Chapters == null || !_book.Chapters.Any() || _book.TotalPagesInBook == 0)
            {
                richTextBoxDisplay.Text = "Книга не загружена или пуста.";
                return;
            }

            if (_currentChapterIndex < 0 || _currentChapterIndex >= _book.Chapters.Count) _currentChapterIndex = 0;
            BookChapter chapter = _book.Chapters[_currentChapterIndex];
            if (!chapter.PagesRtf.Any())
            {
                richTextBoxDisplay.Text = $"В главе \"{chapter.Title}\" нет страниц.";
                UpdateNavigationState();
                return;
            }
            if (_currentPageInChapterIndex < 0 || _currentPageInChapterIndex >= chapter.PagesRtf.Count) _currentPageInChapterIndex = 0;

            try
            {
                richTextBoxDisplay.Rtf = chapter.PagesRtf[_currentPageInChapterIndex];
            }
            catch (Exception ex)
            {
                richTextBoxDisplay.Text = $"Ошибка отображения страницы: {ex.Message}\n\nГлава: {chapter.Title}\nСтр. главы: {_currentPageInChapterIndex + 1}";
            }

            UpdateNavigationState();
        }

        private void UpdateNavigationState()
        {
            if (_book.TotalPagesInBook == 0) return;

            int absolutePageNumber = _book.GetAbsolutePageNumber(_currentChapterIndex, _currentPageInChapterIndex);
            pageTrackBar.Maximum = Math.Max(1, _book.TotalPagesInBook);
            pageTrackBar.Value = absolutePageNumber;
            pageInfoLabel.Text = $"{absolutePageNumber} / {_book.TotalPagesInBook}";

            this.Text = $"Читалка - {_book.Chapters[_currentChapterIndex].Title} (Стр. {_currentPageInChapterIndex + 1} из {_book.Chapters[_currentChapterIndex].PagesRtf.Count})";

            prevButton.Enabled = absolutePageNumber > 1;
            nextButton.Enabled = absolutePageNumber < _book.TotalPagesInBook;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            int currentPage = _book.GetAbsolutePageNumber(_currentChapterIndex, _currentPageInChapterIndex);
            string bookInfo = $"Файл: {_fileName}\nГлава: {_currentChapterIndex + 1}\nСтраница: {currentPage}";
            this.Tag = bookInfo;
        }

        private void PrevButton_Click(object sender, EventArgs e) => NavigatePrevious();

        private void NextButton_Click(object sender, EventArgs e) => NavigateNext();

        private void NavigatePrevious()
        {
            if (_book.TotalPagesInBook == 0) return;
            int absolutePageNumber = _book.GetAbsolutePageNumber(_currentChapterIndex, _currentPageInChapterIndex);
            if (absolutePageNumber == 1) return;

            if (_currentPageInChapterIndex > 0)
            {
                _currentPageInChapterIndex--;
            }
            else if (_currentChapterIndex > 0)
            {
                _currentChapterIndex--;
                _currentPageInChapterIndex = _book.Chapters[_currentChapterIndex].PagesRtf.Count - 1;
            }
            DisplayCurrentPage();
        }

        private void NavigateNext()
        {
            if (_book.TotalPagesInBook == 0) return;
            int absolutePageNumber = _book.GetAbsolutePageNumber(_currentChapterIndex, _currentPageInChapterIndex);
            if (absolutePageNumber == _book.TotalPagesInBook)
            {
                MessageBox.Show("Вы достигли конца книги.", "Конец книги", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_currentPageInChapterIndex < _book.Chapters[_currentChapterIndex].PagesRtf.Count - 1)
            {
                _currentPageInChapterIndex++;
            }
            else if (_currentChapterIndex < _book.Chapters.Count - 1)
            {
                _currentChapterIndex++;
                _currentPageInChapterIndex = 0;
            }
            DisplayCurrentPage();
        }

        private void TocButton_Click(object sender, EventArgs e)
        {
            if (_book.TotalPagesInBook == 0) return;
            using (Form2 tocForm = new Form2(_book.Chapters))
            {
                if (tocForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (tocForm.SelectedChapterIndex >= 0 && tocForm.SelectedChapterIndex < _book.Chapters.Count)
                    {
                        _currentChapterIndex = tocForm.SelectedChapterIndex;
                        _currentPageInChapterIndex = 0;
                        DisplayCurrentPage();
                    }
                }
            }
        }

        private void PageTrackBar_Scroll(object sender, EventArgs e)
        {
            if (_book.TotalPagesInBook == 0) return;
            int absolutePageNumber = pageTrackBar.Value;
            var (chapIdx, pageInChapIdx) = _book.GetChapterAndPageIndices(absolutePageNumber);
            _currentChapterIndex = chapIdx;
            _currentPageInChapterIndex = pageInChapIdx;
            DisplayCurrentPage();
        }
    }
}