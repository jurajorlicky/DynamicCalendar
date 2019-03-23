using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCalendar
{
    public class Patient
    {
        public int id { get; set; }
        public object remote_id { get; set; }
        public string last_name { get; set; }
        public string first_name { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public object birthdate { get; set; }
        public string address { get; set; }
        public string notes { get; set; }
        public string status { get; set; }
        public object metadata { get; set; }
        public string language_code { get; set; }
    }

    public class PatientRootObject
    {
        public Patient patient { get; set; }
    }
}
