using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq; // Для работы с XML-документами

namespace FB2Reader
{
    /// <summary>
    /// Класс для чтения и обработки файлов формата FB2.
    /// </summary>
    public class FB2Reader
    {
        /// <summary>
        /// Представляет запись в оглавлении книги.
        /// </summary>
        public class TableOfContentsEntry
        {
            public string Title { get; set; }
            public string RtfContent { get; set; } // RTF-содержимое этой главы
            public int StartFragmentIndex { get; set; } // Для будущей навигации (пока не используется)
        }

        /// <summary>
        /// Список фрагментов текста книги.
        /// Пока не используется для навигации, но сохраняется.
        /// </summary>
        public List<string> TextFragments { get; private set; }

        /// <summary>
        /// Оглавление книги.
        /// Содержит RTF-содержимое каждой главы.
        /// </summary>
        public List<TableOfContentsEntry> TableOfContents { get; private set; }

        public FB2Reader()
        {
            TextFragments = new List<string>();
            TableOfContents = new List<TableOfContentsEntry>();
        }

        /// <summary>
        /// Читает FB2-файл по указанному пути и возвращает его содержимое в формате RTF.
        /// Также заполняет оглавление с RTF-содержимым каждой главы.
        /// </summary>
        /// <param name="filePath">Путь к FB2-файлу.</param>
        /// <returns>RTF-строка, представляющая все содержимое файла с форматированием.</returns>
        public string ReadFb2File(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл не найден.", filePath);
            }

            XDocument doc;
            try
            {
                // Загружаем XML-документ из файла
                doc = XDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Ошибка при загрузке FB2-файла. Убедитесь, что это корректный XML: {ex.Message}", ex);
            }

            // Пространство имен FB2
            XNamespace fb2Ns = "http://www.gribuser.ru/xml/fictionbook/2.0";

            // Находим корневой элемент <FictionBook>
            XElement fictionBook = doc.Element(fb2Ns + "FictionBook");
            if (fictionBook == null)
            {
                throw new FormatException("Некорректный FB2-файл: отсутствует корневой элемент <FictionBook>.");
            }

            // Находим элемент <body>, который содержит основной текст книги
            XElement body = fictionBook.Element(fb2Ns + "body");
            if (body == null)
            {
                throw new FormatException("Некорректный FB2-файл: отсутствует элемент <body>.");
            }

            StringBuilder fullRtfBuilder = new StringBuilder();
            fullRtfBuilder.Append(GetRtfHeader()); // Добавляем RTF заголовок

            int fragmentIndex = 0;

            // Рекурсивная функция для обработки элементов и извлечения текста в RTF
            // Теперь она также будет заполнять TableOfContents
            ProcessFb2BodyContent(body, fb2Ns, fullRtfBuilder, ref fragmentIndex);

            fullRtfBuilder.Append(GetRtfFooter()); // Добавляем RTF футер

            return fullRtfBuilder.ToString();
        }

        /// <summary>
        /// Генерирует стандартный RTF-заголовок.
        /// Добавлено \ansicpg1251 для лучшей поддержки кириллицы.
        /// </summary>
        private string GetRtfHeader()
        {
            // \rtf1\ansi\deff0\ansicpg1251 - стандартный заголовок с указанием кодовой страницы Windows-1251 (кириллица)
            // \nouicompat - для совместимости
            // {\fonttbl{\f0\fnil\fcharset204 Calibri;}{\f1\fnil\fcharset204 Segoe UI;}} - таблица шрифтов
            // \pard\sa200\sl276\slmult1 - параметры параграфа (spacing after, line spacing)
            // \f1\fs20\lang1049 - используем шрифт f1 (Segoe UI), размер 10pt (20 half-points), русский язык
            return @"{\rtf1\ansi\deff0\ansicpg1251\nouicompat{\fonttbl{\f0\fnil\fcharset204 Calibri;}{\f1\fnil\fcharset204 Segoe UI;}}\pard\sa200\sl276\slmult1\f1\fs20\lang1049 ";
        }

