using FileUtility.Entities;
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
        private bool loaded = false;
        private FileInfo fileInfo = null;
        private long totalLines = 0;
        private ESplitFileType splitFileType = ESplitFileType.NotSet;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, a) =>
            {
                loaded = true;
                splitFileType = ESplitFileType.SplitByNumberOfRows;
                UpdateUi();
            };
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
            totalLines = 0;

            StringBuilder sb = new StringBuilder();

            using (StreamReader readed = new StreamReader(fileInfo.FullName))
            {
                string line = null;

                while ((line = readed.ReadLine()) != null)
                {
                    if (totalLines <= 50)
                    {
                        sb.AppendLine(line);
                    }
                    totalLines++;
                }
            }
            FileName.Content = fileInfo.FullName;
            FileSizeText.Text = $"{fileInfo.Length}";
            LinesCountText.Text = $"{totalLines}";
            OutputFolderName.Content = fileInfo.DirectoryName;
            if (totalLines > 1)
            {
                SplitEvery.Text = (totalLines / 2).ToString();
                SplitButton.IsEnabled = true;
            }
            else
            {
                SplitEvery.Text = "0";
                SplitButton.IsEnabled = false;
            }
            if (totalLines > 0)
            {
                TextPreview.Text = sb.ToString();
                FilesSplitNumber.Text = (totalLines > 2 ? 2 : 1).ToString();
                ExtractRequiredRowsFrom.Text = "1";
                ExtractRequiredRowsTo.Text = totalLines.ToString();
            }
            else
            {
                TextPreview.Text = string.Empty;
                FilesSplitNumber.Text = "0";
                ExtractRequiredRowsFrom.Text = "0";
                ExtractRequiredRowsTo.Text = "0";
            }
            Log($"File {fileInfo.FullName} is selected");
        }

        private void SplitButtonClick(object sender, RoutedEventArgs e)
        {
            bool hasError = false;
            bool copyHeader = IsCopyHeader.IsChecked ?? false;
            long lineSplitEvery = 0;
            int fileSplitNumber = 0;
            long extractRowsFrom = 0;
            long extractRowsTo = 0;
            int outputFileNumber = 0;
            string fileNameBase = $"{fileInfo.FullName.Substring(0, fileInfo.FullName.LastIndexOf(fileInfo.Extension))}_output_{{0}}{fileInfo.Extension}";
            string fileName = string.Format(fileNameBase, outputFileNumber);

            TextPreview.Text = string.Empty;
            Log($"File processing starts.");
            OpenSplitFileButton.IsEnabled = false;
            SplitButton.IsEnabled = false;
            Legend("Output files:\r\n");
            if (File.Exists(fileName))
            {
                Log($"File {fileName} already exists", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfRows
                && !long.TryParse(SplitEvery.Text, out lineSplitEvery))
            {
                Log($"Invalid number of 'Split at every' {SplitEvery.Text}. Must be positive number.", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfRows
                && lineSplitEvery > totalLines - 1)
            {
                Log($"'Split at every' must be set to positive number less than or equal to {totalLines - 1}", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfRows
                && lineSplitEvery < totalLines / 100)
            {
                Log($"'Split at every' must be set to positive number greater then or equal to {totalLines / 100}", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfEqualFiles
                && !int.TryParse(FilesSplitNumber.Text, out fileSplitNumber))
            {
                Log($"Invalid number of 'Split at every' {FilesSplitNumber.Text}. Must be positive number.", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfEqualFiles
                && fileSplitNumber > 10)
            {
                Log($"'Number of split files' must be set to positive number less then or equal to 10", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfEqualFiles
                && fileSplitNumber < 2)
            {
                Log($"'Number of split files' must be set to positive number greater then or equal to 2", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.ExtractRequiredRows
                && !long.TryParse(ExtractRequiredRowsFrom.Text, out extractRowsFrom))
            {
                Log($"Invalid number of 'Extract rows from' {ExtractRequiredRowsFrom.Text}. Must be positive number.", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.ExtractRequiredRows
                && !long.TryParse(ExtractRequiredRowsTo.Text, out extractRowsTo))
            {
                Log($"Invalid number of 'Extract rows to' {ExtractRequiredRowsTo.Text}. Must be positive number.", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.ExtractRequiredRows
                && extractRowsFrom > extractRowsTo)
            {
                Log($"'Extract rows from' {extractRowsFrom} must be less or equal to 'Extract rows to' {extractRowsTo}", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.ExtractRequiredRows
                && extractRowsFrom < 1)
            {
                Log($"'Extract rows from' {extractRowsFrom} must be positive number.", true);
                hasError = true;
            }
            else if (splitFileType == ESplitFileType.ExtractRequiredRows
                && extractRowsTo > totalLines)
            {
                Log($"'Extract rows to' {extractRowsTo} must be less or equal to {totalLines}.", true);
                hasError = true;
            }
            else
            {
                if (splitFileType == ESplitFileType.SplitByNumberOfRows) // by rows number
                {
                    hasError = SplitByRows(fileName, fileNameBase, copyHeader, lineSplitEvery, ref outputFileNumber);
                }
                else if (splitFileType == ESplitFileType.SplitByNumberOfEqualFiles) // by files number
                {
                    hasError = SplitByFiles(fileName, fileNameBase, copyHeader, fileSplitNumber, ref outputFileNumber);
                }
                else if (splitFileType == ESplitFileType.ExtractRequiredRows) // extract rows
                {
                    ExtractRequiredRows(fileName, copyHeader, extractRowsFrom, extractRowsTo);
                }
            }
            if (!hasError)
            {
                Log($"File processing is completed. {outputFileNumber + 1} {(outputFileNumber > 0 ? "output files were created" : "output file was created")}");
            }
            else
            {
                Log($"File processing is completed with errors.", true);
                if (outputFileNumber == 0)
                {
                    SplitButton.IsEnabled = true;
                }
            }
            OpenSplitFileButton.IsEnabled = true;
        }

        private void ExtractRequiredRows(string fileName, bool copyHeader, long fromRow, long toRow)
        {
            long splitLinesCount = -1;

            Log($"Output file {fileName} is created");
            Legend($"- {fileName}");
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                using (StreamReader readed = new StreamReader(fileInfo.FullName))
                {
                    string line = null;

                    while ((line = readed.ReadLine()) != null)
                    {
                        splitLinesCount++;
                        if (splitLinesCount == 0
                            && copyHeader)
                        {
                            writer.WriteLine(line);
                        }
                        if (splitLinesCount < fromRow)
                        {
                            continue;
                        }
                        if (splitLinesCount >= toRow)
                        {
                            break;
                        }
                        writer.WriteLine(line);
                    }
                }
            }
        }

        private bool SplitByFiles(string fileName, string fileNameBase, bool copyHeader, long fileSplitNumber, ref int outputFileNumber)
        {
            return SplitByRows(fileName, fileNameBase, copyHeader, totalLines / fileSplitNumber, ref outputFileNumber);
        }

        private bool SplitByRows(string fileName, string fileNameBase, bool copyHeader, long lineSplitEvery, ref int outputFileNumber)
        {
            bool hasError = false;
            string headerLine = null;
            long splitLinesCount = 0;
            StreamWriter writer = new StreamWriter(fileName);

            Log($"Output file {fileName} is created");
            Legend($"- {fileName}");
            using (StreamReader readed = new StreamReader(fileInfo.FullName))
            {
                string line = null;

                while ((line = readed.ReadLine()) != null)
                {
                    if (outputFileNumber == 0 // get header
                        && splitLinesCount == 0
                        && copyHeader)
                    {
                        headerLine = line;
                    }
                    if (splitLinesCount > lineSplitEvery)
                    {
                        // close current output
                        writer.Flush();
                        writer.Close();
                        writer.Dispose();
                        outputFileNumber++;
                        splitLinesCount = 0;
                        // start new output
                        fileName = string.Format(fileNameBase, outputFileNumber);
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
                    if (outputFileNumber > 0 // insert header in other chunks
                        && splitLinesCount == 0
                        && copyHeader)
                    {
                        writer?.WriteLine(headerLine);
                    }
                    writer?.WriteLine(line);
                    splitLinesCount++;
                }
            }
            writer?.Flush();
            writer?.Close();
            writer?.Dispose();
            return hasError;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SplitByTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            splitFileType = (ESplitFileType)SplitByType.SelectedIndex + 1;
            if (!loaded)
            {
                return;
            }
            UpdateUi();
        }

        private void UpdateUi()
        {
            if (splitFileType == ESplitFileType.SplitByNumberOfRows) // by rows number
            {
                SplitEvery.Visibility = Visibility.Visible;
                SplitEveryLabel1.Visibility = Visibility.Visible;
                SplitEveryLabel2.Visibility = Visibility.Visible;
                FilesSplitNumber.Visibility = Visibility.Collapsed;
                FilesSplitNumberLabel1.Visibility = Visibility.Collapsed;
                FilesSplitNumberLabel2.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsLabel1.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsFrom.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsLabel2.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsTo.Visibility = Visibility.Collapsed;
                SplitButton.Content = "Split";
            }
            else if (splitFileType == ESplitFileType.SplitByNumberOfEqualFiles) // by files number
            {
                SplitEvery.Visibility = Visibility.Collapsed;
                SplitEveryLabel1.Visibility = Visibility.Collapsed;
                SplitEveryLabel2.Visibility = Visibility.Collapsed;
                FilesSplitNumber.Visibility = Visibility.Visible;
                FilesSplitNumberLabel1.Visibility = Visibility.Visible;
                FilesSplitNumberLabel2.Visibility = Visibility.Visible;
                ExtractRequiredRowsLabel1.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsFrom.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsLabel2.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsTo.Visibility = Visibility.Collapsed;
                SplitButton.Content = "Split";
            }
            else if (splitFileType == ESplitFileType.ExtractRequiredRows) // by required rows
            {
                SplitEvery.Visibility = Visibility.Collapsed;
                SplitEveryLabel1.Visibility = Visibility.Collapsed;
                SplitEveryLabel2.Visibility = Visibility.Collapsed;
                FilesSplitNumber.Visibility = Visibility.Collapsed;
                FilesSplitNumberLabel1.Visibility = Visibility.Collapsed;
                FilesSplitNumberLabel2.Visibility = Visibility.Collapsed;
                ExtractRequiredRowsLabel1.Visibility = Visibility.Visible;
                ExtractRequiredRowsFrom.Visibility = Visibility.Visible;
                ExtractRequiredRowsLabel2.Visibility = Visibility.Visible;
                ExtractRequiredRowsTo.Visibility = Visibility.Visible;
                SplitButton.Content = "Extract";
            }
        }
    }
}
