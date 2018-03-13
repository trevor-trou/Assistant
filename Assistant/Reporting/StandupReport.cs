using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanbanFlowClient;
using GoogleCalendar;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using static iTextSharp.text.Font;
using System.Diagnostics;
using ClassLibrary;
using System.Configuration;

namespace Reporting
{
    public class StandupReport
    {
        private CalendarServices GoogleClient;
        private KanbanFlow KanbanClient;
        public StandupReport(CalendarServices googleClient, KanbanFlow kanbanClient)
        {
            GoogleClient = googleClient;
            KanbanClient = kanbanClient;
        }
        private string exceptionString;

        public string ErrorMessage
        {
            get { return exceptionString; }
        }

        public bool GenerateReport(TimeSpan? workableHours = null, double targetProductivity = 0.75)
        {
            if (!workableHours.HasValue)
            {
                workableHours = new TimeSpan(12, 0, 0);
            }
            // Check if we can generate the report
            if (!(GoogleClient.SignedIn && KanbanClient.SignedIn))
            {
                exceptionString = "Missing authorization from either Google Client of Kanban Client";
                return false;
            }
            List<GeneralizedEvent> todaysEvents = GoogleClient.GetTodaysEvents();
            List<GeneralizedTask> dueToday = KanbanClient.GetDueToday();
            List<GeneralizedTask> dueTomorrow = KanbanClient.GetDueTomorrow();
            List<GeneralizedTask> dueThisWeek = KanbanClient.GetDueLaterThisWeek();

            TimeSpan obligatedTime = new TimeSpan();
            foreach (GeneralizedEvent e in todaysEvents)
            {
                if (e.Duration.HasValue)
                    obligatedTime += e.Duration.Value;
            }

            // workableHours guaranteed to have value because of first conditional
            double anticipatedMinutes = workableHours.Value.TotalMinutes - obligatedTime.TotalMinutes;
            anticipatedMinutes *= targetProductivity;

            // Pomodoro Technique:
            // Work 25 minutes then take a 5 minute break. Every 4th break, take a 30 minute break instead
            // 1 cycle: (25 + 5) + (25 + 5) + (25 + 5) + (25 + 30) = 145 minutes
            // 4 pomodoros can be completed every 145 minutes

            int pomCount = (int)((anticipatedMinutes / 145) * 4);

            createDocument(todaysEvents, dueToday, dueTomorrow, dueThisWeek, obligatedTime, pomCount);
            Process p = new Process();
            //p.StartInfo = new ProcessStartInfo("Test.pdf");
            //p.Start();
            return true;
        }

