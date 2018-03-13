using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanbanFlowClient;
using KanbanFlowClient.Classes;
using GoogleCalendar;
using Newtonsoft.Json;
using Reporting;

namespace Assistant
{
    class Program
    {
        static void Main(string[] args)
        {
            KanbanFlow kb = new KanbanFlow();
            kb.SignIn();
            kb.populateBoard();
            kb.populateDueDates();

            CalendarServices cs = new CalendarServices();
            cs.SignIn();

            StandupReport report = new StandupReport(cs, kb);
            report.GenerateReport(1);
        }
    }
}
