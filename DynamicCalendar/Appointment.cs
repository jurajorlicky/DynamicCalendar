using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCalendar
{
    public class Appointment2
    {
        public int id { get; set; }
        public object remote_id { get; set; }
        public int patient_id { get; set; }
        public string color { get; set; }
        public string title { get; set; }
        public string status { get; set; }
        public object patient_arrived_at { get; set; }
        public bool noshow { get; set; }
        public object metadata { get; set; }
        public int start { get; set; }
        public int stop { get; set; }
        public string notes { get; set; }
        public object patient_remote_id { get; set; }
        public object service_remote_id { get; set; }
    }

    public class Appointment
    {
        public Appointment2 appointment { get; set; }
    }

    public class RootObject
    {
        public List<Appointment> appointments { get; set; }
    }
}
