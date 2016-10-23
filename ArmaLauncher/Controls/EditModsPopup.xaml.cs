using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;
using Newtonsoft.Json;

namespace ArmaLauncher.Controls
{
    /// <summary>
    /// Interaction logic for PasswordPopup.xaml
    /// </summary>
    public partial class EditModsPopup : Popup, INotifyPropertyChanged
    {
        private bool ShouldOpen { get; set; }
        const string StringModPrefix = "-mod=";
        private int StartModParseIndex { get; set; }
        private int EndModParseIndex { get; set; }

        public ObservableCollection<ClientMod> UnSelectedMods
        {
            get { return _unSelectedMods; }
            set
            {
                _unSelectedMods = value;
                OnPropertyChanged("UnSelectedMods");
            }
        }

        public ObservableCollection<ClientMod> SelectedMods
        {
            get { return _selectedMods; }
            set
            {
                _selectedMods = value;
                OnPropertyChanged("SelectedMods");
            }
        }

        private ClientMod _leftDataGridSelectedItem;
        public ClientMod LeftDataGridSelectedItem
        {
            get { return _leftDataGridSelectedItem; }
            set
            {
                _leftDataGridSelectedItem = value;
                dgUnSelectedMods.UpdateLayout();
            }
        }

        private ClientMod _rightDataGridSelectedItem;
        public ClientMod RightDataGridSelectedItem
        {
            get { return _rightDataGridSelectedItem; }
            set
            {
                _rightDataGridSelectedItem = value;
                dgSelectedMods.UpdateLayout();
            }
        }

        private FrameworkElement _parentFrameworkElement;
        private ObservableCollection<ClientMod> _unSelectedMods;
        private ObservableCollection<ClientMod> _selectedMods;

        public FrameworkElement ParentFrameworkElement
        {
            get
            {
                return _parentFrameworkElement;
            }
            set
            {
                if (Equals(_parentFrameworkElement, value)) return;
                _parentFrameworkElement = value;
                OnPropertyChanged("ParentFrameworkElement");
            }
        }

        private void EditModsPopup_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        public EditModsPopup()
        {
            InitializeComponent();
            this.DataContext = this;
            ParentFrameworkElement = Application.Current.MainWindow;
            ShouldOpen = true;

            //reset grid
            dgUnSelectedMods.AutoGenerateColumns = true;
            dgUnSelectedMods.AutoGenerateColumns = false;
            dgUnSelectedMods.Items.Refresh();

            //reset grid
            dgSelectedMods.AutoGenerateColumns = true;
            dgSelectedMods.AutoGenerateColumns = false;
            dgSelectedMods.Items.Refresh();

            // Move window start
            var thumb = new Thumb
            {
                Width = 0,
                Height = 0,
            };
            ContentCanvas.Children.Add(thumb);

            MouseDown += (sender, e) =>
            {
                thumb.RaiseEvent(e);
            };

            thumb.DragDelta += (sender, e) =>
            {
                popupMods.HorizontalOffset += e.HorizontalChange;
                popupMods.VerticalOffset += e.VerticalChange;
            };
            // Move window end

            //todo:aaron-load grids
            var gameType = Globals.Current.ArmaGameType;
            // load into Selected: get any mods that are already in the launch params
            // load into Unselected: look up all mods from Arma installation directory
            switch (gameType)
            {
                case GameType.Arma2:
                    SelectedMods = !String.IsNullOrEmpty(Globals.Current.Arma2ModParams) ? ParseModsFromString(Globals.Current.Arma2ModParams) : new ObservableCollection<ClientMod>() ;
                    //UnSelectedMods = GetModListOffDisk(Globals.Current.Arma2Path);
                    UnSelectedMods = GetModListOffDisk(Globals.Current.Arma2OAPath);
                    break;
                case GameType.Arma3:
                case GameType.None:
                    SelectedMods = !String.IsNullOrEmpty(Globals.Current.Arma3ModParams) ? ParseModsFromString(Globals.Current.Arma3ModParams) : new ObservableCollection<ClientMod>();
                    UnSelectedMods = GetModListOffDisk(Globals.Current.Arma3Path);
                    break;
            }

            if (ShouldOpen)
            {
                SortLists();

                ModsPopup_OnLoaded();
                popupMods.IsOpen = true;
            }
        }

