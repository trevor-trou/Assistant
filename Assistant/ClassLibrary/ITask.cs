using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary
{
    interface ITask
    {
        string Name { get; set; }
        string Description { get; set; }
        TimeSpan TimeEstimate { get; set; }
        DateTime? DueDate { get; set; }
        bool Completed { get; set; }
    }
}
