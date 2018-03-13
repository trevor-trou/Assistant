using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using KanbanFlowClient.Classes;
using Newtonsoft.Json;
using System.Linq;

namespace KanbanFlowClient
{
    public class KanbanFlow
    {
        public Column[] boardContents { get; set; }
        private bool populated;
        private bool boardDatesSet;
        private string exceptionString;

        static Uri baseAddress = new Uri("https://kanbanflow.com/api/v1/");

        private string encodedAccessToken;
        public string accessToken
        {
            get { return encodedAccessToken; }
            set
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                encodedAccessToken = Convert.ToBase64String(bytes);
            }
        }

        public bool SignedIn
        {
            get { return encodedAccessToken != null; }
        }
        public KanbanFlow()
        {
            populated = false;
            boardDatesSet = false;
            exceptionString = "";
        }

        // Consider making a repository factory, that way we can program against an interface and execute unit tests
        public bool SignIn()
        {
            bool state = false;
            string path = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
            path = Path.Combine(path, ".credentials/kanbanflow.json");
            if (!File.Exists(path))
            {
                Console.WriteLine("Please input the KanbanFlow API Token: ");
                var token = Console.ReadLine();
                accessToken = $"apiToken:{token}";
                if (!testToken())
                {
                    encodedAccessToken = null;
                    Console.WriteLine("Invalid KanbanFlow API Token");
                    state = false; // Maybe have a "try again?"
                }
                else
                {
                    string credPath = System.Environment.GetFolderPath(
                            System.Environment.SpecialFolder.Personal);
                    credPath = System.IO.Path.Combine(credPath, ".credentials/kanbanflow.json");

                    Credential cred = new Credential();
                    cred.CredentialType = "apiToken";
                    cred.CredentialValue = token;

                    using (System.IO.StreamWriter fs = new System.IO.StreamWriter(credPath))
                    {
                        var str = JsonConvert.SerializeObject(cred, Formatting.Indented);
                        fs.WriteLine(str);
                    }
                    state = true;
                }
            }
            else
            {
                using (var streamReader = new StreamReader(path))
                {
                    try
                    {
                        string credFileContents = streamReader.ReadToEnd();
                        Credential cred = JsonConvert.DeserializeObject<Credential>(credFileContents);

                        accessToken = $"{cred.CredentialType}:{cred.CredentialValue}";
                        if (!testToken())
                        {
                            encodedAccessToken = null;
                            Console.WriteLine("The saved KanbanFlow API Token is no longer valid.");
                            state = false; // Maybe have a, "Please enter new token?"
                        }
                        else
                            state = true;
                    }
                    catch (Exception ex)
                    {
                        exceptionString = ex.ToString();
                        state = false;
                    }
                }
                if (state == false)
                {
                    File.Delete(path);
                }
            }

            return state;
        }