        /// <summary>
        /// Генерирует стандартный RTF-футер.
        /// </summary>
        private string GetRtfFooter()
        {
            return "}";
        }

        /// <summary>
        /// Рекурсивно обрабатывает XML-элементы внутри <body>, извлекая текст и формируя оглавление в RTF.
        /// </summary>
        /// <param name="element">Текущий XML-элемент для обработки (обычно <body> или <section>).</param>
        /// <param name="fb2Ns">Пространство имен FB2.</param>
        /// <param name="fullRtfBuilder">StringBuilder для накопления всего RTF-содержимого книги.</param>
        /// <param name="fragmentIndex">Текущий индекс фрагмента текста.</param>
        private void ProcessFb2BodyContent(XElement element, XNamespace fb2Ns, StringBuilder fullRtfBuilder, ref int fragmentIndex)
        {
            foreach (XNode node in element.Nodes())
            {
                if (node is XElement childElement)
                {
                    string tagName = childElement.Name.LocalName;

                    if (tagName == "section")
                    {
                        // Обрабатываем секцию как отдельную главу
                        XElement titleElement = childElement.Element(fb2Ns + "title");
                        string sectionTitlePlainText = string.Empty;

                        if (titleElement != null && !string.IsNullOrWhiteSpace(titleElement.Value))
                        {
                            sectionTitlePlainText = titleElement.Value.Trim();
                        }
                        else
                        {
                            // Если у секции нет title, ищем первый параграф <p> в этой секции
                            XElement firstParagraph = childElement.Descendants(fb2Ns + "p").FirstOrDefault();
                            if (firstParagraph != null && !string.IsNullOrWhiteSpace(firstParagraph.Value))
                            {
                                // Используем первую строку параграфа в качестве заголовка
                                sectionTitlePlainText = firstParagraph.Value.Trim().Split('\n')[0];
                                if (sectionTitlePlainText.Length > 100) // Обрезаем длинные заголовки
                                {
                                    sectionTitlePlainText = sectionTitlePlainText.Substring(0, 100) + "...";
                                }
                            }
                            else
                            {
                                sectionTitlePlainText = "Без названия";
                            }
                        }

                        // Создаем StringBuilder для RTF этой конкретной секции
                        StringBuilder sectionRtfBuilder = new StringBuilder();
                        sectionRtfBuilder.Append(GetRtfHeader()); // Каждая секция - это свой RTF документ

                        // Добавляем заголовок секции в RTF секции
                        if (titleElement != null) // Если был оригинальный title, форматируем его
                        {
                            sectionRtfBuilder.Append(@"\par\pard\qc\b\fs32 "); // 16pt bold centered
                            sectionRtfBuilder.Append(GetElementRtf(titleElement, fb2Ns));
                            sectionRtfBuilder.Append(@"\b0\fs20\par\pard\ql "); // Reset to default font size and left align
                        }
                        else if (!string.IsNullOrWhiteSpace(sectionTitlePlainText) && sectionTitlePlainText != "Без названия")
                        {
                            // Если заголовок взят из параграфа, форматируем его как обычный текст, но крупнее
                            sectionRtfBuilder.Append(@"\par\pard\qc\b\fs28 "); // 14pt bold centered
                            sectionRtfBuilder.Append(EscapeRtfTextAndNewlines(sectionTitlePlainText));
                            sectionRtfBuilder.Append(@"\b0\fs20\par\pard\ql "); // Reset
                        }


                        // Рекурсивно обрабатываем содержимое секции для ее собственного RTF
                        ProcessSectionContent(childElement, fb2Ns, sectionRtfBuilder, ref fragmentIndex, true);

                        sectionRtfBuilder.Append(GetRtfFooter()); // Закрываем RTF для секции

                        // Добавляем запись в оглавление
                        TableOfContents.Add(new TableOfContentsEntry
                        {
                            Title = sectionTitlePlainText,
                            RtfContent = sectionRtfBuilder.ToString(), // Сохраняем RTF для этой главы
                            StartFragmentIndex = fragmentIndex // Индекс фрагмента для будущей навигации
                        });

                        // Добавляем RTF этой секции в общий RTF книги
                        fullRtfBuilder.Append(sectionRtfBuilder.ToString());
                    }
                    else
                    {
                        // Для других элементов, не являющихся секциями, просто обрабатываем их как часть общего потока
                        ProcessSectionContent(childElement, fb2Ns, fullRtfBuilder, ref fragmentIndex, false);
                    }
                }
                else if (node is XText textNode)
                {
                    // Добавляем необработанный текст напрямую, если он есть
                    string text = EscapeRtfTextAndNewlines(textNode.Value).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        fullRtfBuilder.Append(text);
                    }
                }
            }
        }

        /// <summary>
        /// Рекурсивно обрабатывает XML-элементы внутри секции или другого блочного элемента.
        /// </summary>
        /// <param name="element">Текущий XML-элемент для обработки.</param>
        /// <param name="fb2Ns">Пространство имен FB2.</param>
        /// <param name="rtfBuilder">StringBuilder для накопления RTF-содержимого.</param>
        /// <param name="fragmentIndex">Текущий индекс фрагмента текста.</param>
        /// <param name="isSectionContent">Указывает, обрабатывается ли содержимое секции (для избежания рекурсивного создания новых секций).</param>
        private void ProcessSectionContent(XElement element, XNamespace fb2Ns, StringBuilder rtfBuilder, ref int fragmentIndex, bool isSectionContent)
        {
            foreach (XNode node in element.Nodes())
            {
                if (node is XElement childElement)
                {
                    string tagName = childElement.Name.LocalName;

                    if (isSectionContent && tagName == "section")
                    {
                        // Если мы уже внутри секции и встречаем вложенную секцию,
                        // обрабатываем ее содержимое как часть текущей секции для RTF,
                        // но не создаем новую запись в оглавлении (это будет сделано родительской ProcessFb2BodyContent).
                        ProcessSectionContent(childElement, fb2Ns, rtfBuilder, ref fragmentIndex, true);
                        continue; // Пропустить дальнейшую обработку в этом switch
                    }

                    switch (tagName)
                    {
                        case "p":
                            string paragraphRtf = GetElementRtf(childElement, fb2Ns).Trim();
                            if (!string.IsNullOrEmpty(paragraphRtf))
                            {
                                rtfBuilder.Append(@"\par "); // Новый параграф
                                rtfBuilder.Append(paragraphRtf);
                                TextFragments.Add(childElement.Value.Trim()); // Сохраняем plain text фрагмент
                                fragmentIndex++;
                            }
                            break;
                        case "subtitle":
                            string subtitleRtf = GetElementRtf(childElement, fb2Ns).Trim();
                            if (!string.IsNullOrEmpty(subtitleRtf))
                            {
                                rtfBuilder.Append(@"\par\pard\qc\b\fs28 "); // 14pt bold centered
                                rtfBuilder.Append(subtitleRtf);
                                rtfBuilder.Append(@"\b0\fs20\par\pard\ql "); // Reset
                                TextFragments.Add(childElement.Value.Trim());
                                fragmentIndex++;
                            }
                            break;
                        case "epigraph":
                            string epigraphRtf = GetElementRtf(childElement, fb2Ns).Trim();
                            if (!string.IsNullOrEmpty(epigraphRtf))
                            {
                                rtfBuilder.Append(@"\par\pard\li720\ri720\i "); // Indent 1 inch, italic
                                rtfBuilder.Append(epigraphRtf);
                                rtfBuilder.Append(@"\i0\par\pard\ql "); // Reset
                                TextFragments.Add(childElement.Value.Trim());
                                fragmentIndex++;
                            }
                            break;
                        case "text-author":
                            string authorRtf = GetElementRtf(childElement, fb2Ns).Trim();
                            if (!string.IsNullOrEmpty(authorRtf))
                            {
                                rtfBuilder.Append(@"\par\pard\qr\i "); // Right align and italic
                                rtfBuilder.Append(authorRtf);
                                rtfBuilder.Append(@"\i0\par\pard\ql "); // Reset
                                TextFragments.Add(childElement.Value.Trim());
                                fragmentIndex++;
                            }
                            break;
                        case "poem":
                            string poemRtf = GetElementRtf(childElement, fb2Ns).Trim();
                            if (!string.IsNullOrEmpty(poemRtf))
                            {
                                // FB2 poem can have nested <stanza> and <v> (verse)
                                // For simplicity, we'll just indent the whole poem block.
                                rtfBuilder.Append(@"\par\pard\li360\ri360 "); // Indent for poem (0.5 inch)
                                rtfBuilder.Append(poemRtf);
                                rtfBuilder.Append(@"\par\pard\ql "); // Reset
                                TextFragments.Add(childElement.Value.Trim());
                                fragmentIndex++;
                            }
                            break;
                        // Добавьте другие элементы блочного уровня, если необходимо (например, <table>, <img>)
                        default:
                            // Рекурсивно обрабатываем дочерние элементы, если они не являются специальными
                            ProcessSectionContent(childElement, fb2Ns, rtfBuilder, ref fragmentIndex, isSectionContent);
                            break;
                    }
                }
                else if (node is XText textNode)
                {
                    // Добавляем необработанный текст напрямую, если он есть
                    string text = EscapeRtfTextAndNewlines(textNode.Value).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        rtfBuilder.Append(text);
                    }
                }
            }
        }

        /// <summary>
        /// Извлекает весь текстовый контент из элемента, включая вложенные элементы, в формате RTF.
        /// Обрабатывает inline-форматирование (strong, emphasis).
        /// </summary>
        /// <param name="element">XML-элемент.</param>
        /// <param name="fb2Ns">Пространство имен FB2.</param>
        /// <returns>RTF-строка, содержащаяся в элементе.</returns>
        private string GetElementRtf(XElement element, XNamespace fb2Ns)
        {
            StringBuilder sb = new StringBuilder();
            foreach (XNode node in element.Nodes())
            {
                if (node is XElement childElement)
                {
                    string tagName = childElement.Name.LocalName;
                    switch (tagName)
                    {
                        case "strong":
                            sb.Append(@"\b "); // Включить жирный шрифт
                            sb.Append(GetElementRtf(childElement, fb2Ns));
                            sb.Append(@"\b0 "); // Выключить жирный шрифт
                            break;
                        case "emphasis": // FB2 использует <emphasis> для курсива
                            sb.Append(@"\i "); // Включить курсив
                            sb.Append(GetElementRtf(childElement, fb2Ns));
                            sb.Append(@"\i0 "); // Выключить курсив
                            break;
                        // Добавьте другие inline-элементы, если необходимо
                        default:
                            // Для других элементов просто рекурсивно обрабатываем их содержимое
                            sb.Append(GetElementRtf(childElement, fb2Ns));
                            break;
                    }
                }
                else if (node is XText textNode)
                {
                    // Экранируем специальные символы RTF и преобразуем переносы строк
                    sb.Append(EscapeRtfTextAndNewlines(textNode.Value));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Экранирует специальные символы RTF и преобразует символы новой строки в RTF-команду \line.
        /// </summary>
        /// <param name="text">Входной текст.</param>
        /// <returns>Экранированный текст с RTF-переносами строк.</returns>
        private string EscapeRtfTextAndNewlines(string text)
        {
            // Сначала экранируем специальные символы RTF
            string escapedText = text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
            // Затем заменяем символы новой строки на RTF-перенос строки
            escapedText = escapedText.Replace("\n", "\\line ");
            // Удаляем символы возврата каретки, так как \n обычно достаточно
            return escapedText.Replace("\r", "");
        }
    }
}