        private void SortLists()
        {
            SelectedMods = new ObservableCollection<ClientMod>(SelectedMods.OrderBy(i => i.Name));
            UnSelectedMods = new ObservableCollection<ClientMod>(UnSelectedMods.OrderBy(i => i.Name));
        }

        private ObservableCollection<ClientMod> ParseModsFromString(string input)
        {
            var clientMods = new ObservableCollection<ClientMod>();

            //var targetInput = string.Empty;

            //var sanitizeInput = input.Split('-');

            ////for now, go with 1st found pattern in 'input'
            //if (sanitizeInput.Count() > 1)
            //{
            //    foreach (var s in sanitizeInput)
            //    {
            //        var stringWithDashBackInForTest = "-" + s;
            //        if (stringWithDashBackInForTest.Contains(StringModPrefix))
            //        {
            //            targetInput = stringWithDashBackInForTest;
            //            break;
            //        }
            //    }
            //}
            //else
            //{
            //    targetInput = input;
            //}

            //get substring of '-mod=' to ';' and ' '
            StartModParseIndex = input.IndexOf(StringModPrefix, System.StringComparison.Ordinal) + StringModPrefix.Length;
            EndModParseIndex = input.LastIndexOf(";", System.StringComparison.Ordinal) + 1;
            if (EndModParseIndex == 0)
                EndModParseIndex = input.Length;

            var parsedMods = input.Substring(StartModParseIndex, EndModParseIndex - StartModParseIndex);

            if (String.IsNullOrEmpty(parsedMods) || String.IsNullOrWhiteSpace(parsedMods)) return clientMods;

            var mods = parsedMods.Split(';').ToList();

            foreach (var mod in mods)
            {
                if (mod != String.Empty)
                    clientMods.Add(new ClientMod {Name = mod});
            }

            return clientMods;
        }

        public ObservableCollection<ClientMod> GetModListOffDisk(string targetDirectory)
        {
            if (string.IsNullOrEmpty(targetDirectory))
            {
                var dialog = new DialogWindow(
                    DialogWindow.DialogType.ErrorInfo,
                    "DOH!",
                    "The game path is empty.  Go to Settings and set the game's application path.",
                    Application.Current.MainWindow);
                dialog.ShowDialog();

                ShouldOpen = false;
                Globals.Current.NavigateToPage("/AppSettings.xaml");
                return new ObservableCollection<ClientMod>();
            }

            var clientMods = new ObservableCollection<ClientMod>();

            // Recurse into subdirectories of this directory.
            var subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (var subdirectory in subdirectoryEntries)
            {
                if (subdirectory.Contains("@"))
                {
                    var targetMod = subdirectory.Replace(targetDirectory, string.Empty).Replace("\\", string.Empty);
                    var server = SelectedMods.FirstOrDefault(i => i.Name == targetMod);
                    if (server == null)
                    {
                        clientMods.Add(new ClientMod
                        {
                            Name = targetMod
                        });
                    }
                }
            }

            return clientMods;
        }

        private void btnModsSave_Click(object sender, RoutedEventArgs e)
        {
            var modsToSave = new StringBuilder();
            foreach (var selectedMod in SelectedMods)
            {
                modsToSave.Append(selectedMod.Name + ";");
            }

            //todo:aaron- this switch can be optimized, but i am sick of messing with it today and it works...
            var splitStringList = new List<string>();

                switch (Globals.Current.ArmaGameType)
                {
                    case GameType.Arma2:
                        if (SelectedMods.Count > 0)
                        {
                            modsToSave.Insert(0, StringModPrefix);

                            //edit previous
                            if (Globals.Current.Arma2ModParams.Contains(StringModPrefix))
                            {
                                splitStringList = UpdatePreviousMods(Globals.Current.Arma2ModParams);
                                // bind everything together and save
                                modsToSave.Insert(0, splitStringList[0]);
                                modsToSave.Insert(modsToSave.Length, " " + splitStringList[1]);
                            }
                            else
                            {
                                modsToSave.Insert(0, Globals.Current.Arma2ModParams);
                            }
                        }
                        else
                        {
                            modsToSave.Insert(0, UpdatePreviousMods(Globals.Current.Arma2ModParams).FirstOrDefault());
                        }
                        Globals.Current.Arma2ModParams = modsToSave.ToString();
                        break;
                    case GameType.Arma3:
                    case GameType.None:
                        if (SelectedMods.Count > 0)
                        {
                            modsToSave.Insert(0, StringModPrefix);

                            //edit previous
                            if (Globals.Current.Arma3ModParams.Contains(StringModPrefix))
                            {
                                splitStringList = UpdatePreviousMods(Globals.Current.Arma3ModParams);
                                // bind everything together and save
                                modsToSave.Insert(0, splitStringList[0]);
                                modsToSave.Insert(modsToSave.Length, " " + splitStringList[1]);
                            }
                            else
                            {
                                modsToSave.Insert(0, Globals.Current.Arma3ModParams);
                            }
                        }
                        else
                        {
                            modsToSave.Insert(0, UpdatePreviousMods(Globals.Current.Arma3ModParams).FirstOrDefault());
                        }
                        Globals.Current.Arma3ModParams = modsToSave.ToString();
                        break;
                }

            popupMods.IsOpen = false;
        }

