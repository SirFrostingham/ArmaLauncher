using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ArmaLauncher.Controls;
using ArmaLauncher.Properties;
using ComboBox = System.Windows.Controls.ComboBox;
using DragEventArgs = System.Windows.DragEventArgs;

namespace ArmaLauncher
{
    /// <summary>
    /// Interaction logic for AppSettings.xaml
    /// </summary>
    public partial class AppSettings : Page, INotifyPropertyChanged
    {

        public AppSettings()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            var eventToFire = PropertyChanged;
            if (eventToFire == null)
                return;

            eventToFire(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnSetDefaults_Click(object sender, RoutedEventArgs e)
        {
            Globals.Current.CloseLauncherApp = Convert.ToBoolean(ConfigurationManager.AppSettings["CloseLauncherApp"]);
            //Globals.Current.Arma2LaunchParams = ConfigurationManager.AppSettings["Arma2LaunchParams"];
            //Globals.Current.Arma2Path = ConfigurationManager.AppSettings["Arma2Path"];
            Globals.Current.WindowedMode = Convert.ToBoolean(ConfigurationManager.AppSettings["WindowedMode"]);
            Globals.Current.GlobalAutoMatchClientMod = Convert.ToBoolean(ConfigurationManager.AppSettings["GlobalAutoMatchClientMod"]);
            //Globals.Current.Arma2OAPath = ConfigurationManager.AppSettings["Arma2OAPath"];
            //Globals.Current.Arma3Path = ConfigurationManager.AppSettings["Arma3Path"];
            Globals.Current.DebugModeDoNotLaunchGame = Convert.ToBoolean(ConfigurationManager.AppSettings["DebugModeDoNotLaunchGame"]);
            Globals.Current.DebugModeTestLocalOnly = Convert.ToBoolean(ConfigurationManager.AppSettings["DebugModeTestLocalOnly"]);
            Globals.Current.RefreshExtended = Convert.ToBoolean(ConfigurationManager.AppSettings["RefreshExtended"]);
            //Globals.Current.Arma3LaunchParams = ConfigurationManager.AppSettings["Arma3LaunchParams"];
            Globals.Current.IncludeArma2PathInArma3LaunchParams = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeArma2PathInArma3LaunchParams"]);
            Globals.Current.IncludeArma2OAPathInArma3LaunchParams = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeArma2OAPathInArma3LaunchParams"]);
            //Globals.Current.AdditionalArmaPaths1 = ConfigurationManager.AppSettings["AdditionalArmaPaths1"];
            //Globals.Current.AdditionalArmaPaths2 = ConfigurationManager.AppSettings["AdditionalArmaPaths2"];
            Globals.Current.IncludeAddtionalArmaPaths1InArma2LaunchParams = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAddtionalArmaPaths1InArma2LaunchParams"]);
            Globals.Current.IncludeAdditionalArmaPaths1InArma3LaunchParams = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAdditionalArmaPaths1InArma3LaunchParams"]);
            Globals.Current.IncludeAdditionalArmaPaths2InArma2LaunchParams = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAdditionalArmaPaths2InArma2LaunchParams"]);
            Globals.Current.IncludeAdditionalArmaPaths2InArma3LaunchParams = Convert.ToBoolean(ConfigurationManager.AppSettings["IncludeAdditionalArmaPaths2InArma3LaunchParams"]);
            Globals.Current.Arma2OAExe = ConfigurationManager.AppSettings["Arma2OAExe"];
            Globals.Current.Arma3Exe = ConfigurationManager.AppSettings["Arma3Exe"];
            Globals.Current.SelectedGameType = ConfigurationManager.AppSettings["SelectedGameType"];
            Globals.Current.Arma2SupportedModList = ConfigurationManager.AppSettings["Arma2SupportedModList"];
            Globals.Current.AppNotes = ConfigurationManager.AppSettings["AppNotes"];
        }

        private void ButtonArma2Path_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = "";
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                folderPath = folderBrowserDialog1.SelectedPath;
                Globals.Current.Arma2Path = folderPath;
            }
        }

        private void ButtonArma2OAPath_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = "";
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                folderPath = folderBrowserDialog1.SelectedPath;
                Globals.Current.Arma2OAPath = folderPath;
            }
        }

        private void ButtonArma3Path_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = "";
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                folderPath = folderBrowserDialog1.SelectedPath;
                Globals.Current.Arma3Path = folderPath;
            }
        }

        private void btnModChooser_Click(object sender, RoutedEventArgs e)
        {
            new EditModsPopup();
        }
    }
}
