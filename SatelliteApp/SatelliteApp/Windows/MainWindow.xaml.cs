using GMap.NET;
using GMap.NET.MapProviders;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Media;
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
using System.Windows.Threading;
using SatelliteApp.Classes;
using Newtonsoft.Json;
using System.Globalization;
using GMap.NET.WindowsPresentation;
using System.IO;
using System.Net.Http;

namespace SatelliteApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort _port = new SerialPort();
        private long _count;
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _timeNotification = new DateTime();
        private List<SelectedConfiguration> _selectedConfigurations = new List<SelectedConfiguration>();
        private Properties.Settings _settings = Properties.Settings.Default;
        private string _path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolderOption.Create) + @"\Satellite\";
        private static HttpClient _client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            BtnUpdatePorts_Click(null, null);
            _port.DataReceived += _port_DataReceived;
            _timer.Tick += _timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 1);
            DataContext = _settings;


            _selectedConfigurations.Add(new SelectedConfiguration());
            ICConfigurations.ItemsSource = _selectedConfigurations;

        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            TBlockTimer.Text = (_timeNotification.Second - DateTime.Now.Second).ToString();
            if (_timeNotification <= DateTime.Now)
            {
                _timer.Stop();
                SoundPlayer sound = new SoundPlayer();
                sound.Stream = Properties.Resources.Signal;
                sound.Play();
                MessageBox.Show("Потеря сигнала", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                sound.Stop();
            }
        }

        private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _count++;
            int second = _settings.TimeNotificationSetting;
            if (second != 0)
            {
                _timeNotification = DateTime.Now.AddSeconds(second);
                _timer.Start();
            }
            Dispatcher.Invoke(() =>
            {
                string _indata = _port.ReadLine();
                string dateTimeNow = DateTime.Now.ToString("HH:mm:ss.fff");
                TBData.Text += CBTimeMark.IsChecked.Value ? dateTimeNow + " -> " + _indata : _indata;
                _indata = _indata.Replace("\r", "");
                string[] data = _indata.Split(_settings.Separator);
                TBlockCount.Text = _count.ToString();

                using (StreamWriter sw = new StreamWriter(_path, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine(_indata = dateTimeNow + _settings.Separator + _indata);
                }

                if (CBAutoScroll.IsChecked.Value)
                    TBData.ScrollToEnd();
                if (int.Parse(TBDelay.Text) > _count)
                {
                    return;
                }
                try
                {
                    #region Логика отправки запроса API
                    if (data[0] == "api") // дописать проверку на интернет 
                    {
                        //+ data[1] + "&satlong=" + data[2] + "&sath=" + data[3];
                        /*string path = "http://" + _settings.DeviceUrl + "/api/v1/data/set/satgps";
                        string home_pos = "{\"key\":\"SATAPPSP\",\"lat\":" + data[1].Replace(",", ".") + ",\"lon\":" + data[2].Replace(",", ".") + ",\"height\":" + data[3].Replace(",", ".") + "}";
                        CreatePostRequestAsync(path, new StringContent(home_pos, Encoding.UTF8, "application/json"));*/

                        string path = "http://" + _settings.DeviceUrl + "/api/v1/data/set/satgps";
                        NetService.gps_data sat_pos = new NetService.gps_data { 
                            lat = Convert.ToDouble(data[1].Replace(".", ",")),
                            lon = Convert.ToDouble(data[2].Replace(".", ",")),
                            height = Convert.ToDouble(data[3].Replace(".", ","))
                        };
                        Task<HttpResponseMessage> task = NetService.Post(path, sat_pos);
                        return;
                    }
                    #endregion
                    SelectedConfiguration selectedConfiguration = _selectedConfigurations.FirstOrDefault(p => p.Value.Name == data[0]);
                    Configuration configuration;
                    if (selectedConfiguration != null)
                        configuration = selectedConfiguration.Value;
                    else
                        return;

                    for (int i = 0; i < SPParameters.Children.Count; i++)
                    {
                        if (SPParameters.Children[i] is TextBlock textBlock && textBlock.Text == data[0])
                        {
                            if (SPParameters.Children[i + 1] is ItemsControl currentList)
                            {
                                PointLatLng pointLatLng = new PointLatLng();
                                for (int j = 0; j < currentList.Items.Count; j++)
                                {
                                    CheckBox checkbox = currentList.Items[j] as CheckBox;
                                    checkbox.Content = configuration.Parameters[j].Name + " - " + data[j + 1];

                                    var chart = configuration.Parameters[j].Chart;
                                    if (chart != null)
                                    {
                                        chart.Data[chart.DataIndex] = double.Parse(data[j + 1], CultureInfo.InvariantCulture);
                                        chart.SignalPlot.MaxRenderIndex = chart.DataIndex;
                                        chart.DataIndex++;
                                        chart.wpfPlot.Plot.AxisAuto();
                                        chart.wpfPlot.Render();
                                    }

                                    if ((configuration.X != null) && configuration.Parameters[j].Name == configuration.X.Name)
                                    {
                                        pointLatLng.Lat = double.Parse(data[j + 1], CultureInfo.InvariantCulture);
                                    }
                                    if ((configuration.Y != null) && configuration.Parameters[j].Name == configuration.Y.Name)
                                    {
                                        pointLatLng.Lng = double.Parse(data[j + 1], CultureInfo.InvariantCulture);
                                    }
                                }
                                (MapMain.Markers.FirstOrDefault() as GMapRoute).Points.Add(pointLatLng);
                                var center = MapMain.Position;
                                MapMain.Position = pointLatLng;
                                if (!CBNewPointInCenter.IsChecked.Value) {
                                    MapMain.Position = center;

                                }
                                

                            }
                            break;
                        }
                    }
                }
                catch { }
            }
            );

        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (_port.IsOpen)
            {
                ImAction.Source =
                new BitmapImage(new Uri(@"/Assets/Icons/Start.png", UriKind.RelativeOrAbsolute));
                TBlockTimer.Text = TBlockCount.Text = "0";
                SPParameters.Children.Clear();

                _selectedConfigurations.Clear();
                ICConfigurations.ItemsSource = null;
                ICConfigurations.Items.Clear();
                _selectedConfigurations.Add(new SelectedConfiguration());
                ICConfigurations.ItemsSource = _selectedConfigurations;

                _count = 0;
                SPPort.IsEnabled = true;
                BtnSend.IsEnabled = false;
                TBSend.IsEnabled = false;
                _timer.Stop();
                _port.Close();
                (MapMain.Markers.FirstOrDefault() as GMapRoute).Points.Clear();
                MapMain.Position = new PointLatLng();
                ICCharts.Items.Clear();
                TBData.Clear();
                ICConfigurations.IsEnabled = true;
                BtnAddConfig.IsEnabled = true;
                _path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolderOption.Create) + @"\Satellite\";
            }
            else
            {
                try
                {
                    #region Проверка на дубликаты конфигураций
                    List<string> currentConfiguration = new List<string>();
                    foreach (var config in _selectedConfigurations)
                    {
                        currentConfiguration.Add(config.Value.Name);
                    }
                    if (currentConfiguration.Distinct().Count() != currentConfiguration.Count())
                    {
                        MessageBox.Show("Не может быть одновременно одинаковых конфигураций!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    #endregion
                    _port.PortName = CBPorts.Text;
                    _port.BaudRate = int.Parse(CBSpeed.Text);
                    _port.Open();
                    //save
                    _settings.Save();
                    ImAction.Source =
                    new BitmapImage(new Uri(@"/Assets/Icons/Stop.png", UriKind.RelativeOrAbsolute));
                    SPPort.IsEnabled = false;
                    BtnSend.IsEnabled = true;
                    TBSend.IsEnabled = true;
                    ICConfigurations.IsEnabled = false;
                    BtnAddConfig.IsEnabled = false;

                    DirectoryInfo dirInfo = new DirectoryInfo(_path);
                    if (!dirInfo.Exists)
                    {
                        dirInfo.Create();
                    }
                    _path = _path + DateTime.Now.ToString("dd-MM-yyyy hh.mm.ss") + ".txt";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MapMain_Loaded(object sender, RoutedEventArgs e)
        {
            // Источники
            var provider = GMapProviders.TryGetProvider(_settings.MapProvider);
            if (provider == null)
            {
                MessageBox.Show("Карта не найдена. Выберите другую карту.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MapMain.MapProvider = provider;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            // Движение
            MapMain.CanDragMap = true;
            MapMain.DragButton = MouseButton.Left;
            //Zoom
            MapMain.MaxZoom = 18;
            MapMain.MinZoom = 2;
            MapMain.Zoom = 5;
            // Добавление слоя с маршрутом
            GMapRoute route = new GMapRoute(new List<PointLatLng>());
            MapMain.Markers.Add(route);
        }

        private void BtnUpdatePorts_Click(object sender, RoutedEventArgs e)
        {
            CBPorts.ItemsSource = SerialPort.GetPortNames();
            CBPorts.SelectedIndex = SerialPort.GetPortNames().Count() - 1;
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (_port.IsOpen)
            {
                _port.Write(TBSend.Text);
                TBSend.Clear();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TBData.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_port.IsOpen)
            {
                if (MessageBox.Show("Вы действительно хотите выйти? Порт будет отключен, а данные сохранены в " + _path, "Question", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            new Windows.SettingWindow().ShowDialog();
            InitializeComponent();
            Window_Loaded(null, null);
        }

        public class SelectedConfiguration
        {
            public Configuration Value { get; set; } = new Configuration();
            public List<Configuration> ConfigurationsBase { get; set; } = JsonConvert.DeserializeObject<List<Configuration>>(Properties.Settings.Default.ConfigurationsList);
        }

        private void BtnAddConfig_Click(object sender, RoutedEventArgs e)
        {
            _selectedConfigurations.Add(new SelectedConfiguration());
            ICConfigurations.Items.Refresh();
        }

        private void BtnDeleteConfig_Click(object sender, RoutedEventArgs e)
        {
            _selectedConfigurations.Remove((sender as Button).DataContext as SelectedConfiguration);
            ICConfigurations.Items.Refresh();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SPParameters.Children.Clear();
            foreach (var configuration in _selectedConfigurations)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Text = configuration.Value.Name;
                SPParameters.Children.Add(textBlock);

                ItemsControl itemsControl = new ItemsControl();

                foreach (var parameter in configuration.Value.Parameters)
                {
                    CheckBox checkBox = new CheckBox();
                    // костыль наверное...
                    TextBlock textParameter = new TextBlock();
                    textParameter.Text = parameter.Name.ToString();
                    checkBox.Content = textParameter;
                    //
                    checkBox.Click += CheckBox_Click; ;
                    checkBox.DataContext = parameter;
                    itemsControl.Items.Add(checkBox);
                }
                SPParameters.Children.Add(itemsControl);
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var parametr = checkbox.DataContext as Parameter;
            if (checkbox.IsChecked.Value)
            {
                ScottPlot.WpfPlot wpfPlot = new ScottPlot.WpfPlot();
                Chart chart = new Chart();
                chart.Data = new double[int.Parse(TBCountPoits.Text)];
                chart.SignalPlot = wpfPlot.Plot.AddSignal(chart.Data);
                wpfPlot.Plot.XLabel(parametr.Name);
                chart.wpfPlot = wpfPlot;
                wpfPlot.Refresh();
                ICCharts.Items.Add(wpfPlot);
                ICCharts.Items.Refresh();
                parametr.Chart = chart;
            }
            else
            {
                ICCharts.Items.Remove(parametr.Chart.wpfPlot);
                parametr.Chart = null;
                ICCharts.Items.Refresh();
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Данный функционал ещё не реализован", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TBCountPoits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void TBDelay_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void BtnMapZoomPlus_Click(object sender, RoutedEventArgs e)
        {
            MapMain.Zoom++;
        }

        private void BtnMapZoomMinus_Click(object sender, RoutedEventArgs e)
        {
            MapMain.Zoom--;
        }

        private void TBSend_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnSend_Click(null, null); 
        }
    }
}
