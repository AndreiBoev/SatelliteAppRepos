using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;

namespace SatelliteApp
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Error(e.ExceptionObject.ToString());
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Error(e.Exception.ToString());
        }

        private void Error(string exception)
        {
            var response = MessageBox.Show("Обнаружено необработанное исключение. Отчёт будет отправлен разработчику, а приложение перезапущено.", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
            if (response == MessageBoxResult.Yes)
            {
                try
                {
                    MailAddress from = new MailAddress("vigri.i.2@mail.ru", "Satellite");
                    MailAddress to = new MailAddress("vigri.i@mail.ru");
                    MailMessage m = new MailMessage(from, to);
                    m.Subject = "Error";
                    m.Body = exception;
                    SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587);
                    smtp.Credentials = new NetworkCredential("vigri.i.2@mail.ru", "p9hbs7sacyrZnwcGEXdU");
                    smtp.EnableSsl = true;
                    smtp.Send(m);
                }
                catch { }
                System.Diagnostics.Process.Start(ResourceAssembly.Location);
                Environment.Exit(0);
            }
        }
    }
}
