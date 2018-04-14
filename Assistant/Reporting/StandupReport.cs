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
using WeatherUnderground;

namespace Reporting
{
    public class StandupReport
    {
        private CalendarServices GoogleClient;
        private KanbanFlow KanbanClient;
        private WeatherClient WeatherReporter;
        public StandupReport(CalendarServices googleClient, KanbanFlow kanbanClient, WeatherClient weatherClient)
        {
            GoogleClient = googleClient;
            KanbanClient = kanbanClient;
            WeatherReporter = weatherClient;
        }
        private string exceptionString;

        public string ErrorMessage
        {
            get { return exceptionString; }
        }

        public string GenerateReport(int daysOffset = 0, TimeSpan? workableHours = null, double targetProductivity = 0.75)
        {
            if (!workableHours.HasValue)
            {
                workableHours = new TimeSpan(12, 0, 0);
            }
            // Check if we can generate the report
            if (!(GoogleClient.SignedIn && KanbanClient.SignedIn))
            {
                exceptionString = "Missing authorization from either Google Client of Kanban Client";
                return "";
            }
            List<GeneralizedEvent> todaysEvents = GoogleClient.GetTodaysEvents(daysOffset);
            List<GeneralizedTask> dueToday = KanbanClient.GetDueToday(daysOffset);
            List<GeneralizedTask> dueTomorrow = KanbanClient.GetDueTomorrow(daysOffset);
            List<GeneralizedTask> dueThisWeek = KanbanClient.GetDueLaterThisWeek(daysOffset);

            Forecast weatherReport = WeatherReporter.GetDailyWeatherReport("", "", daysOffset);

            TimeSpan obligatedTime = new TimeSpan();
            foreach (GeneralizedEvent e in todaysEvents)
            {
                if (e.Duration.HasValue)
                    obligatedTime += e.Duration.Value;
            }

            // workableHours guaranteed to have value because of first conditional
            double anticipatedMinutes = workableHours.Value.TotalMinutes - obligatedTime.TotalMinutes;
            anticipatedMinutes *= targetProductivity;

            // Note that I'm considering extra time to be "the amount of time I can reasonably do nothing"
            // I'll still allocate 8% of the total time as overhead that I don't consider anywhere. This could be used
            // as travel time, etc. 
            var multiplier = 1 - targetProductivity - 0.08;
            if (multiplier < 0)
                multiplier = 0;

            double slackOffMinutes = workableHours.Value.TotalMinutes - obligatedTime.TotalMinutes;
            slackOffMinutes *= multiplier;
            TimeSpan slackOffTime = new TimeSpan(0, (int)slackOffMinutes, 0);

            // Pomodoro Technique:
            // Work 25 minutes then take a 5 minute break. Every 4th break, take a 30 minute break instead
            // 1 cycle: (25 + 5) + (25 + 5) + (25 + 5) + (25 + 30) = 145 minutes
            // 4 pomodoros can be completed every 145 minutes

            int pomCount = (int)((anticipatedMinutes / 145) * 4);

            string path = createDocument(todaysEvents, dueToday, dueTomorrow, dueThisWeek, obligatedTime, weatherReport, pomCount, daysOffset, slackOffTime);
            //Process p = new Process();
            //p.StartInfo = new ProcessStartInfo("Test.pdf");
            //p.Start();
            return path;
        }

        //private void createDocument()
        private string createDocument(List<GeneralizedEvent> events, List<GeneralizedTask> dueToday, List<GeneralizedTask> dueTomorrow,
            List<GeneralizedTask> dueThisWeek, TimeSpan obligatedTime, Forecast weather, int pomCount, int daysOffset = 0, TimeSpan? slackOffTime = null)
        {

            // Define fonts:
            var regular = new Font(FontFamily.HELVETICA, 11, Font.NORMAL, new BaseColor(0, 0, 0));
            var bold = new Font(FontFamily.HELVETICA, 11, Font.BOLD, new BaseColor(0, 0, 0));
            var boldItalic = new Font(FontFamily.HELVETICA, 11, Font.BOLDITALIC, new BaseColor(0, 0, 0));
            var italic = new Font(FontFamily.HELVETICA, 11, Font.ITALIC, new BaseColor(0, 0, 0));

            //bool moreEvents = false, moreDueToday = false, moreDueTomorrow = false, moreDueLater = false;
            // Limit number of events and tasks to display
            var d = DateTime.Now.AddDays(daysOffset);
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

            string absolutePath = Path.Combine(path, $"{title}.pdf");
            FileStream fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
            Document doc = new Document(PageSize.LETTER, 72, 72, 36, 36);
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();
            Header header = new Header("Authored By", "Trevor T");
            doc.Add(header);
            //doc.Add(new Paragraph($"Hello,\nToday is {DateTime.Now.ToLongDateString()}.", new Font(FontFamily.HELVETICA, 20, Font.BOLD, new BaseColor(0, 0, 0))));
            Paragraph standupTitle = new Paragraph($"Standup for {d.ToLongDateString()}", new Font(FontFamily.HELVETICA, 20, Font.BOLD, new BaseColor(0, 0, 0)));
            standupTitle.Alignment = Element.ALIGN_CENTER;
            doc.Add(standupTitle);

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
                if(t.Completed)
                    l.Add(new Chunk($"(Done) ", italic));

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
                if (t.Completed)
                    l.Add(new Chunk($"(Done) ", italic));

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
                switch (t.DueDate.Value.DayOfWeek)
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
                if (t.Completed)
                    l.Add(new Chunk($"(Done) ", italic));

                l.Add(new Chunk($"{t.Name}    ", bold));
                l.Add(new Chunk($"(Due on {dayOfWeek})", italic));
                listTasksThisWeek.Add(l);
            }
            listTasksThisWeek.IndentationLeft = 35;
            doc.Add(listTasksThisWeek);
            #endregion

