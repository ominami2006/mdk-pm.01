using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FB2Reader
{
    public partial class Form2 : Form
    {
        private List<BookChapter> _chapters;
        private FlowLayoutPanel flowLayoutPanel;
        public int SelectedChapterIndex { get; private set; } = -1; // To return selected chapter

        public Form2(List<BookChapter> chapters)
        {
            InitializeComponent();
            _chapters = chapters;
        }

        private void SetupUI()
        {
            this.Text = "Оглавление книги";
            this.ClientSize = new System.Drawing.Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            flowLayoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            this.Controls.Add(flowLayoutPanel);

            if (_chapters == null || _chapters.Count == 0)
            {
                Label noContentLabel = new Label
                {
                    Text = "Оглавление не найдено или пусто.",
                    AutoSize = true,
                    Margin = new Padding(5)
                };
                flowLayoutPanel.Controls.Add(noContentLabel);
                return;
            }

            for (int i = 0; i < _chapters.Count; i++)
            {
                Button chapterButton = new Button
                {
                    Text = $"{i + 1}. {_chapters[i].Title}",
                    Tag = i, // Store chapter index
                    AutoSize = true,
                    MinimumSize = new Size(flowLayoutPanel.ClientSize.Width - 30, 30), // Adjust for padding/scrollbar
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8),
                    Margin = new Padding(0, 0, 0, 5),
                    Font = new Font("Segoe UI", 10F)
                };
                chapterButton.Click += ChapterButton_Click;
                flowLayoutPanel.Controls.Add(chapterButton);
            }
            
            foreach (Control ctrl in flowLayoutPanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.Width = flowLayoutPanel.ClientSize.Width - 10; // Account for padding
                }
            }
        }

        private void ChapterButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null && clickedButton.Tag is int chapterIndex)
            {
                SelectedChapterIndex = chapterIndex;
                this.DialogResult = DialogResult.OK; // Indicate selection was made
                this.Close();
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            SetupUI();
        }
    }
}