using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Array_Translate_Tool
{
    public partial class MainWindow : Window
    {
        public class TermEntry
        {
            public int Number { get; set; }
            public string Term { get; set; }
            public string Original { get; set; }
            public string Translation { get; set; }
            public int JsonIndex { get; set; }
            public bool IsModified { get; set; }
        }

        private ObservableCollection<TermEntry> terms = new ObservableCollection<TermEntry>();
        private string jsonPath;
        private JToken jsonData;
        private int origIndex = 0;
        private int langIndex = 1;
        private bool unsavedChanges = false;
        private int[] matchIndices = new int[0];
        private int currentMatch = -1;

        public MainWindow()
        {
            InitializeComponent();
            DataGridTerms.ItemsSource = terms;
            SetControlsEnabled(false);
            BtnOpen.IsEnabled = true;
            UpdateNavigationButtons();
        }

        private void SetControlsEnabled(bool state)
        {
            BtnSave.IsEnabled = state;
            BtnLoadTranslation.IsEnabled = state;
            BtnExportCsv.IsEnabled = state;
            BtnImportCsv.IsEnabled = state;
            TxtSearch.IsEnabled = state;
            ChkCase.IsEnabled = state;
            BtnRestoreOriginal.IsEnabled = state;
            BtnRestoreAll.IsEnabled = state;
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;

            jsonPath = dlg.FileName;
            jsonData = JToken.Parse(File.ReadAllText(jsonPath));

            bool isItemsFormat = jsonData["Items"] is JArray;
            bool isNewFormat = false;

            JArray termsArray = null;
            JArray langsArray = null;

            if (isItemsFormat)
            {
                // Старий формат
                termsArray = (JArray)jsonData["Items"];
                langsArray = jsonData["Languages"] as JArray;
            }
            else if (jsonData["lines"]?["Array"] is JArray linesArray && jsonData["languages"]?["Array"] is JArray languagesArray)
            {
                // Новий формат
                termsArray = linesArray;
                langsArray = languagesArray;
                isNewFormat = true;
            }
            else
            {
                // Спроба взяти інші варіанти з json (як раніше)
                termsArray = GetTermsArray(jsonData) as JArray;
                langsArray = GetLangsArray(jsonData) as JArray;
            }

            if (termsArray == null || langsArray == null || !langsArray.Any())
            {
                MessageBox.Show("Невірна структура JSON", "Помилка");
                return;
            }

            var names = langsArray.Select((l, i) =>
            {
                if (l.Type == JTokenType.Object)
                {
                    var name = l["Name"]?.ToString();
                    return $"{i}: {name ?? $"Мова {i}"}";
                }
                else if (l.Type == JTokenType.Integer || l.Type == JTokenType.Float || l.Type == JTokenType.String)
                {
                    return $"{i}: {l.ToString()}";
                }
                else
                {
                    return $"{i}: Мова {i}";
                }
            }).ToArray();

            if (!AskIndex("Виберіть індекс, з якого перекладається:", names, out origIndex)) return;
            if (!AskIndex("Виберіть індекс, який БУДЕ перекладатись:", names, out langIndex)) return;

            terms.Clear();

            for (int i = 0; i < termsArray.Count; i++)
            {
                var item = termsArray[i];

                if (isNewFormat)
                {
                    // Новий формат: беремо lineID як ідентифікатор
                    string term = item["lineID"]?.ToString() ?? i.ToString();

                    var transArr = item["translationText"]?["Array"] as JArray;
                    if (transArr == null) continue;

                    string orig = "";
                    string trans = "";

                    if (origIndex == 0)
                        orig = (string)item["text"] ?? "";
                    else if (origIndex > 0 && transArr.Count >= origIndex)
                        orig = transArr[origIndex - 1]?.ToString() ?? "";

                    if (langIndex == 0)
                        trans = (string)item["text"] ?? "";
                    else if (langIndex > 0 && transArr.Count >= langIndex)
                        trans = transArr[langIndex - 1]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(orig))
                    {
                        terms.Add(new TermEntry
                        {
                            Number = terms.Count + 1,
                            Term = term,
                            Original = orig,
                            Translation = trans,
                            JsonIndex = i
                        });
                    }
                }
                else
                {
                    // Старий формат
                    string term = isItemsFormat ? (string)item["Id"] : (string)item["Term"];
                    var langs = isItemsFormat
                        ? item["Texts"]?.ToList()
                        : item["Languages"]?["Array"]?.ToList();

                    if (langs != null && origIndex < langs.Count && langIndex < langs.Count)
                    {
                        var orig = langs[origIndex]?.ToString() ?? "";
                        var trans = langs[langIndex]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(orig))
                        {
                            terms.Add(new TermEntry
                            {
                                Number = terms.Count + 1,
                                Term = term,
                                Original = orig,
                                Translation = trans,
                                JsonIndex = i
                            });
                        }
                    }
                }
            }

            DataGridTerms.Visibility = Visibility.Visible;
            SetControlsEnabled(true);
            unsavedChanges = false;
            UpdateTitle();
        }

        private bool AskIndex(string prompt, string[] items, out int result)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(prompt + "\n\n" + string.Join("\n", items), "Вибір", "0");
            return int.TryParse(input, out result) && result >= 0 && result < items.Length;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            bool isItemsFormat = jsonData["Items"] is JArray;
            bool isNewFormat = false;

            JArray termsArray = null;

            if (isItemsFormat)
            {
                termsArray = (JArray)jsonData["Items"];
            }
            else if (jsonData["lines"]?["Array"] is JArray linesArray)
            {
                termsArray = linesArray;
                isNewFormat = true;
            }
            else
            {
                termsArray = GetTermsArray(jsonData) as JArray;
            }

            if (termsArray == null)
            {
                MessageBox.Show("Неможливо зберегти: не знайдено масиву термінів", "Помилка");
                return;
            }

            foreach (var entry in terms)
            {
                if (isNewFormat)
                {
                    var item = termsArray[entry.JsonIndex];
                    var transArr = item["translationText"]?["Array"] as JArray;

                    if (transArr == null)
                    {
                        // Якщо немає масиву, створюємо
                        transArr = new JArray();
                        item["translationText"] = new JObject { ["Array"] = transArr };
                    }

                    // Для нового формату: індекси у transArr на 0 базі, але 0 — це Original у "text"
                    // Тому для langIndex == 0 оновлюємо "text", інакше — translationText.Array[langIndex - 1]
                    if (langIndex == 0)
                    {
                        item["text"] = entry.Translation.Replace("\r\n", "\n");
                    }
                    else
                    {
                        // Запевняємось, що індекс існує
                        while (transArr.Count < langIndex)
                            transArr.Add("");

                        transArr[langIndex - 1] = entry.Translation.Replace("\r\n", "\n");
                    }
                }
                else
                {
                    if (isItemsFormat)
                    {
                        var texts = jsonData["Items"][entry.JsonIndex]["Texts"] as JArray;
                        if (texts == null)
                        {
                            texts = new JArray();
                            jsonData["Items"][entry.JsonIndex]["Texts"] = texts;
                        }
                        while (texts.Count <= langIndex)
                            texts.Add("");
                        texts[langIndex] = entry.Translation.Replace("\r\n", "\n");
                    }
                    else
                    {
                        var array = GetTermsArray(jsonData);
                        var langArray = array[entry.JsonIndex]["Languages"]["Array"] as JArray;
                        if (langArray == null)
                        {
                            langArray = new JArray();
                            array[entry.JsonIndex]["Languages"]["Array"] = langArray;
                        }
                        while (langArray.Count <= langIndex)
                            langArray.Add("");
                        langArray[langIndex] = entry.Translation.Replace("\r\n", "\n");
                    }
                }
            }

            var dlg = new SaveFileDialog
            {
                FileName = "N_" + System.IO.Path.GetFileName(jsonPath),
                Filter = "JSON Files (*.json)|*.json"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, jsonData.ToString(Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
                unsavedChanges = false;
                UpdateTitle();
                MessageBox.Show("Збережено!");
            }
        }

        private void BtnLoadTranslation_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;

            var data = JToken.Parse(File.ReadAllText(dlg.FileName));

            bool isItemsFormat = data["Items"] is JArray;
            bool isNewFormat = data["lines"]?["Array"] is JArray;

            JArray sourceTerms = null;

            if (isItemsFormat)
            {
                sourceTerms = (JArray)data["Items"];
            }
            else if (isNewFormat)
            {
                sourceTerms = (JArray)data["lines"]["Array"];
            }
            else
            {
                sourceTerms = GetTermsArray(data) as JArray;
            }

            if (sourceTerms == null)
            {
                MessageBox.Show("Невірна структура перекладу.", "Помилка");
                return;
            }

            var dict = new Dictionary<string, string>();

            if (isItemsFormat)
            {
                foreach (var t in sourceTerms)
                {
                    var id = (string)t["Id"];
                    var texts = t["Texts"] as JArray;
                    if (id != null && texts != null && texts.Count > langIndex)
                    {
                        var val = texts[langIndex]?.ToString().Trim() ?? "";
                        dict[id] = val;
                    }
                }
            }
            else if (isNewFormat)
            {
                foreach (var t in sourceTerms)
                {
                    var term = (string)t["lineID"]?.ToString();
                    if (term == null) continue;

                    string val = "";

                    if (langIndex == 0)
                    {
                        val = (string)t["text"] ?? "";
                    }
                    else
                    {
                        var arr = t["translationText"]?["Array"] as JArray;
                        if (arr != null && arr.Count >= langIndex)
                            val = arr[langIndex - 1]?.ToString() ?? "";
                    }

                    val = val.Trim();
                    dict[term] = val;
                }
            }
            else
            {
                foreach (var t in sourceTerms)
                {
                    var term = (string)t["Term"];
                    var langsArray = t["Languages"]?["Array"] as JArray;
                    if (term != null && langsArray != null && langsArray.Count > langIndex)
                    {
                        var val = langsArray[langIndex]?.ToString().Trim() ?? "";
                        dict[term] = val;
                    }
                }
            }

            bool changed = false;
            foreach (var entry in terms)
            {
                if (dict.TryGetValue(entry.Term, out var value) && value != entry.Translation)
                {
                    entry.Translation = value;
                    entry.IsModified = entry.Translation != entry.Original;
                    changed = true;
                }
            }

            if (changed)
            {
                DataGridTerms.Items.Refresh();
                unsavedChanges = terms.Any(t => t.IsModified);
                UpdateTitle();
            }
            else
            {
                MessageBox.Show("Немає нових перекладів.");
            }
        }

        private JToken GetTermsArray(JToken data)
        {
            return data["mSource"]?["mTerms"]?["Array"] ?? data["mTerms"]?["Array"];
        }

        private JToken GetLangsArray(JToken data)
        {
            return data["mSource"]?["mLanguages"]?["Array"] ?? data["mLanguages"]?["Array"];
        }

        private void UpdateTitle()
        {
            Title = (unsavedChanges ? "*" : "") + "Array Translate Tool 2 © Галицький Розбишака";
        }

        private void DataGridTerms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridTerms.SelectedItem is TermEntry entry)
            {
                TxtTranslation.TextChanged -= TxtTranslation_TextChanged;
                TxtTranslation.Text = entry.Translation;
                TxtTranslation.TextChanged += TxtTranslation_TextChanged;
            }
            else
            {
                TxtTranslation.TextChanged -= TxtTranslation_TextChanged;
                TxtTranslation.Clear();
                TxtTranslation.TextChanged += TxtTranslation_TextChanged;
            }
        }

        private void TxtTranslation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataGridTerms.SelectedItem is TermEntry entry)
            {
                var newText = TxtTranslation.Text;
                if (entry.Translation != newText)
                {
                    entry.Translation = newText;
                    entry.IsModified = entry.Translation != entry.Original;
                    unsavedChanges = true;
                    UpdateTitle();
                    DataGridTerms.Items.Refresh();
                }
            }
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!terms.Any())
            {
                MessageBox.Show("Немає даних для експорту.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                FileName = System.IO.Path.GetFileNameWithoutExtension(jsonPath) + ".csv",
                Filter = "CSV Files (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            using (var writer = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                ShouldQuote = args => true
            }))
            {
                csv.WriteField("№");
                csv.WriteField("ID");
                csv.WriteField("Оригінал");
                csv.WriteField("Переклад");
                csv.NextRecord();

                foreach (var entry in terms)
                {
                    csv.WriteField(entry.Number);
                    csv.WriteField(entry.Term);
                    csv.WriteField(entry.Original);
                    csv.WriteField(entry.Translation);
                    csv.NextRecord();
                }
            }

            MessageBox.Show("Експортовано у CSV.");
        }

        private void BtnImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var reader = new StreamReader(dlg.FileName, Encoding.UTF8))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    DetectDelimiter = true,
                    IgnoreBlankLines = true,
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    PrepareHeaderForMatch = args => args.Header.Trim()
                }))
                {
                    var records = csv.GetRecords<dynamic>().ToList();
                    bool changed = false;

                    foreach (var record in records)
                    {
                        var dict = record as IDictionary<string, object>;
                        if (dict == null || !dict.ContainsKey("ID")) continue;

                        string term = dict["ID"]?.ToString();
                        string translation = dict.ContainsKey("Переклад")
                            ? dict["Переклад"]?.ToString()
                            : (dict.ContainsKey("Translation") ? dict["Translation"]?.ToString() : null);

                        if (term != null && translation != null)
                        {
                            var entry = terms.FirstOrDefault(t => t.Term == term);
                            if (entry != null && entry.Translation != translation)
                            {
                                entry.Translation = translation;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        DataGridTerms.Items.Refresh();
                        unsavedChanges = true;
                        UpdateTitle();
                        MessageBox.Show("Імпортовано успішно.");
                    }
                    else
                    {
                        MessageBox.Show("Немає нових перекладів.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка імпорту:\n" + ex.Message);
            }
        }

        private void BtnRestoreOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTerms.SelectedItem is TermEntry entry)
            {
                entry.Translation = entry.Original;
                entry.IsModified = false;
                TxtTranslation.Text = entry.Original;

                unsavedChanges = terms.Any(t => t.IsModified);
                UpdateTitle();
                DataGridTerms.Items.Refresh();
            }
        }

        private void BtnRestoreAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Увага: Поточний переклад буде втрачено.\n\nВи дійсно хочете повернути оригінал у всі рядки?",
                "Попередження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var entry in terms)
                {
                    entry.Translation = entry.Original;
                    entry.IsModified = false;
                }

                if (DataGridTerms.SelectedItem is TermEntry current)
                    TxtTranslation.Text = current.Original;
                else
                    TxtTranslation.Clear();

                DataGridTerms.Items.Refresh();

                unsavedChanges = terms.Any(t => t.IsModified);
                UpdateTitle();
            }
        }

        private void DataGridTerms_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var binding = (e.EditingElement as TextBox)?.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();
            }
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchText();
        }

        private void SearchText()
        {
            var query = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            Func<string, bool> matcher;

            if (ChkCase.IsChecked == true)
            {
                matcher = s => s.Contains(query);
            }
            else
            {
                matcher = s => s.ToLower().Contains(query.ToLower());
            }

            matchIndices = terms
                .Select((e, i) => new { e, i })
                .Where(x => matcher(x.e.Original) || matcher(x.e.Translation))
                .Select(x => x.i)
                .ToArray();

            if (matchIndices.Length > 0)
            {
                currentMatch = 0;
                HighlightMatch();
            }
            else
            {
                LblSearchStatus.Content = "";
                TxtSearch.Background = System.Windows.Media.Brushes.MistyRose;
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                timer.Tick += delegate
                {
                    TxtSearch.ClearValue(TextBox.BackgroundProperty);
                    timer.Stop();
                };
                timer.Start();
            }
            UpdateNavigationButtons();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (matchIndices.Length == 0) return;
            currentMatch = (currentMatch - 1 + matchIndices.Length) % matchIndices.Length;
            HighlightMatch();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (matchIndices.Length == 0) return;
            currentMatch = (currentMatch + 1) % matchIndices.Length;
            HighlightMatch();
        }

        private void HighlightMatch()
        {
            if (currentMatch >= 0 && currentMatch < matchIndices.Length)
            {
                var rowIndex = matchIndices[currentMatch];
                DataGridTerms.SelectedIndex = rowIndex;
                DataGridTerms.ScrollIntoView(DataGridTerms.Items[rowIndex]);
                LblSearchStatus.Content = (currentMatch + 1) + " з " + matchIndices.Length;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (unsavedChanges)
            {
                var res = MessageBox.Show("Бажаєте зберегти перед виходом?", "Вихід",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                    BtnSave_Click(null, null);
                else if (res == MessageBoxResult.Cancel)
                    e.Cancel = true;
            }
        }

        private void UpdateNavigationButtons()
        {
            bool hasMatches = matchIndices != null && matchIndices.Length > 0;
            BtnPrev.IsEnabled = hasMatches;
            BtnNext.IsEnabled = hasMatches;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                BtnSave_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
            }
            else if (e.Key == Key.Escape)
            {
                TxtSearch.Clear();
                DataGridTerms.Focus();
            }
            else if (e.Key == Key.F3)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    BtnPrev_Click(null, null);
                else
                    BtnNext_Click(null, null);

                e.Handled = true;
            }
        }
    }
}
