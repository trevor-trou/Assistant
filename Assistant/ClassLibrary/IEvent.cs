using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary
{
    interface IEvent
    {
        string Name { get; set; }
        string Description { get; set; }
        string Location { get; set; }
        DateTime? StartTime { get; set; }
        DateTime? EndTime { get; set; }
        TimeSpan? Duration { get; set; }
    }
}
