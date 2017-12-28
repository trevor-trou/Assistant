using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class GeneralizedTask
    {
        string Name { get; set; }
        string Description { get; set; }
        TimeSpan TimeEstimate { get; set; }
        DateTime? DueDate { get; set; }
    }
}
