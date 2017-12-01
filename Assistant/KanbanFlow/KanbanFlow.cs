using System;
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
            set {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                encodedAccessToken = Convert.ToBase64String(bytes);
            }
        }

        public KanbanFlow(string accessToken, bool encodedBase64)
        {
            populated = false;
            boardDatesSet = false;
            exceptionString = "";
            if(encodedBase64)
            {
                encodedAccessToken = accessToken;
            }
            else
            {
                this.accessToken = accessToken;
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
                    catch(Exception ex)
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
            for(int i = 0; i < boardContents.Length; i++)
            {
                for(int j = 0; j < boardContents[i].tasks.Length; j++)
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
                                // Add logic to exclude Done column
                                Date[] returnedDates = JsonConvert.DeserializeObject<Date[]>(responseString);
                                Date DueDate = returnedDates.Where(x => x.targetColumnId.Equals(doneColumnId)).FirstOrDefault();
                                if(DueDate != null)
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
