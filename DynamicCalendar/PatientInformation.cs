using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCalendar
{
    public class PatientInformation
    {
        public static void MakeFile(Appointment appointment, int center_id, string user_email, string user_token, int calendar_id)
        {
            string url = "https://" + "progenda.be/api/v2/centers/" + center_id + "/patients/" + appointment.appointment.patient_id + "?user_email=" + user_email + "&user_token=" + user_token;
            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent", "DynamicCalendar");
            string downloadString = webClient.DownloadString(url);
            PatientRootObject patientRootObject = JsonConvert.DeserializeObject<PatientRootObject>(downloadString);
            
            string patientString = "";
            patientString += "First name: " + patientRootObject.patient.first_name + "\r\n";
            patientString += "Last name: " + patientRootObject.patient.last_name + "\r\n";
            patientString += "Email: " + patientRootObject.patient.email + "\r\n";
            patientString += "Phone number: " + patientRootObject.patient.phone_number + "\r\n";
            patientString += "Notes: " + appointment.appointment.notes;

            WriteToFile(patientString);
        }

        public static void WriteToFile(string patientString)
        {
            FileIOPermission f = new FileIOPermission(PermissionState.None);
            f.AllLocalFiles = FileIOPermissionAccess.Write;

            string path =  Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Cancellations";
            // This text is added only once to the file.
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += "\\" + DateTime.Today.ToString("yyyy - MM - dd") + ".txt";
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("**********************");
                    sw.WriteLine(patientString);
                    sw.WriteLine("**********************\r\n");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("**********************");
                    sw.WriteLine(patientString);
                    sw.WriteLine("**********************\r\n");
                }
            }
        }
    }
}
