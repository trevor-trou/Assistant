using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace GoogleCalendar
{
    public class GeneralizedEvent : IEvent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