        private List<string> UpdatePreviousMods(string input)
        {
            var output = new List<string>();

            // get 1st part
            var start = StartModParseIndex - StringModPrefix.Length;
            var firstPart = input.Substring(0, start);
            output.Add(firstPart);

            if (SelectedMods.Count == 0)
            {
                return output;
            }

            // get last portion, excluding middle section
            var secondPart = input.Split(input[input.Length - 1]);
            output.Add(secondPart[secondPart.Count() - 1]);
            return output;
        }

        private void btnModsCancel_Click(object sender, RoutedEventArgs e)
        {
            popupMods.IsOpen = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ModsPopup_OnLoaded()
        {
            var left = 0.0;
            var top = 0.0;
            var width = 0.0;
            var height = 0.0;

            var typeString = ParentFrameworkElement.GetType().AssemblyQualifiedName;

            if (!String.IsNullOrEmpty(typeString) && typeString.Contains("Popup"))
            {
                var targetToLoadOver = ParentFrameworkElement as Popup;
                if (targetToLoadOver != null)
                {
                    left = targetToLoadOver.HorizontalOffset;
                    top = targetToLoadOver.VerticalOffset;
                    width = targetToLoadOver.Width;
                    height = targetToLoadOver.Height;
                }

                this.HorizontalOffset = left + (width / 6);
                this.VerticalOffset = top - (height / 1.5);
            }
            else
            {
                var targetToLoadOver = ParentFrameworkElement as Window;
                if (targetToLoadOver != null)
                {
                    left = targetToLoadOver.Left;
                    top = targetToLoadOver.Top;
                    width = targetToLoadOver.Width;
                    height = targetToLoadOver.Height;
                }

                this.HorizontalOffset = left + (width / 2.5);
                this.VerticalOffset = (top + (height / 1.5) > this.Height) ? top + (height / 1.5) : 0;
            }
        }

        private void BtnRightAddMods_OnClick(object sender, RoutedEventArgs e)
        {
            if (LeftDataGridSelectedItem == null) return;

            // add to selected
            SelectedMods.Add(LeftDataGridSelectedItem);

            // remove from unselected
            var newModList = new ObservableCollection<ClientMod>();
            foreach (var unselectedMod in UnSelectedMods)
            {
                if (unselectedMod.Name != LeftDataGridSelectedItem.Name)
                    newModList.Add(unselectedMod);
            }
            UnSelectedMods = newModList;
            SortLists();
        }

        private void BtnLeftRemoveMods_OnClick(object sender, RoutedEventArgs e)
        {
            if (RightDataGridSelectedItem == null) return;

            // add to unselected
            UnSelectedMods.Add(RightDataGridSelectedItem);

            // remove to selected
            var newModList = new ObservableCollection<ClientMod>();
            foreach (var selectedMod in SelectedMods)
            {
                if (selectedMod.Name != RightDataGridSelectedItem.Name)
                    newModList.Add(selectedMod);
            }
            SelectedMods = newModList;
            SortLists();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var selectedMod in SelectedMods)
            {
                UnSelectedMods.Add(selectedMod);
            }
            SelectedMods = new ObservableCollection<ClientMod>();
            SortLists();
        }
    }
}
