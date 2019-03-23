using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace DynamicCalendar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AppointmentShifter();
        }

        public void AppointmentShifter()
        {
            //string email = "test3@gmail.com";
            //string token = "FwBtkBnRfFMcnH2GfFcAhkzwI0su5bp4FvhMrRssZdY=";
            //int calendar_ID = 6600;
            //int center_ID = 4625;

            string email = TextBoxEmail.Text;
            string token = TextBoxToken.Text;
            int calendar_ID = Convert.ToInt32(TextBoxCalendar.Text);
            int center_ID = Convert.ToInt32(TextBoxCenter.Text);

            //GET all appointments
            string url_appointment_get = "https://" + "progenda.be/api/v2/calendars/" + calendar_ID + "/appointments?user_email=" + email + "&user_token=" + token;
            string test = new WebClient().DownloadString(url_appointment_get);
            RootObject appointments = JsonConvert.DeserializeObject<RootObject>(test);
            appointments.appointments = appointments.appointments.OrderBy(a => a.appointment.start).ToList();

            for (int i = 0; i < appointments.appointments.Count() - 1; i++)
            {
                Appointment app = appointments.appointments[i];
                Appointment app2 = appointments.appointments[i + 1];

                if (app.appointment.stop > app2.appointment.start)
                {
                    //Push 2nd appointment to a later time
                    int difference = app2.appointment.stop - app2.appointment.start;
                    app2.appointment.start = app.appointment.stop;
                    app2.appointment.stop = app2.appointment.start + difference;

                    //PUT appointment if affected
                    WebClient wc = new WebClient();
                    wc.Headers.Add("Content-Type", "application/json");
                    string body = "{\"appointment\":{\"start\":\"" + app2.appointment.start + "\", \"stop\":\"" + app2.appointment.stop + "\"}}";
                    byte[] postArray = Encoding.ASCII.GetBytes(body);
                    string url = "https://progenda.be/api/v2/calendars/" + calendar_ID + "/appointments/" + app2.appointment.id + "?user_email=" + email + "&user_token=" + token;
                    byte[] responseArray = wc.UploadData(url, "PUT", postArray);
                }
            }
        }
    }
}
