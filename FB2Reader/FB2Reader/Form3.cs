using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FB2Reader
{
    /// <summary>
    /// Форма для отображения RTF-текста одной главы.
    /// </summary>
    public partial class Form3 : Form
    {
        private string _rtfContent;
        private string _chapterTitle;
        private RichTextBox richTextBox;
        private Label loadingLabel;

        public Form3(string rtfContent, string chapterTitle)
        {
            InitializeComponent();
            _rtfContent = rtfContent;
            _chapterTitle = chapterTitle;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Text = _chapterTitle; // Заголовок формы - название главы
            this.StartPosition = FormStartPosition.CenterScreen;
            SetupUI();
            this.Load += Form3_Load; // Подписываемся на событие загрузки формы
        }
        /// <summary>
        /// Настройка пользовательского интерфейса формы.
        /// </summary>
        private void SetupUI()
        {
            // Добавляем Label для сообщения о загрузке
            loadingLabel = new Label();
            loadingLabel.Text = "Загрузка главы, пожалуйста, подождите...";
            loadingLabel.AutoSize = true;
            loadingLabel.Location = new Point(
                (this.ClientSize.Width - loadingLabel.Width) / 2,
                (this.ClientSize.Height - loadingLabel.Height) / 2
            );
            loadingLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            loadingLabel.ForeColor = Color.DarkBlue;
            loadingLabel.Anchor = AnchorStyles.None; // Центрирование
            this.Controls.Add(loadingLabel);
            loadingLabel.BringToFront(); // Убедимся, что Label находится поверх других элементов

            // Используем RichTextBox для отображения форматированного текста
            richTextBox = new RichTextBox();
            richTextBox.DetectUrls = true; // Позволяет RichTextBox обнаруживать URL-адреса
            richTextBox.ReadOnly = true;
            richTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            richTextBox.Dock = DockStyle.Fill; // Заполняет всю форму
            richTextBox.Font = new System.Drawing.Font("Segoe UI", 10); // Устанавливаем шрифт
            richTextBox.Visible = false; // Скрываем RichTextBox пока идет загрузка
            this.Controls.Add(richTextBox);
        }

        /// <summary>
        /// Обработчик события загрузки формы.
        /// Асинхронно загружает RTF-контент в RichTextBox.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие.</param>
        /// <param name="e">Аргументы события.</param>
        private async void Form3_Load(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor; // Изменяем курсор на "песочные часы"

            try
            {
                // Выполняем загрузку RTF в RichTextBox в фоновом потоке
                // Хотя сама установка .Rtf происходит в UI-потоке,
                // Task.Run здесь используется для имитации потенциально длительной подготовки данных,
                // если бы она была нужна. В данном случае, основная задержка - это сам .Rtf = ...
                // Но для больших RTF, это все равно может быть заметно.
                await Task.Run(() =>
                {
                    // Установка RTF должна происходить в UI-потоке, поэтому используем Invoke
                    if (richTextBox.InvokeRequired)
                    {
                        richTextBox.Invoke(new Action(() => richTextBox.Rtf = _rtfContent));
                    }
                    else
                    {
                        richTextBox.Rtf = _rtfContent;
                    }
                });

                // После загрузки RTF, скрываем сообщение о загрузке и показываем RichTextBox
                loadingLabel.Visible = false;
                richTextBox.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при отображении главы: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close(); // Закрываем форму, если произошла ошибка
            }
            finally
            {
                this.Cursor = Cursors.Default; // Возвращаем обычный курсор
            }
        }
    }
}
