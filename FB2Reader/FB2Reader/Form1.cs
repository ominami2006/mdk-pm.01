using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FB2Reader
{
    public partial class Form1 : Form
    {
        private Button openFileButton;
        private Label statusLabel;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "FB2 Reader - Выбор файла";
            this.Size = new System.Drawing.Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;

            openFileButton = new Button
            {
                Text = "Открыть FB2 файл",
                Location = new System.Drawing.Point((this.ClientSize.Width - 200) / 2, 30),
                Size = new System.Drawing.Size(200, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            };
            openFileButton.Click += OpenFileButton_Click;
            this.Controls.Add(openFileButton);

            statusLabel = new Label
            {
                Text = "Выберите FB2 файл для чтения.",
                Location = new System.Drawing.Point(10, openFileButton.Bottom + 20),
                Size = new System.Drawing.Size(this.ClientSize.Width - 20, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F),
            };
            this.Controls.Add(statusLabel);
        }

        private async void OpenFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "FB2 Files (*.fb2)|*.fb2|All Files (*.*)|*.*",
                Title = "Выберите файл FB2"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                openFileButton.Enabled = false;
                statusLabel.Text = "Загрузка и обработка книги...\nПожалуйста, подождите.";
                this.Cursor = Cursors.WaitCursor;

                BookDocument book = new BookDocument();
                bool success = false;

                try
                {
                    success = await Task.Run(() => book.LoadAndProcessFile(filePath, new Size(760, 580)));

                    if (success && book.Chapters.Any() && book.TotalPagesInBook > 0)
                    {
                        Form3 readerForm = new Form3(book);
                        this.Hide();
                        readerForm.ShowDialog();
                        this.Close();
                    }
                    else
                    {
                        statusLabel.Text = "Не удалось загрузить или обработать книгу.\nВозможно, файл поврежден или пуст.";
                        MessageBox.Show("Не удалось загрузить книгу. Убедитесь, что файл корректен, содержит текст и поддается разбивке на страницы.", "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Ошибка при загрузке: {ex.Message}";
                    MessageBox.Show($"Произошла ошибка: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    openFileButton.Enabled = true;
                    this.Cursor = Cursors.Default;
                    if (!success || (book.Chapters.Count == 0 || book.TotalPagesInBook == 0))
                    {
                        statusLabel.Text = "Выберите FB2 файл для чтения.";
                    }
                }
            }
        }
    }
}