        public List<GeneralizedTask> GetDueToday(int daysOffset = 0)
        {
            DateTime todayLowerBound = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.AddDays(daysOffset).Day, 0, 0, 0);
            DateTime todayUpperBound = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.AddDays(daysOffset).Day, 23, 59, 59);

            return GetTasksDue(todayLowerBound, todayUpperBound);
        }

        public List<GeneralizedTask> GetDueTomorrow(int daysOffset = 0)
        {
            DateTime tomorrow = DateTime.Now.AddDays(1 + daysOffset);

            DateTime tomorrowLowerBound = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 0, 0, 0);
            DateTime tomorrowUpperBound = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 23, 59, 59);

            return GetTasksDue(tomorrowLowerBound, tomorrowUpperBound);
        }

        public List<GeneralizedTask> GetDueLaterThisWeek(int daysOffset = 0)
        {
            DateTime dayAfterTomorrow = DateTime.Now.AddDays(2 + daysOffset);
            DateTime oneWeekFromToday = DateTime.Now.AddDays(7 + daysOffset);
            DateTime lower = new DateTime(dayAfterTomorrow.Year, dayAfterTomorrow.Month, dayAfterTomorrow.Day, 0, 0, 0);
            DateTime upper = new DateTime(oneWeekFromToday.Year, oneWeekFromToday.Month, oneWeekFromToday.Day, 23, 59, 59);

            return GetTasksDue(dayAfterTomorrow, oneWeekFromToday);
        }
        private List<GeneralizedTask> GetTasksDue(DateTime lowerBound, DateTime upperBound, bool excludeDone = true)
        {
            List<GeneralizedTask> due = new List<GeneralizedTask>();

            // Check if the board is populated and the dates are set
            if (!(populated && boardDatesSet))
            {
                exceptionString = "Cannot get tasks due - board isn't populated or the due dates haven't been fetched.";
            }
            else
            {
                string doneColumnId = boardContents.Where(x => x.columnName.ToLower().Equals("done")).FirstOrDefault()?.columnId;
                // Time complexity for this is poor, however Kanban boards are unlikely to have a large enough data set. Especially since we
                // won't be considering the tasks in the done column.
                foreach (Column col in boardContents)
                {
                    if (col.columnId != doneColumnId || !excludeDone)
                    {
                        foreach (Task task in col.tasks)
                        {
                            if (task.dueDate.HasValue && task.dueDate.Value >= lowerBound && task.dueDate.Value <= upperBound)
                            {
                                due.Add(new GeneralizedTask()
                                {
                                    Name = task.name,
                                    Description = task.description,
                                    TimeEstimate = (new TimeSpan(0, 0, task.totalSecondsEstimate)),
                                    DueDate = task.dueDate.Value
                                });
                            }
                        }
                    }
                }
            }

            return due.OrderBy(x => x.DueDate).ToList();
        }
        private bool testToken()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedAccessToken);
                var resp = client.GetAsync("board").Result;
                if (resp.IsSuccessStatusCode)
                {
                    var brd = JsonConvert.DeserializeObject<Board>(resp.Content.ReadAsStringAsync().Result);
                    // Maybe consider doing this in a log file.
                    Console.WriteLine($"Access to board {brd.name} verified.");

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool populateBoard()
        {
            bool state = true;
            using (var client = new HttpClient())
            {
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedAccessToken);
                var resp = client.GetAsync("tasks").Result;
                if (resp.IsSuccessStatusCode)
                {
                    string responseString = resp.Content.ReadAsStringAsync().Result;
                    try
                    {
                        boardContents = JsonConvert.DeserializeObject<Column[]>(responseString);
                        populated = true;
                    }
                    catch (Exception ex)
                    {
                        exceptionString = ex.ToString();
                        state = false;
                    }
                }
                else
                {
                    exceptionString = resp.Content.ToString();
                    state = false;
                }
            }
            return state;
        }
        public bool populateDueDates()
        {
            bool state = true;
            string doneColumnId = boardContents.Where(x => x.columnName.ToLower().Equals("done")).FirstOrDefault()?.columnId;
            for (int i = 0; i < boardContents.Length; i++)
            {
                if (boardContents[i].columnId != doneColumnId)
                {
                    for (int j = 0; j < boardContents[i].tasks.Length; j++)
                    {
                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = baseAddress;
                            client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedAccessToken);
                            var resp = client.GetAsync($"tasks/{boardContents[i].tasks[j]._id}/dates").Result;
                            if (resp.IsSuccessStatusCode)
                            {
                                string responseString = resp.Content.ReadAsStringAsync().Result;
                                try
                                {
                                    Date[] returnedDates = JsonConvert.DeserializeObject<Date[]>(responseString);
                                    Date DueDate = returnedDates.Where(x => x.targetColumnId.Equals(doneColumnId)).FirstOrDefault();
                                    if (DueDate != null)
                                    {
                                        boardContents[i].tasks[j].dueDate = DueDate.dueTimestampLocal;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    exceptionString = ex.ToString();
                                    state = false;
                                }
                            }
                            else
                            {
                                exceptionString = resp.Content.ToString();
                                state = false;
                            }
                        }
                    }
                }
            }
            boardDatesSet = state;
            return state;
        }
        public bool isPopulated()
        {
            return populated;
        }
        public bool isDatesSet()
        {
            return boardDatesSet;
        }
        public string getException()
        {
            return exceptionString;
        }
    }
    //static void getBoard()
    //{
    //    using (var client = new HttpClient())
    //    {
    //        string encodedToken = Encode.EncodeToBase64(accessToken);
    //        client.BaseAddress = new Uri("https://kanbanflow.com/api/v1/");
    //        client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedToken);
    //        var resp = client.GetAsync("board").Result;
    //        //var response = client.PostAsJsonAsync("api/values", modelObject).Result;
    //        if (resp.IsSuccessStatusCode)
    //        {
    //            string responseString = resp.Content.ReadAsStringAsync().Result;
    //            Board returned = JsonConvert.DeserializeObject<Board>(responseString);
    //            TestPDFReport.createPDF(responseString);
    //            //TestPDFReport.createPDFWithTable();
    //        }
    //    }
    //}
}
