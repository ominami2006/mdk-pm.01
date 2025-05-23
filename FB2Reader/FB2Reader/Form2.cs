using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FB2Reader
{
    /// <summary>
    /// Форма для отображения оглавления книги.
    /// </summary>
    public partial class Form2 : Form
    {
        private List<FB2Reader.TableOfContentsEntry> _tableOfContents;
        private FlowLayoutPanel flowLayoutPanel; // Используем FlowLayoutPanel для автоматического расположения кнопок

        public Form2(List<FB2Reader.TableOfContentsEntry> tableOfContents)
        {
            InitializeComponent();
            this.ClientSize = new System.Drawing.Size(600, 800);
            this.Text = "Оглавление книги";
            this.StartPosition = FormStartPosition.CenterScreen;
            _tableOfContents = tableOfContents;
            SetupUI();
        }

        /// <summary>
        /// Настройка пользовательского интерфейса формы.
        /// </summary>
        private void SetupUI()
        {
            flowLayoutPanel = new FlowLayoutPanel();
            flowLayoutPanel.Dock = DockStyle.Fill;
            flowLayoutPanel.AutoScroll = true; // Включаем прокрутку, если кнопок много
            flowLayoutPanel.FlowDirection = FlowDirection.TopDown; // Располагаем кнопки сверху вниз
            flowLayoutPanel.WrapContents = false; // Кнопки не будут переноситься на новую строку
            flowLayoutPanel.Padding = new Padding(10); // Отступы от края
            flowLayoutPanel.Margin = new Padding(10); // Отступы от края
            this.Controls.Add(flowLayoutPanel);

            if (_tableOfContents == null || _tableOfContents.Count == 0)
            {
                Label noContentLabel = new Label();
                noContentLabel.Text = "Оглавление не найдено или пусто.";
                noContentLabel.AutoSize = true;
                noContentLabel.Location = new Point(10, 10);
                flowLayoutPanel.Controls.Add(noContentLabel);
                return;
            }

            // Динамически создаем кнопки для каждого элемента оглавления
            foreach (var entry in _tableOfContents)
            {
                Button chapterButton = new Button();
                chapterButton.Text = entry.Title;
                chapterButton.Tag = entry.RtfContent; // Сохраняем RTF-содержимое в Tag кнопки
                chapterButton.AutoSize = true; // Автоматический размер по тексту
                chapterButton.MinimumSize = new Size(flowLayoutPanel.Width - 20, 30); // Ширина кнопки по ширине FlowLayoutPanel
                chapterButton.TextAlign = ContentAlignment.MiddleLeft; // Выравнивание текста по левому краю
                chapterButton.Padding = new Padding(5);
                chapterButton.Margin = new Padding(0, 0, 0, 5); // Отступ между кнопками
                chapterButton.Click += ChapterButton_Click;
                flowLayoutPanel.Controls.Add(chapterButton);
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку главы.
        /// Открывает Form3 с RTF-текстом выбранной главы.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие (кнопка главы).</param>
        /// <param name="e">Аргументы события.</param>
        private void ChapterButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null && clickedButton.Tag is string rtfContent)
            {
                Form3 chapterDisplayForm = new Form3(rtfContent, clickedButton.Text);
                chapterDisplayForm.ShowDialog(); // Показываем форму главы как модальное окно
            }
        }
    }
}
