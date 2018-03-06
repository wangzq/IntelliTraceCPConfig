namespace IntelliTraceCPConfig
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Xml;

    using Microsoft.Win32;

    public partial class Main : Window
    {
        #region Fields

        private readonly string _defaultPath;
        private readonly Dictionary<string, int> _len = new Dictionary<string, int>
                                                            {
                                                                {"100 MB", 1600},
                                                                {"250 MB", 4000},
                                                                {"500 MB", 8000},
                                                                {"1 GB", 16384},
                                                                {"2 GB", 32768},
                                                                {"5 GB", 81920},
                                                                {"10 GB", 163840},
                                                                {"20 GB", 327680}
                                                            };

        private List<string> modules;

        #endregion Fields

        #region Constructors

        public Main()
        {
            InitializeComponent();

            var root = tree.Items[0] as IntelliTraceCPConfigViewModel;

            CommandBindings.Add(
                new CommandBinding(
                    ApplicationCommands.Undo,
                    (sender, e) => // Execute
                        {
                            e.Handled = true;
                            if (root != null) root.IsChecked = false;

                            tree.Focus();
                        },
                    (sender, e) =>
                        {
                            e.Handled = true;
                            if (root != null) e.CanExecute = (root.IsChecked != false);
                        }));

            tree.Focus();
            tabAdvanced.IsEnabled = false;
            tabModules.IsEnabled = false;
            tabEvents.IsEnabled = false;
            grdGeneral.IsEnabled = false;
            SaveMenuItem.IsEnabled = false;
        }

        #endregion Constructors

        #region Properties

        public string SelectedPath
        {
            get; set;
        }

        #endregion Properties

        #region Methods

        public void AddPattern(string pattern)
        {
            modules.Remove(pattern);
            modules.Add(pattern);
            lstModules.Items.Clear();
            foreach (string module in modules)
            {
                lstModules.Items.Add(module);
            }
        }

        private void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            var ab = new About();
            ab.Show();
        }

        private void BtnAddAssembliesClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = _defaultPath,
                Filter = "DLL and Executables (*.dll, *.exe)|*.dll*;*.exe"
            };

            dialog.Multiselect = true;
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                foreach (string filenames in dialog.FileNames)
                {
                    AddPattern(Path.GetFileNameWithoutExtension(filenames));
                }
            }
        }

        private void BtnAddPatternClick(object sender, RoutedEventArgs e)
        {
            var addModule = new AddModulePattern();
            addModule.txtPattern.Focus();
            addModule.Owner = this;
            addModule.ShowDialog();
        }

        private void BtnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (lstModules.SelectedItems.Count > 0)
            {
                foreach (object selectedItem in lstModules.SelectedItems)
                {
                    modules.Remove(selectedItem.ToString());
                }
            }
            lstModules.Items.Clear();
            foreach (string module in modules)
            {
                lstModules.Items.Add(module);
            }
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void CloseMenuClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MaxamountRecordingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int dicFileSize;
            var cbitem = (ComboBoxItem)MaxamountRecording.SelectedItem;
            _len.TryGetValue(cbitem.Content.ToString(), out dicFileSize);
            IntelliTraceCPConfigViewModel.SetFileSizeValue(dicFileSize.ToString(CultureInfo.InvariantCulture));
        }

        private void OpenMenuClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = _defaultPath,
                DefaultExt = ".xml",
                Filter = "Xml documents (.xml)|*.xml"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                tabAdvanced.IsEnabled = true;
                tabModules.IsEnabled = true;
                grdGeneral.IsEnabled = true;
                tabEvents.IsEnabled = true;
                SaveMenuItem.IsEnabled = true;

                SelectedPath = dialog.FileName;

                var root = tree.Items[0] as IntelliTraceCPConfigViewModel;
                IntelliTraceCPConfigViewModel.FileName = SelectedPath;
                tree.IsEnabled = true;
                btnSave.IsEnabled = true;

                if (root != null) tree.DataContext = root.ShowFileContent();
                int fileSize = IntelliTraceCPConfigViewModel.GetFileSizeValue();

                modules = IntelliTraceCPConfigViewModel.GetModuleList();

                foreach (string module in modules)
                {
                    lstModules.Items.Add(module);
                }

                //lstModules.ItemsSource = modules;

                bool isExcluded = IntelliTraceCPConfigViewModel.AreModuleExcluded();

                bool traceInstrumenationEnabled = IntelliTraceCPConfigViewModel.GetTraceInstrumentation();

                rbEventsOnly.IsChecked = !traceInstrumenationEnabled;
                rbEventsAndCall.IsChecked = traceInstrumenationEnabled;

                rbExcluded.IsChecked = isExcluded;
                rbIncluded.IsChecked = !isExcluded;

                foreach (var itm in _len)
                {
                    var citem = new ComboBoxItem { Content = itm.Key };
                    if (fileSize == itm.Value)
                        citem.IsSelected = true;
                    MaxamountRecording.Items.Add(citem);
                }
            }
        }

        private void RbExcludedChecked(object sender, RoutedEventArgs e)
        {
            IntelliTraceCPConfigViewModel.SetModuledExcluded(true);
        }

        private void RbIncludedChecked(object sender, RoutedEventArgs e)
        {
            IntelliTraceCPConfigViewModel.SetModuledExcluded(false);
        }

        private void SaveFile()
        {
            var dialog = new SaveFileDialog { FileName = SelectedPath, DefaultExt = ".xml", Filter = "Xml documents (.xml)|*.xml" };

            dialog.ShowDialog();
            XmlDocument xdoc = IntelliTraceCPConfigViewModel.GetModifiedDocument(modules);
            xdoc.Save(dialog.FileName);
        }

        private void SaveMenuClick(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        #endregion Methods

        private void rbEventsAndCall_Checked(object sender, RoutedEventArgs e)
        {
            IntelliTraceCPConfigViewModel.SetTraceInstrumentation(true);
        }

        private void rbEventsOnly_Checked(object sender, RoutedEventArgs e)
        {
            IntelliTraceCPConfigViewModel.SetTraceInstrumentation(false);
        }
    }
}