        //private void createDocument()
        private void createDocument(List<GeneralizedEvent> events, List<GeneralizedTask> dueToday, List<GeneralizedTask> dueTomorrow,
            List<GeneralizedTask> dueThisWeek, TimeSpan obligatedTime, int pomCount)
        {

            // Define fonts:
            var regular = new Font(FontFamily.HELVETICA, 11, Font.NORMAL, new BaseColor(0, 0, 0));
            var bold = new Font(FontFamily.HELVETICA, 11, Font.BOLD, new BaseColor(0, 0, 0));
            var boldItalic = new Font(FontFamily.HELVETICA, 11, Font.BOLDITALIC, new BaseColor(0, 0, 0));
            var italic = new Font(FontFamily.HELVETICA, 11, Font.ITALIC, new BaseColor(0, 0, 0));

            //bool moreEvents = false, moreDueToday = false, moreDueTomorrow = false, moreDueLater = false;
            // Limit number of events and tasks to display
            var d = DateTime.Now;
            var title = $"{d.Year}-{d.Month}-{d.Day}";
            string path = ConfigurationManager.AppSettings.Get("ReportStorage");
            if (File.Exists(Path.Combine(path, $"{title}.pdf")))
            {
                int i = 1;
                while (File.Exists(Path.Combine(path, $"{title} ({i}).pdf")))
                {
                    i++;
                }
                title = $"{title} ({i})";
            }

            FileStream fs = new FileStream(Path.Combine(path, $"{title}.pdf"), FileMode.Create, FileAccess.Write, FileShare.None);
            Document doc = new Document(PageSize.LETTER, 72, 72, 36, 36);
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();
            Header header = new Header("Authored By", "Trevor T");
            doc.Add(new Paragraph($"Hello,\nToday is {DateTime.Now.ToLongDateString()}.", new Font(FontFamily.HELVETICA, 20, Font.BOLD, new BaseColor(0, 0, 0))));

            #region Obligations
            doc.Add(new Paragraph((float)40.0, "Today's obligations:\n", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            List listEvents = new List(List.UNORDERED, 10f);
            listEvents.SetListSymbol("\u2022");
            //events.OrderBy(x => x.StartTime);
            foreach (GeneralizedEvent e in events)
            {
                var l = new ListItem();
                if (e.StartTime.HasValue && e.EndTime.HasValue)
                {
                    l.Add(new Chunk($"{e.Name}    ", bold));
                    l.Add(new Chunk($"({e.StartTime.Value.ToShortTimeString()} - {e.EndTime.Value.ToShortTimeString()})", italic));
                }
                else
                {
                    l.Add(new Chunk($"{e.Name}", bold));
                }
                    
                listEvents.Add(l);
            }
            listEvents.IndentationLeft = 35;
            doc.Add(listEvents);
            #endregion

            #region Due Today
            doc.Add(new Paragraph((float)40.0, "Tasks due today:\n", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            List listTasksToday = new List(List.UNORDERED, 10f);
            listTasksToday.SetListSymbol("\u2022");
            foreach (GeneralizedTask t in dueToday)
            {
                var l = new ListItem();
                l.Add(new Chunk($"{t.Name}    ", bold));
                l.Add(new Chunk($"(Due at {t.DueDate.Value.ToShortTimeString()})", italic));
                listTasksToday.Add(l);
            }
            listTasksToday.IndentationLeft = 35;
            doc.Add(listTasksToday);
            #endregion

            #region Due Tomorrow
            doc.Add(new Paragraph((float)40.0, "Tasks due tomorrow:\n", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            List listTasksTomorrow = new List(List.UNORDERED, 10f);
            listTasksTomorrow.SetListSymbol("\u2022");
            foreach (GeneralizedTask t in dueTomorrow)
            {
                var l = new ListItem();
                l.Add(new Chunk($"{t.Name}    ", bold));
                l.Add(new Chunk($"(Due at {t.DueDate.Value.ToShortTimeString()})", italic));
                listTasksTomorrow.Add(l);
            }
            listTasksTomorrow.IndentationLeft = 35;
            doc.Add(listTasksTomorrow);
            #endregion

            #region Due This Week
            doc.Add(new Paragraph((float)40.0, "Tasks due later this week:\n", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            List listTasksThisWeek = new List(List.UNORDERED, 10f);
            listTasksThisWeek.SetListSymbol("\u2022");
            foreach (GeneralizedTask t in dueThisWeek)
            {
                string dayOfWeek = "";
                switch(t.DueDate.Value.DayOfWeek)
                {
                    case DayOfWeek.Monday: dayOfWeek = "Monday"; break;
                    case DayOfWeek.Tuesday: dayOfWeek = "Tuesday"; break;
                    case DayOfWeek.Wednesday: dayOfWeek = "Wednesday"; break;
                    case DayOfWeek.Thursday: dayOfWeek = "Thursday"; break;
                    case DayOfWeek.Friday: dayOfWeek = "Friday"; break;
                    case DayOfWeek.Saturday: dayOfWeek = "Saturday"; break;
                    case DayOfWeek.Sunday: dayOfWeek = "Sunday"; break;
                }

                var l = new ListItem();
                l.Add(new Chunk($"{t.Name}    ", bold));
                l.Add(new Chunk($"(Due on {dayOfWeek})", italic));
                listTasksThisWeek.Add(l);
            }
            listTasksThisWeek.IndentationLeft = 35;
            doc.Add(listTasksThisWeek);
            #endregion

            Paragraph scheduled = new Paragraph((float)40.0);
            scheduled.Add(new Chunk("Hours Scheduled: ", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            scheduled.Add(new Chunk($"{obligatedTime.Hours} hours {obligatedTime.Minutes} minutes", new Font(FontFamily.HELVETICA, 16, Font.UNDERLINE, new BaseColor(0, 0, 0))));

            Paragraph poms = new Paragraph((float)20.0);
            poms.Add(new Chunk("Pomodoros to Complete: ", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            poms.Add(new Chunk($"{pomCount}", new Font(FontFamily.HELVETICA, 16, Font.UNDERLINE, new BaseColor(0, 0, 0))));

            doc.Add(scheduled);
            doc.Add(poms);
            //doc.Add(new Paragraph((float)40.0, $"Today you have {obligatedTime.Hours} hours {obligatedTime.Minutes} minutes obligated.", new Font(FontFamily.HELVETICA, 11, Font.BOLD, new BaseColor(0, 0, 0))));
            //doc.Add(new Paragraph((float)40.0, $"Today you should try to complete .", new Font(FontFamily.HELVETICA, 11, Font.BOLD, new BaseColor(0, 0, 0))));
            //doc.Add(new Paragraph((float)50.0, "Today's scheduled tasks:\n", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            //List list = new List(List.UNORDERED, 10f);
            //list.SetListSymbol("\u2022");
            //list.Add(new ListItem("One"));
            //list.Add(new ListItem("Two"));
            //list.IndentationLeft = 40;
            //doc.Add(list);

            doc.Close();
        }
    }
}
