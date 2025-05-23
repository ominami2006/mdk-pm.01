using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks; // Для асинхронных операций
using System.Windows.Forms; // Для работы с Windows Forms
using System.Drawing; // Для работы с цветом и размером шрифта

namespace FB2Reader
{
    /// <summary>
    /// Главная форма приложения FB2Reader.
    /// Отвечает за выбор FB2-файла и передачу управления форме оглавления.
    /// </summary>
    public partial class Form1 : Form
    {
        private Button openFileButton; // Объявляем кнопку на уровне класса

        public Form1()
        {
            InitializeComponent(); // Инициализация компонентов формы, созданных дизайнером
            SetupUI(); // Дополнительная настройка UI
        }

        /// <summary>
        /// Метод для программной настройки элементов UI.
        /// </summary>
        private void SetupUI()
        {
            // Создаем новую кнопку
            openFileButton = new Button();
            openFileButton.Text = "Открыть FB2 файл";
            openFileButton.Location = new System.Drawing.Point(10, 10); // Позиция кнопки
            openFileButton.Size = new System.Drawing.Size(150, 30); // Размер кнопки
            openFileButton.Click += OpenFileButton_Click; // Подписываемся на событие Click

            // Добавляем кнопку на форму
            this.Controls.Add(openFileButton);

            this.Text = "FB2 Reader"; // Заголовок окна
            this.Size = new System.Drawing.Size(400, 200); // Размер окна
            this.StartPosition = FormStartPosition.CenterScreen; // Размещаем окно по центру экрана
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Открыть FB2 файл".
        /// Метод теперь асинхронный для предотвращения зависания UI.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие.</param>
        /// <param name="e">Аргументы события.</param>
        private async void OpenFileButton_Click(object sender, EventArgs e)
        {
            // Создаем экземпляр OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Устанавливаем фильтр для файлов FB2
            openFileDialog.Filter = "FB2 Files (*.fb2)|*.fb2|All Files (*.*)|*.*";
            openFileDialog.Title = "Выберите файл FB2"; // Заголовок диалогового окна

            // Показываем диалоговое окно и проверяем результат
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName; // Получаем выбранный путь к файлу

                // Показываем индикатор загрузки и отключаем кнопку
                this.Cursor = Cursors.WaitCursor; // Изменяем курсор на "песочные часы"
                openFileButton.Enabled = false; // Отключаем кнопку, чтобы предотвратить повторное нажатие

                // Создаем временную форму для отображения сообщения о загрузке
                Form loadingForm = new Form();
                loadingForm.Text = "Загрузка...";
                loadingForm.Size = new Size(300, 150);
                loadingForm.StartPosition = FormStartPosition.CenterScreen;
                loadingForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                loadingForm.ControlBox = false; // Без кнопок закрытия/сворачивания

                Label loadingLabel = new Label();
                loadingLabel.Text = "Загрузка файла, пожалуйста, подождите...";
                loadingLabel.AutoSize = true;
                loadingLabel.Location = new Point(
                    (loadingForm.ClientSize.Width - loadingLabel.Width) / 2,
                    (loadingForm.ClientSize.Height - loadingLabel.Height) / 2
                );
                loadingLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                loadingLabel.ForeColor = Color.DarkBlue;
                loadingLabel.Anchor = AnchorStyles.None;
                loadingForm.Controls.Add(loadingLabel);
                loadingForm.Show(); // Показываем форму загрузки
                loadingForm.Refresh(); // Обновляем форму, чтобы сообщение сразу отобразилось

                try
                {
                    // Выполняем чтение FB2-файла в фоновом потоке
                    FB2Reader reader = await Task.Run(() =>
                    {
                        FB2Reader fb2Reader = new FB2Reader();
                        fb2Reader.ReadFb2File(filePath); // Читаем файл и заполняем оглавление
                        return fb2Reader;
                    });

                    // После успешной загрузки, закрываем форму загрузки
                    loadingForm.Close();

                    // Открываем Form2 для отображения оглавления
                    Form2 tableOfContentsForm = new Form2(reader.TableOfContents);
                    tableOfContentsForm.ShowDialog(); // Показываем как модальное окно

                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка: Файл не найден", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (FormatException ex)
                {
                    MessageBox.Show($"Некорректный формат FB2 файла: {ex.Message}", "Ошибка формата", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // Сбрасываем состояние UI после завершения операции (даже если произошла ошибка)
                    this.Cursor = Cursors.Default; // Возвращаем обычный курсор
                    openFileButton.Enabled = true; // Включаем кнопку обратно
                    if (loadingForm.Visible) // Убедимся, что форма загрузки закрыта, если она все еще открыта
                    {
                        loadingForm.Close();
                    }
                }
            }
        }
    }
}
