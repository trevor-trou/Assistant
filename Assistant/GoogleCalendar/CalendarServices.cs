using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using ClassLibrary;

namespace GoogleCalendar
{
    public class CalendarServices
    {
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string ApplicationName = "Assistant";

        private bool signedIn;
        private UserCredential credential;
        private CalendarService service;
        public bool SignedIn
        {
            get { return signedIn; }
        }

        private string exceptionString;


        public CalendarServices()
        {
            signedIn = false;
        }

        public bool SignIn()
        {
            string path = ConfigurationManager.AppSettings.Get("GoogleCalClientSecretLocation");
            if(!File.Exists(path))
            {
                signedIn = false;
                exceptionString = "Missing client_secret.json (Did you register this application with the Google Developers Console?)";
                return false;
            }
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/assistant.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            if (startService())
                return true;
            else
                return false;
        }

        // Helper methods
        private bool startService()
        {
            try
            {
                service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Report-specific methods
        /// <summary>
        /// Get today's events
        /// </summary>
        /// <returns></returns>
        public List<GeneralizedEvent> GetEvents()
        {
            return GetEvents(DateTime.Now, new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59));
        }
        public List<GeneralizedEvent> GetEvents(DateTime timeMin, DateTime timeMax)
        {
            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.TimeMax = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.bn 
            Events events = request.Execute();
            return events.Items.Select(x => new GeneralizedEvent()
            {
                Name = x.Summary,
                Description = x.Description,
                Location = x.Location,
                StartTime = x.Start.DateTime,
                EndTime = x.End.DateTime,
                Duration = (x.Start.DateTime.HasValue && x.End.DateTime.HasValue) ? x.Start.DateTime.Value - x.End.DateTime.Value : new TimeSpan()
            }).ToList();
        }
    }
}
