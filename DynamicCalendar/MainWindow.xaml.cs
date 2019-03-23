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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;

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

        static string[] Scopes = { CalendarService.Scope.Calendar };
        static string ApplicationName = "Google Calendar API .NET Quickstart";
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

            GoogleCalendar();
            AppointmentShifter();
            cancelLateAppointments();
        }

        public void GoogleCalendar()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.

            string email = "test2@gmail.com";
            string token = "aLYj5i2DnGcqYSrW4OvdBrdbClI7j/hKpXeZyl0dzLw=";
            int calendar_ID = 6599;


            Events events = request.Execute();
            Console.WriteLine("Upcoming events:");
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    string end = eventItem.End.DateTime.ToString();
                    string id = eventItem.Id;
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                        end = eventItem.End.Date;
                    }
                    Console.WriteLine("{0}", id);
                    Console.WriteLine("{0} (startdate: {1} enddate: {2})", eventItem.Summary, when, end);
                    Appointment2 a = new Appointment2();
                    a.remote_id = id;
                    a.start = (Int32)ToUnixTime((DateTime)eventItem.Start.DateTime);
                    a.stop = (Int32)ToUnixTime((DateTime)eventItem.End.DateTime);
                    a.notes = "true";
                    a.title = eventItem.Summary;
                    a.color = "#FFEB3B";
                    a.status = "booked";

                    try
                    {
                        WebClient wc = new WebClient();
                        wc.Headers.Add("Content-Type", "application/json");
                        string body = "{\"appointment\":{\"start\":\"" + a.start + "\", \"stop\":\"" + a.stop + "\", \"remote_id\":\"" + a.remote_id + "\", \"notes\":\"" + a.notes + "\", \"title\":\"" + a.title + "\", \"color\":\"" + a.color + "\"}}";
                        byte[] postArray = Encoding.ASCII.GetBytes(body);
                        string url = "https://progenda.be/api/v2/calendars/" + calendar_ID + "/appointments?user_email=" + email + "&user_token=" + token;
                        byte[] responseArray = wc.UploadData(url, "POST", postArray);
                    }
                    catch (Exception)
                    {
                        WebClient w = new WebClient();
                        w.Headers.Add("Content-Type", "application/json");
                        string body = "{\"appointment\":{\"start\":\"" + a.start + "\", \"stop\":\"" + a.stop + "\", \"remote_id\":\"" + a.remote_id + "\", \"notes\":\"" + a.notes + "\", \"title\":\"" + a.title + "\", \"color\":\"" + a.color + "\", \"status\":\"" + a.status + "\"}}";
                        byte[] postArray = Encoding.ASCII.GetBytes(body);
                        string url = "https://progenda.be/api/v2/calendars/" + calendar_ID + "/appointments/remote_id:" + a.remote_id + "?user_email=" + email + "&user_token=" + token;
                        byte[] responseArray = w.UploadData(url, "PUT", postArray);
                    }
                }
            }
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

            DateTime end = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 24, 16, 0, 0);

            string urlData = String.Empty;
            urlData = wc.DownloadString(api + "/centers?user_email=" + email + "&user_token=" + token);

            long unix = ToUnixTime(DateTime.Today);
            long unixEnd = ToUnixTime(end);

            string test = wc.DownloadString(api + "/calendars/" + calendar_ID + "/appointments?user_email=" +
                email + "&user_token=" + token);

            RootObject appointments = (RootObject)JsonConvert.DeserializeObject<RootObject>(test);
            List<Appointment> appointments2 = appointments.appointments
                .Where(app => app.appointment.status.Equals("booked") && app.appointment.color != "#FFEB3B")
                .ToList();
            for (int i = 0; i < appointments2.Count(); i++) {
                Appointment app = appointments2[i];
                DateTime time = FromUnixTime(app.appointment.stop).AddHours(1);
                int hour = time.Hour;
                int minute = time.Minute;
                if (time.TimeOfDay > end.TimeOfDay) {
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

        public static long ToUnixTime(DateTime date = new DateTime())
        {
            return (Int32)(date.Subtract(new DateTime(1970, 1, 1, 1, 0, 0))).TotalSeconds;
        }
    }
}
