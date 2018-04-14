using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WeatherUnderground
{
    public class WeatherClient
    {
        static string baseAddress = "http://api.wunderground.com/api";

        private string accessToken;
        private string defaultCity;
        private string defaultState;
        public bool AccessTokenSet
        {
            get { return (accessToken != null) && (accessToken != ""); }
        }

        public WeatherClient()
        {
            // Attempt to get the access token
            string path = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
            path = Path.Combine(path, ".credentials/WeatherUnderground.json");

            if(File.Exists(path))
            {
                using (var streamReader = new StreamReader(path))
                {
                    try
                    {
                        string credentialFileContents = streamReader.ReadToEnd();
                        Settings wugSettings = JsonConvert.DeserializeObject<Settings>(credentialFileContents);

                        accessToken = wugSettings.apiKey;
                        defaultCity = wugSettings.DefaultCity;
                        defaultState = wugSettings.DefaultState;
                    }
                    catch(Exception)
                    {

                    }
                }
            }
        }

        public bool SetToken(bool overrideOldToken = false)
        {
            if(overrideOldToken || String.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Please input a valid WeatherUnderground API Key: ");
                var token = Console.ReadLine();

                Console.WriteLine("Please specify a default city for Weather Reports (city, ST): ");

                string info = Console.ReadLine();

                string defaultCity = "";
                string defaultState = "";
                if(!String.IsNullOrEmpty(info))
                {
                    // TODO: Add more string handling.
                    defaultCity = info.Split(',')[0].Trim(' ');
                    defaultState = info.Split(',')[1].Trim(' ');
                    this.defaultCity = defaultCity;
                    this.defaultState = defaultState;
                }

                string credPath = System.Environment.GetFolderPath(
                            System.Environment.SpecialFolder.Personal);
                credPath = System.IO.Path.Combine(credPath, ".credentials/WeatherUnderground.json");

                Settings wugSettings = new Settings();
                wugSettings.apiKey = token;
                wugSettings.DefaultCity = defaultCity;
                wugSettings.DefaultState = defaultState;

                try
                {
                    using (System.IO.StreamWriter fs = new System.IO.StreamWriter(credPath))
                    {
                        var str = JsonConvert.SerializeObject(wugSettings, Formatting.Indented);
                        fs.WriteLine(str);
                    }
                    Console.WriteLine("WeatherUnderground API Key successfully saved.");
                }
                catch(Exception)
                {
                    Console.WriteLine("Error saving WeatherUnderground API Key.");
                    return false;
                }
            }
            return true;
        }

        public Forecast GetDailyWeatherReport(string cityName = "", string state = "", int daysOffset = 0, bool overrideIcons = true)
        {
            Forecast toReturn = null;
            if(cityName == "" || state == "")
            {
                cityName = defaultCity;
                state = defaultState;
            }

            if(daysOffset < 0 || daysOffset > 3)
            {
                return null;
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"{baseAddress}/{accessToken}/forecast/q/{state}/");
                // client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedAccessToken);
                var resp = client.GetAsync($"{cityName}.json").Result;
                if (resp.IsSuccessStatusCode)
                {
                    string responseString = resp.Content.ReadAsStringAsync().Result;
                    IList<Forecast> dailyForecasts = new List<Forecast>();
                    try
                    {
                        //Response = responseString;
                        //boardContents = JsonConvert.DeserializeObject<Column[]>(responseString);
                        //populated = true;

                        JObject entireWeatherReport = JObject.Parse(responseString);
                        IList<JToken> dailyReports = entireWeatherReport["forecast"]["txt_forecast"]["forecastday"].Children().ToList();

                        foreach (JToken report in dailyReports)
                        {
                            Forecast daysForecast = report.ToObject<Forecast>();
                            dailyForecasts.Add(daysForecast);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    var dayString = DateTime.Now.AddDays(daysOffset).DayOfWeek.ToString();

                    foreach(Forecast forecast in dailyForecasts)
                    {
                        if(String.Equals(forecast.title.ToLowerInvariant(), dayString.ToLowerInvariant())) {
                            toReturn = forecast;
                            if(overrideIcons)
                            {
                                String tmp = forecast.icon_url;
                                forecast.icon_url = tmp.Replace("/i/c/k/", "/i/c/i/");
                            }
                            break;
                        }
                    }
                }
            }

            return toReturn;
        }
    }
}
