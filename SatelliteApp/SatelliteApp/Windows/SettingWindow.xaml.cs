using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GMap.NET.MapProviders;
using Microsoft.Win32;
using Newtonsoft.Json;
using SatelliteApp.Classes;
using System.Net.Http;

namespace SatelliteApp.Windows
{
    /// <summary>
    /// Логика взаимодействия для SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        private string _file = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GMap.NET\\TileDBv5\\en\\Data.gmdb";
        private List<Configuration> _configurations = new List<Configuration>();

        private static HttpClient _client = new HttpClient();
        private Properties.Settings _settings = Properties.Settings.Default;
        
        async public static void CreateGetRequestAsync(string path)
        {
            try
            {
                await _client.GetAsync(path);
            }
            catch { }
        }
        async public static void CreatePostRequestAsync(string path, StringContent args)
        {
            try
            {
                await _client.PostAsync(path,args);
            }
            catch { }
        }
        public SettingWindow()
        {
            InitializeComponent();
            int timeNotification = Properties.Settings.Default.TimeNotificationSetting;
            if (timeNotification != 0)
            {
                SPTimeNotification.IsEnabled = true;
                CBTimeNotification.IsChecked = true;
            }
            DataContext = _settings;
            TBTimeNotification.Text = timeNotification.ToString();
            DataContext = Properties.Settings.Default;
            CBMapProviders.ItemsSource = GMapProviders.List;
            _configurations = JsonConvert.DeserializeObject<List<Configuration>>(Properties.Settings.Default.ConfigurationsList);
            DGConfigurations.ItemsSource = _configurations;
            CBMapProviders.SelectedItem = GMapProviders.TryGetProvider(Properties.Settings.Default.MapProvider);
            TBHost.Text = _settings.DeviceUrl;
            TBLat.Text = _settings.HomeLat;
            TBLong.Text = _settings.HomeLong;
            TBAlt.Text = _settings.HomeAlt;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                SPTimeNotification.IsEnabled = true;
            }
            else
            {
                SPTimeNotification.IsEnabled = false;
                Properties.Settings.Default.TimeNotificationSetting = 0;
                TBTimeNotification.Text = "0";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _settings.Save();_settings.Save();
            List<string> currentConfiguration = new List<string>();
            foreach (var config in _configurations)
            {
                currentConfiguration.Add(config.Name);
            }
            if (currentConfiguration.Distinct().Count() != currentConfiguration.Count())
            {
                MessageBox.Show("Не может быть одновременно одинаковых конфигураций!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
                return;
            }
            Properties.Settings.Default.MapProvider = CBMapProviders.Text; 
            Properties.Settings.Default.ConfigurationsList = JsonConvert.SerializeObject(_configurations);
            Properties.Settings.Default.Save();
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void TBTimeNotification_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if ((sender as TextBox).Text.Length >= 1)
            {
                e.Handled = true;
            }
        }

        private void BtnImportMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "File format | *.gmdb";
            if (ofd.ShowDialog().Value)
            {
                try
                {
                    File.Delete(_file);
                    File.Copy(ofd.FileName, _file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportMap_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.Copy(_file, fbd.SelectedPath + "\\Data.gmdb");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MIAssignY_Click(object sender, RoutedEventArgs e)
        {
            var parameter = DGParameters.SelectedItem as Parameter;
            (DGConfigurations.SelectedItem as Configuration).Y = parameter;
            DGConfigurations_SelectionChanged(null, null);
        }

        private void MIAssignX_Click(object sender, RoutedEventArgs e)
        {
            var parameter = DGParameters.SelectedItem as Parameter;
            (DGConfigurations.SelectedItem as Configuration).X = parameter;
            DGConfigurations_SelectionChanged(null, null);
        }

        private void DGConfigurations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var config = DGConfigurations.SelectedItem as Configuration;
            TBlockX.Text = (config != null) && (config.X != null) ? config.X.Name : "";
            TBlockY.Text = (config != null) && (config.Y != null) ? config.Y.Name : "";
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TBEmail.Text) || string.IsNullOrWhiteSpace(TBName.Text) ||
                string.IsNullOrWhiteSpace(TBTopic.Text) || string.IsNullOrWhiteSpace(TBText.Text))
            {
                MessageBox.Show("Все поля обязательны для заполнения", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                BtnSend.IsEnabled = false;
                MailAddress from = new MailAddress("vigri.i.2@mail.ru", "Satellite");
                MailAddress to = new MailAddress("vigri.i@mail.ru");
                MailMessage m = new MailMessage(from, to);
                m.Subject = TBTopic.Text + " " + TBName.Text + " " + TBEmail.Text;
                m.Body = TBText.Text;
                SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587);
                smtp.Credentials = new NetworkCredential("vigri.i.2@mail.ru", "p9hbs7sacyrZnwcGEXdU");
                smtp.EnableSsl = true;
                smtp.Send(m);
                MessageBox.Show("Ваше сообщение было отправлено успешно. Mы свяжемся с Вами в ближайшее время.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                TBEmail.Clear();
                TBName.Clear();
                TBTopic.Clear();
                TBText.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            BtnSend.IsEnabled = true;
        }

        private void BtnSendLatLong_Click(object sender, RoutedEventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(TBLat.Text) || string.IsNullOrWhiteSpace(TBLong.Text) || string.IsNullOrWhiteSpace(TBAlt.Text))
            {
                MessageBox.Show("Все поля обязательны для заполнения", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            {
                try
                {
                    string path = "http://" + _settings.DeviceUrl + "/api/v1/data/set/homegps";
                    string home_pos = "{\"key\":\"SATAPPSP\",\"lat\":" + TBLat.Text.Replace(",", ".") + ",\"lon\":" + TBLong.Text.Replace(",", ".") + ",\"height\":" + TBAlt.Text.Replace(",", ".") + "}";
                    CreatePostRequestAsync(path, new StringContent(home_pos, Encoding.UTF8, "application/json"));
                    MessageBox.Show(home_pos, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSaveHost_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(TBHost.Text))
            {
                MessageBox.Show("Хост не указан", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            {
               _settings.DeviceUrl=TBHost.Text;
            }
        }
    }
}
