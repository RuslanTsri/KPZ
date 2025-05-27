using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Lab6.Services;

namespace Lab6
{
    public partial class MainWindow : Window
    {
        private readonly QRCodeService _qrService = new QRCodeService();
        private string _lastGeneratedQRCodePath;
        private readonly DatabaseService _dbService = new DatabaseService();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Tag == null) // Зберігаємо початковий текст як Tag
            {
                textBox.Tag = textBox.Text; // Зберігаємо початковий текст (плейсхолдер) у Tag
                textBox.Text = ""; // Очищаємо поле
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag?.ToString(); // Повертаємо плейсхолдер з Tag, якщо поле пусте
                textBox.Tag = null; // Очищаємо Tag
            }
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Перевіряємо, чи елемент, який має фокус, є TextBox
            if (Keyboard.FocusedElement is TextBox focusedTextBox)
            {
                // Перевіряємо, чи клікнули не по самому TextBox
                // Якщо клікнули по TextBox, то фокус вже не зніматимемо
                // e.OriginalSource представляє елемент, який був клікнутий
                if (!focusedTextBox.IsMouseOver)
                {
                    // Примусово переміщуємо фокус на головний Grid
                    // Це знімає фокус з будь-якого поточного елемента, що має фокус.
                    // Важливо: Grid повинен бути Focusable=true, але для більшості панелей це працює за замовчуванням.
                    // Якщо не працює, можна встановити Keyboard.Focus(this); (фокус на вікно)
                    Keyboard.ClearFocus(); // Більш загальний спосіб зняти фокус
                    e.Handled = true; // Позначаємо подію як оброблену, щоб вона не поширювалась далі
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _dbService.InitializeDatabase();
            ResetVisibility(); 

            if (QRCodeTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string type = selectedItem.Content.ToString();
                if (type == "Text" || type == "URL")
                {
                    SafeSetVisibility(ContentTextBox, Visibility.Visible);
                }
            }
        }

private void ClearQRCodeImage(object sender, RoutedEventArgs e)
{
    QRCodeImage.Source = null;
    _lastGeneratedQRCodePath = null;
    StatusText.Text = "Зображення очищено";
}

private void CopyQRCodePath(object sender, RoutedEventArgs e)
{
    if (!string.IsNullOrEmpty(_lastGeneratedQRCodePath))
    {
        Clipboard.SetText(_lastGeneratedQRCodePath);
        StatusText.Text = "Шлях скопійовано до буфера обміну";
    }
    else
    {
        StatusText.Text = "Немає шляху для копіювання";
    }
}

        private void GenerateQRCode(object sender, RoutedEventArgs e)
        {
            if (ContentTextBox == null)
            {
                MessageBox.Show("Помилка: Поле Content не ініціалізовано.", "Помилка");
                return;
            }

            string content = ContentTextBox.Text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Заповніть поле Content");
                return;
            }

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            Directory.CreateDirectory(folderPath);

            string fileName = $"{Guid.NewGuid()}.png";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                _qrService.GenerateQRCode(content, filePath);
                _dbService.SaveQRCode(content, filePath);
                _lastGeneratedQRCodePath = filePath;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                QRCodeImage.Source = bitmap;
                StatusText.Text = $"QR-код успішно згенеровано: {DateTime.Now:HH:mm:ss}";
                MessageBox.Show("QR-код згенеровано і збережено!");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Помилка: {ex.Message}";
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка");
            }
        }

        private void ViewSavedQRCodes(object sender, RoutedEventArgs e)
        {
            List<string> list = _dbService.GetAllQRCodes();
            MessageBox.Show(list.Count == 0
                                ? "Історія порожня"
                                : string.Join(Environment.NewLine, list),
                            "History");
        }

        private void ClearHistory(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистити всі записи?", "Confirm",
                                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _dbService.ClearAllQRCodes();
                MessageBox.Show("Історію очищено");
            }
        }

        private void QRCodeTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetVisibility();

            if (QRCodeTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string type = selectedItem.Content.ToString();
                StatusText.Text = $"Обрано тип QR-коду: {type}";
                switch (type)
                {
                    case "Text":
                    case "URL":
                        SafeSetVisibility(ContentTextBox, Visibility.Visible);
                        break;
                    case "Wi-Fi":
                        SafeSetVisibility(SSIDTextBox, Visibility.Visible);
                        SafeSetVisibility(PasswordTextBox, Visibility.Visible);
                        break;
                    case "Email":
                        SafeSetVisibility(EmailTextBox, Visibility.Visible);
                        SafeSetVisibility(SubjectTextBox, Visibility.Visible);
                        break;
                    case "Contact":
                        SafeSetVisibility(NameTextBox, Visibility.Visible);
                        SafeSetVisibility(PhoneTextBox, Visibility.Visible);
                        break;
                }
            }
        }

        private void ResetVisibility()
        {
            SafeSetVisibility(ContentTextBox, Visibility.Collapsed);
            SafeSetVisibility(SSIDTextBox, Visibility.Collapsed);
            SafeSetVisibility(PasswordTextBox, Visibility.Collapsed);
            SafeSetVisibility(EmailTextBox, Visibility.Collapsed);
            SafeSetVisibility(SubjectTextBox, Visibility.Collapsed);
            SafeSetVisibility(NameTextBox, Visibility.Collapsed);
            SafeSetVisibility(PhoneTextBox, Visibility.Collapsed);
        }

        private void SafeSetVisibility(UIElement element, Visibility visibility)
        {
            if (element != null)
            {
                element.Visibility = visibility;
            }
        }
    }
}
