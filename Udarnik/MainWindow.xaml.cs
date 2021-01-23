using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using MahApps.Metro.Controls;

namespace Udarnik
{
    public partial class MainWindow : MetroWindow
    {
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "Udarnik";
        //Buffer
        private string buff;

        private string writePath = "save.txt";

        public string spreadsheetId;
        //Картинки
        private List<string> imgfiles = new List<string>();
        //Лоты в таблице
        private List<string> rowfiles = new List<string>();
        private String range;
        private SheetsService service;


        public MainWindow()
        {
            InitializeComponent();
            box_update(); //Выводит прошлые данные
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }
        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Clear();
            listBox1.Items.Clear();
            if (checkBox.IsChecked == true)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(writePath, append: false, Encoding.UTF8))
                    {
                        await sw.WriteLineAsync(textBox.Text);
                        await sw.WriteLineAsync(textBox1.Text);
                        await sw.WriteLineAsync(textBox2.Text);
                        await sw.WriteLineAsync(comboBox.SelectedIndex.ToString());
                        await sw.WriteLineAsync(comboBox1.SelectedIndex.ToString());
                    }
                    Console.WriteLine("Запись выполнена");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            try
            {
                if (textBox.Text != "" && textBox1.Text != "" && textBox2.Text != "")
                {
                    //Поиск фото в папке
                    //С помощью GetFiles записывает в List<string> imgfiles наименование изображений
                    imgfiles = GetFiles(textBox1.Text, "*.jpg|*.png|*.jpeg", SearchOption.TopDirectoryOnly);
                    if (imgfiles.Count == 0)
                    {
                        System.Windows.MessageBox.Show("В папке нет изображений");
                        return;
                    }
                    label4.Content = imgfiles.Count.ToString();
                    //Проверка URL
                    sheetId();
                    getTab();
                    for (int i = 0; i < rowfiles.Count; i++)
                    {
                        rowfiles[i] = rowfiles[i].Replace(" ", "");
                        if (!imgfiles.Contains(rowfiles[i]))
                        {
                            rowfiles[i] = rowfiles[i].Replace("-", "/");
                            listBox.Items.Add(rowfiles[i]);
                        }
                    }
                    //кол-во не найденых
                    label2.Content = listBox.Items.Count.ToString();
                    for (int i = 0; i < imgfiles.Count; i++)
                    {
                        if (!rowfiles.Contains(imgfiles[i]))
                        {
                            listBox1.Items.Add(imgfiles[i]);
                        }
                    }
                    imgfiles.Clear();
                    rowfiles.Clear();
                    if (listBox.Items.Count == 0)
                    {
                        System.Windows.MessageBox.Show("Выбранный лист или столбец пустой");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Одна или несколько форм не заполнены");
                }
            }
            catch (GoogleApiException)
            {
                System.Windows.MessageBox.Show("Неправильно введено URL или наименование Листа");
            }
            catch (DirectoryNotFoundException)
            {
                System.Windows.MessageBox.Show("Неверно введен путь к фото");
            }
        }
        //Открытие проводника для фото
        private void button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            try
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textBox1.Text = dialog.SelectedPath;
                }
            }
            finally
            {
                ((IDisposable)(object)dialog)?.Dispose();
            }
        }
        //С помощью GetFiles записывает в List<string> imgfiles наименование изображений
        public List<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            List<string> files = new List<string>();
            string[] array = searchPattern.Split('|');
            string[] array2 = array;
            foreach (string sp in array2)
            {
                files.AddRange(Directory.GetFiles(path, sp, searchOption));
            }
            //Перебор files
            for (int i = 0; i < files.Count; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i])!.Replace(" ", "");
                string bfiles = "";
                //Посимвольный перебор
                for (int b = 0; b < files[i].Length && files[i][b] != '('; b++)
                {
                    bfiles += files[i][b];
                }
                files[i] = bfiles;
            }
            //Сортировкам по алфавиту
            files.Sort();
            return files.Distinct().ToList(); ;
        }
        //Очистить данные
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            textBox.Text = "";
            textBox1.Text = "";
            textBox2.Text = "";
            comboBox.SelectedIndex = 1;
            comboBox1.SelectedIndex = 1;
        }

        //Взять сохранение, если есть
        private void box_update()
        {
            try
            {
                using (new StreamWriter(writePath, append: true, Encoding.UTF8))
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (File.ReadAllLines(writePath).Length == 5)
            {
                textBox.Text = File.ReadAllLines(writePath)[0];
                textBox1.Text = File.ReadAllLines(writePath)[1];
                textBox2.Text = File.ReadAllLines(writePath)[2];
                comboBox.SelectedIndex = int.Parse(File.ReadAllLines(writePath)[3]);
                comboBox1.SelectedIndex = int.Parse(File.ReadAllLines(writePath)[4]);
            }
        }
        //Проверка URL
        private void sheetId()
        {
            spreadsheetId = "";
            for (int i = 0; i < textBox.Text.Length; i++)
            {
                if (textBox.Text[i] == '/' && textBox.Text[i + 1] == 'd' && textBox.Text[i + 2] == '/')
                {
                    for (i += 3; textBox.Text[i] != '/'; i++)
                    {
                        spreadsheetId += textBox.Text[i];
                    }
                    break;
                }
            }
        }
        //Взятие данных из указанной таблицы
        private void getTab()
        {

            // Define request parameters.
            range = "'" + textBox2.Text + "'!" + comboBox.Text + comboBox1.Text + ":" + comboBox.Text;
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values != null && values.Count > 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    for (int j = 0; j < values[i].Count; j++)
                    {
                        rowfiles.Add(values[i][j].ToString().Replace("/", "-"));
                    }
                }
                /*foreach (var row in values)
                {
                    // Print columns A and E, which correspond to indices 0 and 4.
                        rowfiles.Add(row.ToString().Replace("/", "-"));
                }*/
            }
            else
            {
                System.Windows.MessageBox.Show("No data found.");
            }
        }


        //Скопировать выведенные данные
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.Items.Count > 0 || listBox1.Items.Count > 0)
            {
                System.Windows.Clipboard.Clear();
                buff = "";
                if (listBox.Items.Count > 0)
                {
                    for (int i = 0; i < listBox.Items.Count; i++)
                    {
                        buff = buff + listBox.Items[i]?.ToString() + " \n";
                    }
                }
                if (listBox1.Items.Count > 0)
                {
                    buff = buff + "Нет в таблице: \n";
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        buff = buff + listBox1.Items[i]?.ToString() + " \n";
                    }
                }
                System.Windows.Clipboard.SetText(buff);
            }
        }
        //Подсчет сфотографированных лотов в папке
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text.Length != 0)
            {
                imgfiles = GetFiles(textBox1.Text, "*.jpg|*.png|*.jpeg", SearchOption.TopDirectoryOnly);
                if (imgfiles.Count == 0)
                {
                    System.Windows.MessageBox.Show("В папке нет изображений");
                    return;
                }
                label4.Content = imgfiles.Count.ToString();
            }
            else
            {
                System.Windows.MessageBox.Show("Введите путь к изображениям");
                return;
            }
            imgfiles.Clear();
        }
        //Поиск дублей
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBox.Items.Clear();
                listBox1.Items.Clear();
                if (textBox.Text != "")
                {
                    sheetId();
                    getTab();
                    for (int i = 0; i < rowfiles.Count; i++)
                    {
                        for (int b = i + 1; b < rowfiles.Count; b++)
                        {
                            if (rowfiles[i] == rowfiles[b] && !listBox.Items.Contains(rowfiles[b]))
                            {
                                listBox.Items.Add(rowfiles[i].Replace("-", "/"));
                            }
                        }
                    }
                    rowfiles.Clear();
                    if (listBox.Items.Count == 0)
                    {
                        System.Windows.MessageBox.Show("Дубликатов нет");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Нет ссылки");
                }

            }
            catch (GoogleApiException)
            {
                System.Windows.MessageBox.Show("Неправильно введено URL или наименование Листа");
            }
        }

        private void listBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listBox.SelectedItem != null)
            {
                System.Windows.Clipboard.Clear();
                buff = listBox.SelectedItem.ToString();
                System.Windows.Clipboard.SetText(buff);
            }

        }
        private void listBox1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                System.Windows.Clipboard.Clear();
                buff = listBox1.SelectedItem.ToString();
                System.Windows.Clipboard.SetText(buff);
            }
        }
    }
}
