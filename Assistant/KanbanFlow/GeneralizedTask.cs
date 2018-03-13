using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanbanFlowClient
{
    public class GeneralizedTask : ITask
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan TimeEstimate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
