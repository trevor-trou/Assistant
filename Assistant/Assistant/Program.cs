using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCalendar;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace Assistant
{
    class Program
    {
        static void Main(string[] args)
        {
            CalendarServices cal = new CalendarServices();
            cal.SignIn();
            Events s = cal.GetUpcomingEventsAndTime();
        }
    }
}