            //Paragraph scheduled = new Paragraph((float)40.0);
            //scheduled.Add(new Chunk("Hours Scheduled: ", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            //scheduled.Add(new Chunk($"{obligatedTime.Hours} hours {obligatedTime.Minutes} minutes", new Font(FontFamily.HELVETICA, 16, Font.UNDERLINE, new BaseColor(0, 0, 0))));

            //Paragraph poms = new Paragraph((float)20.0);
            //poms.Add(new Chunk("Pomodoros to Complete: ", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            //poms.Add(new Chunk($"{pomCount}", new Font(FontFamily.HELVETICA, 16, Font.UNDERLINE, new BaseColor(0, 0, 0))));

            //doc.Add(scheduled);
            //doc.Add(poms);


            //if (slackOffTime.HasValue)
            //{
            //    Paragraph slack = new Paragraph((float)20.0);
            //    slack.Add(new Chunk("Extra Time: ", new Font(FontFamily.HELVETICA, 16, Font.BOLD, new BaseColor(0, 0, 0))));
            //    slack.Add(new Chunk($"{slackOffTime.Value.Hours} hours {slackOffTime.Value.Minutes} minutes", new Font(FontFamily.HELVETICA, 16, Font.UNDERLINE, new BaseColor(0, 0, 0))));
            //    doc.Add(slack);
            //}

            #region Summary Table
            PdfPTable table = new PdfPTable(2);
            table.SpacingBefore = 40;
            table.WidthPercentage = 100;
            PdfPCell hoursTitle = new PdfPCell(new Phrase("Hours Scheduled: ", new Font(FontFamily.HELVETICA, 14, Font.BOLD, new BaseColor(0, 0, 0))));
            hoursTitle.HorizontalAlignment = Element.ALIGN_LEFT;
            hoursTitle.Border = Rectangle.TOP_BORDER;
            table.AddCell(hoursTitle);
            PdfPCell hours = new PdfPCell(new Phrase($"{obligatedTime.Hours} hours {obligatedTime.Minutes} minutes", new Font(FontFamily.HELVETICA, 14, Font.NORMAL, new BaseColor(0, 0, 0))));
            hours.HorizontalAlignment = Element.ALIGN_RIGHT;
            hours.Border = Rectangle.TOP_BORDER;
            table.AddCell(hours);

            PdfPCell pomsTitle = new PdfPCell(new Phrase("Pomodoros to Complete: ", new Font(FontFamily.HELVETICA, 14, Font.BOLD, new BaseColor(0, 0, 0))));
            pomsTitle.HorizontalAlignment = Element.ALIGN_LEFT;
            pomsTitle.Border = Rectangle.NO_BORDER;
            table.AddCell(pomsTitle);
            PdfPCell poms = new PdfPCell(new Phrase($"{pomCount}", new Font(FontFamily.HELVETICA, 14, Font.NORMAL, new BaseColor(0, 0, 0))));
            poms.HorizontalAlignment = Element.ALIGN_RIGHT;
            poms.Border = Rectangle.NO_BORDER;
            table.AddCell(poms);

            if(slackOffTime.HasValue)
            {
                PdfPCell slackTitle = new PdfPCell(new Phrase("Extra Time: ", new Font(FontFamily.HELVETICA, 14, Font.BOLD, new BaseColor(0, 0, 0))));
                slackTitle.HorizontalAlignment = Element.ALIGN_LEFT;
                slackTitle.Border = Rectangle.BOTTOM_BORDER;
                table.AddCell(slackTitle);
                PdfPCell slack = new PdfPCell(new Phrase($"{slackOffTime.Value.Hours} hours {slackOffTime.Value.Minutes} minutes", new Font(FontFamily.HELVETICA, 14, Font.NORMAL, new BaseColor(0, 0, 0))));
                slack.HorizontalAlignment = Element.ALIGN_RIGHT;
                slack.Border = Rectangle.BOTTOM_BORDER;
                table.AddCell(slack);
            }

            Image img = Image.GetInstance(weather.icon_url);
            PdfPCell leftWeatherCell = new PdfPCell(img,false);
            leftWeatherCell.HorizontalAlignment = Element.ALIGN_LEFT;
            leftWeatherCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            leftWeatherCell.Border = Rectangle.BOTTOM_BORDER;
            table.AddCell(leftWeatherCell);
            PdfPCell rightWeatherCell = new PdfPCell(new Phrase(weather.fcttext, new Font(FontFamily.HELVETICA, 14, Font.NORMAL, new BaseColor(0, 0, 0))));
            rightWeatherCell.HorizontalAlignment = Element.ALIGN_JUSTIFIED;
            rightWeatherCell.Border = Rectangle.BOTTOM_BORDER;
            table.AddCell(rightWeatherCell);

            doc.Add(table);
            #endregion

            Paragraph generated = new Paragraph((float)10.0, $"(Generated on {DateTime.Now.ToShortDateString()} at {DateTime.Now.ToShortTimeString()})", new Font(FontFamily.HELVETICA, 9, Font.ITALIC, new BaseColor(0, 0, 0)));
            generated.Alignment = Element.ALIGN_RIGHT;
            doc.Add(generated);
            doc.AddHeader("Date Generated", $"{DateTime.Now.ToShortDateString()}");

            doc.Close();

            return absolutePath;
        }
    }
}
