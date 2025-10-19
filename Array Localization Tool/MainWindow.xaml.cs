using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Array_Translate_Tool
{
    public partial class MainWindow : Window
    {
        public class TermEntry : INotifyPropertyChanged
        {
            private string _translation;
            private bool _isModified;

            public int Number { get; set; }
            public string Term { get; set; }
            public string Original { get; set; }

            public string Translation
            {
                get => _translation;
                set
                {
                    if (_translation != value)
                    {
                        _translation = value;
                        OnPropertyChanged(nameof(Translation));
                    }
                }
            }

            public int JsonIndex { get; set; }

            public bool IsModified
            {
                get => _isModified;
                set
                {
                    if (_isModified != value)
                    {
                        _isModified = value;
                        OnPropertyChanged(nameof(IsModified));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<TermEntry> terms = new ObservableCollection<TermEntry>();
        private string jsonPath;
        private JToken jsonData;
        private int origIndex = 0;
        private int langIndex = 1;
        private bool unsavedChanges = false;
        private int[] matchIndices = new int[0];
        private int currentMatch = -1;
        private bool isStringTableFormat = false;

        public MainWindow()
        {
            InitializeComponent();
            DataGridTerms.ItemsSource = terms;
            SetControlsEnabled(false);
            BtnOpen.IsEnabled = true;
            UpdateNavigationButtons();
            DataGridTerms.PreviewKeyDown += DataGridTerms_PreviewKeyDown;
        }

        private void SetControlsEnabled(bool state)
        {
            BtnSave.IsEnabled = state;
            BtnLoadTranslation.IsEnabled = state;
            BtnExportCsv.IsEnabled = state;
            BtnImportCsv.IsEnabled = state;
            TglSearchByID.IsEnabled = state;
            TxtSearch.IsEnabled = state;
            BtnSearch.IsEnabled = state;
            ChkCase.IsEnabled = state;
            ChkExact.IsEnabled = state;
            BtnRestoreOriginal.IsEnabled = state;
            BtnRestoreAll.IsEnabled = state;
        }

        public void OpenFile(string filePath)
        {
            try
            {
                jsonPath = filePath;
                jsonData = JToken.Parse(File.ReadAllText(jsonPath));

                if (jsonData["m_TableData"]?["Array"] is JArray stringTableArray &&
                    jsonData["m_LocaleId"]?["m_Code"] != null)
                {
                    isStringTableFormat = true;
                    LoadStringTableFormat(stringTableArray);
                    return;
                }

                isStringTableFormat = false;

                bool isItemsFormat = jsonData["Items"] is JArray;
                bool isNewFormat = false;

                JArray termsArray = null;
                JArray langsArray = null;

                if (isItemsFormat)
                {
                    termsArray = (JArray)jsonData["Items"];
                    langsArray = jsonData["Languages"] as JArray;
                }
                else if (jsonData["lines"]?["Array"] is JArray linesArray && jsonData["languages"]?["Array"] is JArray languagesArray)
                {
                    termsArray = linesArray;
                    langsArray = languagesArray;
                    isNewFormat = true;
                }
                else
                {
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

                int tempOrigIndex, tempLangIndex;

                if (!ShowIndexSelectionDialog("Виберіть індекс мови, з якого перекладається:", names, out tempOrigIndex))
                    return;
                origIndex = tempOrigIndex;

                if (!ShowIndexSelectionDialog("Виберіть індекс мови, який перекладатиметься:", names, out tempLangIndex))
                    return;
                langIndex = tempLangIndex;

                terms.Clear();

                for (int i = 0; i < termsArray.Count; i++)
                {
                    var item = termsArray[i];

                    if (isNewFormat)
                    {
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
                                Original = ConvertNewlinesToMarkers(orig),
                                Translation = ConvertNewlinesToMarkers(trans),
                                JsonIndex = i
                            });
                        }
                    }
                    else
                    {
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
                                    Original = ConvertNewlinesToMarkers(orig),
                                    Translation = ConvertNewlinesToMarkers(trans),
                                    JsonIndex = i
                                });
                            }
                        }
                    }
                }

                DataGridTerms.Visibility = Visibility.Visible;
                SetControlsEnabled(true);
                unsavedChanges = false;
                StatsPanel.Visibility = Visibility.Visible;
                UpdateStats();
                UpdateTitle();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Файл не знайдено за шляхом: {filePath}", "Помилка");
                SetControlsEnabled(false);
                BtnOpen.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося завантажити файл. Можливо, він пошкоджений або має невірний формат.\n\nПомилка: {ex.Message}", "Помилка");
                SetControlsEnabled(false);
                BtnOpen.IsEnabled = true;
            }
        }

        private void LoadStringTableFormat(JArray tableArray)
        {
            string localeCode = jsonData["m_LocaleId"]?["m_Code"]?.ToString() ?? "Unknown";

            terms.Clear();

            for (int i = 0; i < tableArray.Count; i++)
            {
                var item = tableArray[i];
                string id = item["m_Id"]?.ToString() ?? i.ToString();
                string text = item["m_Localized"]?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(text))
                {
                    terms.Add(new TermEntry
                    {
                        Number = terms.Count + 1,
                        Term = id,
                        Original = ConvertNewlinesToMarkers(text),
                        Translation = ConvertNewlinesToMarkers(text),
                        JsonIndex = i
                    });
                }
            }

            DataGridTerms.Visibility = Visibility.Visible;
            SetControlsEnabled(true);
            unsavedChanges = false;
            StatsPanel.Visibility = Visibility.Visible;
            UpdateStats();
            UpdateTitle();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (unsavedChanges)
            {
                var result = MessageBox.Show("У вас є незбережені зміни. Ви дійсно хочете відкрити новий файл?", "Попередження", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
            }

            var dlg = new OpenFileDialog { Filter = "Файл JSON (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                OpenFile(dlg.FileName);
            }
        }

        private bool ShowIndexSelectionDialog(string prompt, string[] items, out int result)
        {
            var dialog = new IndexSelectionWindow(prompt, items);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                result = dialog.SelectedIndex;
                return true;
            }

            result = -1;
            return false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isStringTableFormat)
            {
                SaveStringTableFormat();
                return;
            }

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
                        transArr = new JArray();
                        item["translationText"] = new JObject { ["Array"] = transArr };
                    }

                    if (langIndex == 0)
                    {
                        item["text"] = ConvertMarkersToNewlines(entry.Translation);
                    }
                    else
                    {
                        while (transArr.Count < langIndex)
                            transArr.Add("");

                        transArr[langIndex - 1] = ConvertMarkersToNewlines(entry.Translation);
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
                        texts[langIndex] = ConvertMarkersToNewlines(entry.Translation);
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
                        langArray[langIndex] = ConvertMarkersToNewlines(entry.Translation);
                    }
                }
            }

            var dlg = new SaveFileDialog
            {
                FileName = "N_" + System.IO.Path.GetFileName(jsonPath),
                Filter = "Файл JSON (*.json)|*.json"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, jsonData.ToString(Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
                unsavedChanges = false;
                UpdateStats();
                UpdateTitle();
                MessageBox.Show("Збережено!");
            }
        }

        private void SaveStringTableFormat()
        {
            var tableArray = jsonData["m_TableData"]["Array"] as JArray;
            if (tableArray == null)
            {
                MessageBox.Show("Неможливо зберегти: не знайдено масиву термінів", "Помилка");
                return;
            }

            foreach (var entry in terms)
            {
                var item = tableArray[entry.JsonIndex];
                item["m_Localized"] = ConvertMarkersToNewlines(entry.Translation);
            }

            var dlg = new SaveFileDialog
            {
                FileName = "N_" + System.IO.Path.GetFileName(jsonPath),
                Filter = "Файл JSON (*.json)|*.json"
            };

            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, jsonData.ToString(Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
                unsavedChanges = false;
                UpdateStats();
                UpdateTitle();
                MessageBox.Show("Збережено!");
            }
        }

        private void BtnLoadTranslation_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Файл JSON (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;

            JToken data;
            try
            {
                data = JToken.Parse(File.ReadAllText(dlg.FileName));
            }
            catch
            {
                MessageBox.Show("Не вдалося відкрити JSON-файл.", "Помилка");
                return;
            }

            if (data["m_TableData"]?["Array"] is JArray stringTableSource)
            {
                LoadTranslationFromStringTable(stringTableSource);
                return;
            }

            JArray sourceTerms = null;
            bool isItemsFormat = data["Items"] is JArray;
            bool isNewFormat = data["lines"]?["Array"] is JArray;

            if (isItemsFormat)
                sourceTerms = (JArray)data["Items"];
            else if (isNewFormat)
                sourceTerms = (JArray)data["lines"]["Array"];
            else if (data["mSource"]?["mTerms"]?["Array"] is JArray arrTerms)
                sourceTerms = arrTerms;
            else
                sourceTerms = GetTermsArray(data) as JArray;

            if (sourceTerms == null)
            {
                MessageBox.Show("Невірна структура перекладу.", "Помилка");
                return;
            }

            JArray langsArray = null;
            if (data["languages"]?["Array"] is JArray arr1)
                langsArray = arr1;
            else if (data["Languages"] is JArray arr2)
                langsArray = arr2;
            else if (data["mLanguages"]?["Array"] is JArray arr3)
                langsArray = arr3;
            else if (data["mSource"]?["mLanguages"]?["Array"] is JArray arr4)
                langsArray = arr4;

            var preview = new List<string>();
            if (langsArray != null && langsArray.Count > 0)
            {
                for (int i = 0; i < langsArray.Count; i++)
                {
                    var l = langsArray[i];
                    string name = null;

                    if (l.Type == JTokenType.Object)
                    {
                        name = l["Name"]?.ToString();
                        if (string.IsNullOrEmpty(name))
                            name = $"Мова {i}";
                    }
                    else
                    {
                        name = l.ToString();
                    }

                    preview.Add($"{i}: {name}");
                }
            }
            else
            {
                preview.Add("0: Мова 0");
            }

            var indexDialog = new IndexForJSON("Оберіть індекс перекладу:", preview);
            if (indexDialog.ShowDialog() != true || indexDialog.SelectedIndex < 0)
                return;

            int chosenIndex = indexDialog.SelectedIndex;
            var dict = new Dictionary<string, string>();

            foreach (var t in sourceTerms)
            {
                string term = null;
                string val = "";

                if (isItemsFormat)
                {
                    term = (string)t["Id"];
                    var texts = t["Texts"] as JArray;
                    if (texts != null && texts.Count > chosenIndex)
                        val = texts[chosenIndex]?.ToString() ?? "";
                }
                else if (isNewFormat)
                {
                    term = (string)t["lineID"]?.ToString();
                    if (term == null) continue;

                    if (chosenIndex == 0)
                        val = (string)t["text"] ?? "";
                    else
                    {
                        var arr = t["translationText"]?["Array"] as JArray;
                        if (arr != null && arr.Count >= chosenIndex)
                            val = arr[chosenIndex - 1]?.ToString() ?? "";
                    }
                }
                else
                {
                    term = (string)t["Term"];
                    var langs = t["Languages"]?["Array"] as JArray;
                    if (term != null && langs != null && langs.Count > chosenIndex)
                        val = langs[chosenIndex]?.ToString() ?? "";
                }

                if (!string.IsNullOrEmpty(term))
                    dict[term] = val;
            }

            bool changed = false;
            foreach (var entry in terms)
            {
                if (dict.TryGetValue(entry.Term, out var value) && value != entry.Translation)
                {
                    entry.Translation = ConvertNewlinesToMarkers(value);
                    entry.IsModified = entry.Translation != entry.Original;
                    changed = true;
                }
            }

            if (changed)
            {
                DataGridTerms.Items.Refresh();
                unsavedChanges = terms.Any(t => t.IsModified);
                UpdateStats();
                UpdateTitle();
            }
            else
            {
                MessageBox.Show("Немає нових перекладів.");
            }
        }

        private void LoadTranslationFromStringTable(JArray stringTableSource)
        {
            var dict = new Dictionary<string, string>();

            foreach (var item in stringTableSource)
            {
                string id = item["m_Id"]?.ToString();
                string text = item["m_Localized"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(id))
                    dict[id] = text;
            }

            bool changed = false;
            foreach (var entry in terms)
            {
                if (dict.TryGetValue(entry.Term, out var value) && value != entry.Translation)
                {
                    entry.Translation = ConvertNewlinesToMarkers(value);
                    entry.IsModified = entry.Translation != entry.Original;
                    changed = true;
                }
            }

            if (changed)
            {
                DataGridTerms.Items.Refresh();
                unsavedChanges = terms.Any(t => t.IsModified);
                UpdateStats();
                UpdateTitle();
                MessageBox.Show("Переклад імпортовано!");
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
            var baseTitle = "Array Localization Tool 2.9";

            if (!string.IsNullOrEmpty(jsonPath))
            {
                var fileName = System.IO.Path.GetFileName(jsonPath);
                baseTitle += $" - {fileName}";
            }

            Title = (unsavedChanges ? "*" : "") + baseTitle;
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
                    UpdateStats();
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
                Filter = "Файл CSV (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            using (var writer = new StreamWriter(dlg.FileName, false, new UTF8Encoding(false)))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                ShouldQuote = args => true
            }))
            {
                csv.WriteField("ID");
                csv.WriteField("Оригінал");
                csv.WriteField("Переклад");
                csv.NextRecord();

                foreach (var entry in terms)
                {
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
            var dlg = new OpenFileDialog { Filter = "Файл CSV (*.csv)|*.csv" };
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
                            if (entry != null)
                            {
                                var translationWithMarkers = ConvertNewlinesToMarkers(translation);
                                if (entry.Translation != translationWithMarkers)
                                {
                                    entry.Translation = translationWithMarkers;
                                    entry.IsModified = entry.Translation != entry.Original;
                                    changed = true;
                                }
                            }
                        }
                    }
                    if (changed)
                    {
                        DataGridTerms.Items.Refresh();
                        unsavedChanges = true;
                        UpdateStats();
                        UpdateTitle();
                        MessageBox.Show("Переклад імпортовано!");
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
                UpdateStats();
            }
        }

        private void BtnRestoreAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Увага: Поточний переклад буде втрачено.\n\nВи дійсно хочете повернути оригінал в усі рядки?",
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
                UpdateStats();
                UpdateTitle();
            }
        }

        private void DataGridTerms_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var binding = (e.EditingElement as TextBox)?.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();
                UpdateStats();
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

            StringComparison comparison = ChkCase.IsChecked == true
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            Func<string, bool> matcher;
            if (ChkExact.IsChecked == true)
            {
                matcher = s => s.Equals(query, comparison);
            }
            else
            {
                matcher = s => s.IndexOf(query, comparison) >= 0;
            }

            matchIndices = terms
                .Select((e, i) => new { e, i })
                .Where(x =>
                {
                    if (TglSearchByID.IsChecked == true)
                        return matcher(x.e.Term);
                    else
                        return matcher(x.e.Original) || matcher(x.e.Translation);
                })
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

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText();
        }

        private void TglSearchByID_Checked(object sender, RoutedEventArgs e)
        {
            TglSearchByID.Content = "ID";
        }

        private void TglSearchByID_Unchecked(object sender, RoutedEventArgs e)
        {
            TglSearchByID.Content = "Текст";
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
                    MessageBoxButton.YesNoCancel);
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

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void UpdateStats()
        {
            int totalRows = terms.Count;
            int totalWords = terms.Sum(t =>
            {
                if (string.IsNullOrWhiteSpace(t.Translation))
                    return 0;

                var textWithoutMarkers = t.Translation.Replace("<\\rn>", " ").Replace("<\\r>", " ").Replace("<\\n>", " ");
                return textWithoutMarkers.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            });

            int translatedRows = terms.Count(t => t.IsModified);
            double percent = totalRows > 0 ? (translatedRows * 100.0 / totalRows) : 0;

            LblTotalRows.Text = totalRows.ToString();
            LblTotalWords.Text = totalWords.ToString();
            LblTranslatedRows.Text = translatedRows.ToString();
            LblTranslatedPercent.Text = percent.ToString("0.0") + "%";
        }

        private string ConvertNewlinesToMarkers(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Replace("\r\n", "<\\rn>").Replace("\r", "<\\r>").Replace("\n", "<\\n>");
        }

        private string ConvertMarkersToNewlines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Replace("<\\rn>", "\r\n").Replace("<\\r>", "\r").Replace("<\\n>", "\n");
        }

        private void DataGridTerms_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var cell = DataGridTerms.CurrentCell;
                if (cell != null && cell.Item != null)
                {
                    var column = cell.Column as DataGridBoundColumn;
                    if (column != null)
                    {
                        var binding = column.Binding as Binding;
                        if (binding != null)
                        {
                            var propertyName = binding.Path.Path;
                            var rowData = cell.Item;
                            var value = rowData.GetType().GetProperty(propertyName)?.GetValue(rowData, null)?.ToString();
                            Clipboard.SetText(value ?? string.Empty);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.O)
            {
                BtnOpen_Click(null, null);
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                if (BtnSave.IsEnabled)
                    BtnSave_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (TxtSearch.IsEnabled)
                {
                    TxtSearch.Focus();
                    TxtSearch.SelectAll();
                }
                e.Handled = true;
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