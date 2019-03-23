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

        private string email;
        private string token;
        private int calendar_ID;
        private int center_ID;
        private WebClient wc = new WebClient();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            email = TextBoxEmail.Text;
            token = TextBoxToken.Text;
            calendar_ID = Convert.ToInt32(TextBoxCalendar.Text);
            center_ID = Convert.ToInt32(TextBoxCenter.Text);

            AppointmentShifter();
            cancelLateAppointments();
        }

        public void AppointmentShifter()
        {
            //GET all appointments
            string url_appointment_get = "https://" + "progenda.be/api/v2/calendars/" + calendar_ID + "/appointments?user_email=" + email + "&user_token=" + token;
            string test = wc.DownloadString(url_appointment_get);
            RootObject appointments = JsonConvert.DeserializeObject<RootObject>(test);
            appointments.appointments = appointments.appointments.Where(a => !a.appointment.status.Equals("cancelled")).
                OrderBy(a => a.appointment.start).ToList();

            for (int i = 0; i < appointments.appointments.Count() - 1; i++)
            {
                Appointment app = appointments.appointments[i];
                Appointment app2 = appointments.appointments[i + 1];

                if (app.appointment.stop > app2.appointment.start && !(app.appointment.color == "#FFEB3B" 
                    && app2.appointment.color == "#FFEB3B"))
                {
                    if (app2.appointment.color == "#FFEB3B") {
                        int dif = app.appointment.stop - app.appointment.start;
                        app.appointment.start = app2.appointment.stop;
                        app.appointment.stop = app.appointment.start + dif;

                        //PUT appointment if affected
                        wc.Headers.Add("Content-Type", "application/json");
                        string body = "{\"appointment\":{\"start\":\"" + app.appointment.start + "\", \"stop\":\"" + app.appointment.stop + "\"}}";
                        byte[] postArray = Encoding.ASCII.GetBytes(body);
                        string url = "https://progenda.be/api/v2/calendars/" + calendar_ID + "/appointments/" + app.appointment.id + "?user_email=" + email + "&user_token=" + token;
                        byte[] responseArray = wc.UploadData(url, "PUT", postArray);
                        appointments.appointments.Remove(app2);
                        i--;
                    }
                    else {
                        int difference = app2.appointment.stop - app2.appointment.start;
                        app2.appointment.start = app.appointment.stop;
                        app2.appointment.stop = app2.appointment.start + difference;

                        //PUT appointment if affected
                        wc.Headers.Add("Content-Type", "application/json");
                        string body = "{\"appointment\":{\"start\":\"" + app2.appointment.start + "\", \"stop\":\"" + app2.appointment.stop + "\"}}";
                        byte[] postArray = Encoding.ASCII.GetBytes(body);
                        string url = "https://progenda.be/api/v2/calendars/" + calendar_ID + "/appointments/" + app2.appointment.id + "?user_email=" + email + "&user_token=" + token;
                        byte[] responseArray = wc.UploadData(url, "PUT", postArray);
                    }
                    //Push 2nd appointment to a later time
                    

                    
                }
            }
        }
        private void cancelLateAppointments() {
            string api = "https://progenda.be/api/v2";

            DateTime end = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 25, 16, 0, 0);

            string urlData = String.Empty;
            urlData = wc.DownloadString(api + "/centers?user_email=" + email + "&user_token=" + token);

            long unix = ToUnixTime(DateTime.Today);
            long unixEnd = ToUnixTime(end);

            string test = wc.DownloadString(api + "/calendars/" + calendar_ID + "/appointments?user_email=" +
                email + "&user_token=" + token);

            RootObject appointments = (RootObject)JsonConvert.DeserializeObject<RootObject>(test);
            List<Appointment> appointments2 = appointments.appointments
                .Where(app => FromUnixTime(app.appointment.start).Day.Equals(end.Day)).ToList();
            for (int i = 0; i < appointments2.Count(); i++) {
                Appointment app = appointments.appointments[i];
                DateTime time = FromUnixTime(app.appointment.stop).AddHours(1);
                int hour = time.Hour;
                int minute = time.Minute;
                bool cancelled = app.appointment.status.Equals("cancelled");
                if (hour >= end.Hour && !cancelled) {
                    try {
                        Appointment2 appointment = app.appointment;
                        #region put remote_id appointment
                        wc.Headers.Add("Content-Type", "application/json");
                        byte[] postArray = Encoding.ASCII.GetBytes("{\"appointment\":{\"remote_id\":\"" + appointment.id + "\"}}");
                        byte[] responseArray = wc.UploadData(new Uri("https://progenda.be/api/v2/calendars/" + calendar_ID +
                            "/appointments/" + appointment.id.ToString() + "?user_email=" + email + "&user_token=" + token), "PUT", postArray);
                        #endregion

                        getAndDeleteAppointment(appointment.id);
                    } catch (Exception ex) {
                        Appointment2 appointment = app.appointment;
                        getAndDeleteAppointment(appointment.id);
                    }
                }
            }

        }

        public void getAndDeleteAppointment(int id) {
            #region get appointment
            string appstring = wc.DownloadString("https://progenda.be/api/v2/calendars/" + calendar_ID +
                "/appointments/remote_id:" + id + "?user_email=" + email + "&user_token=" + token);
            Appointment app2 = JsonConvert.DeserializeObject<Appointment>(appstring);
            Appointment2 appointment2 = app2.appointment;
            #endregion

            #region delete appointment
            WebRequest request = WebRequest.Create("https://progenda.be/api/v2/calendars/" + calendar_ID +
                                "/appointments/remote_id:" + appointment2.remote_id + "?user_email=" + email + "&user_token=" + token);
            request.Method = "DELETE";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            #endregion
        }

        public static DateTime FromUnixTime(long unixTime) {
            return new DateTime(1970,1,1).AddSeconds(unixTime);
        }

        public static long ToUnixTime(DateTime date) {
            return (Int32)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
