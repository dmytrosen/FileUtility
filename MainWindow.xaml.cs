using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileInfo fileInfo = null;
        long lineSplitEvery = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string text, bool isError = false)
        {
            ListBoxItem logItem = new ListBoxItem()
            {
                Content = $"{DateTime.Now}: {text}",
                Foreground = new SolidColorBrush(isError ? Colors.Red : Colors.Navy)
            };

            History.Items.Add(logItem);
            History.ScrollIntoView(logItem);
        }

        private void Legend(string text)
        {
            TextPreview.Text += $"{text}\r\n";
        }

        private void InputSplitFileButtonClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt",
                CheckFileExists = true,
                DefaultExt = "*.csv",
                Multiselect = false,
                ShowReadOnly = true,
                Title = "Open input CSV file"
            };

            bool? result = ofd.ShowDialog();

            if (!(result ?? false))
            {
                return;
            }
            fileInfo = new FileInfo(ofd.FileName);

            StringBuilder sb = new StringBuilder();
            long linesCount = 0;
            using (StreamReader readed = new StreamReader(fileInfo.FullName))
            {
                string line = null;

                while ((line = readed.ReadLine()) != null)
                {
                    if (linesCount <= 50)
                    {
                        sb.AppendLine(line);
                    }
                    linesCount++;
                }
            }
            FileName.Content = fileInfo.FullName;
            FileSizeText.Content = $"{fileInfo.Length}.";
            LinesCountText.Content = $"{linesCount}.";
            OutputFolderName.Content = fileInfo.DirectoryName;
            if (linesCount > 1)
            {
                lineSplitEvery = (linesCount / 2);
                SplitButton.IsEnabled = true;
            }
            else
            {
                lineSplitEvery = 0;
                SplitButton.IsEnabled = false;
            }
            SplitEvery.Text = lineSplitEvery.ToString();
            if (linesCount > 0)
            {
                TextPreview.Text = sb.ToString();
            }
            else
            {
                TextPreview.Text = string.Empty;
            }
            Log($"File {fileInfo.FullName} is selected");
        }

        private void SplitButtonClick(object sender, RoutedEventArgs e)
        {
            bool hasError = false;
            string headerLine = null;
            bool copyHeader = IsCopyHeader.IsChecked ?? false;
            long linesCount = 0;
            int outputCount = 0;
            string fileName = $"{fileInfo.FullName.Substring(0, fileInfo.FullName.LastIndexOf(fileInfo.Extension))}_output_{outputCount}{fileInfo.Extension}";

            TextPreview.Text = string.Empty;
            Log($"File split starts.");
            OpenSplitFileButton.IsEnabled = false;
            SplitButton.IsEnabled = false;
            Legend("Output files:\r\n");
            if (File.Exists(fileName))
            {
                Log($"File {fileName} already exists", true);
                hasError = true;
            }
            else
            {
                StreamWriter writer = new StreamWriter(fileName);

                Log($"Output file {fileName} is created");
                Legend($"- {fileName}");

                using (StreamReader readed = new StreamReader(fileInfo.FullName))
                {
                    string line = null;

                    while ((line = readed.ReadLine()) != null)
                    {
                        if (outputCount == 0 // get header
                            && linesCount == 0
                            && copyHeader)
                        {
                            headerLine = line;
                        }
                        long.TryParse(SplitEvery.Text, out lineSplitEvery);
                        if (linesCount > lineSplitEvery)
                        {
                            // close current output
                            writer.Flush();
                            writer.Close();
                            writer.Dispose();
                            outputCount++;
                            linesCount = 0;
                            // start new output
                            fileName = $"{fileInfo.FullName.Substring(0, fileInfo.FullName.LastIndexOf(fileInfo.Extension))}_output_{outputCount}{fileInfo.Extension}";
                            if (File.Exists(fileName))
                            {
                                Log($"File {fileName} already exists", true);
                                writer = null;
                                hasError = true;
                            }
                            else
                            {
                                writer = new StreamWriter(fileName);
                                Log($"Output file {fileName} is created");
                                Legend($"- {fileName}");
                            }
                        }
                        if (outputCount > 0 // insert header in other chunks
                            && linesCount == 0
                            && copyHeader)
                        {
                            writer?.WriteLine(headerLine);
                        }
                        writer?.WriteLine(line);
                        linesCount++;
                    }
                }
                writer?.Flush();
                writer?.Close();
                writer?.Dispose();
            }
            if (!hasError)
            {
                Log($"File split is completed. {outputCount + 1} {(outputCount > 0 ? "output files were created" : "output file was created")}");
            }
            else
            {
                Log($"File split is completed with errors.", true);
            }
            OpenSplitFileButton.IsEnabled = true;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
