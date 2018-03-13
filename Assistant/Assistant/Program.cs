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
            bool continueRunning = true;

            if(args.Length != 0)
            {
                var suppliedArgs = parseArguments(args);
                if(suppliedArgs.Contains("standup"))
                {
                    continueRunning = false;
                    if(suppliedArgs.Contains("tomorrow"))
                    {
                        // run tomorrow
                        generateStandup(1);
                    }
                    else
                    {
                        // run today
                        generateStandup();
                    }
                }
                else
                {
                    Console.WriteLine("Unrecognized command. Please type \"help\" for command line options");
                }
            }

            while (continueRunning)
            {
                Console.WriteLine("Enter a command or \"help\"");
                string input = Console.ReadLine();

                List<string> inputArgs = parseArguments(input);
                if(inputArgs.Contains("exit"))
                {
                    continueRunning = false;
                }
                else
                {
                    if (inputArgs.Contains("standup"))
                    {
                        continueRunning = false;
                        if (inputArgs.Contains("tomorrow"))
                        {
                            // run tomorrow
                            generateStandup(1);
                        }
                        else
                        {
                            // run today
                            generateStandup();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unrecognized command. Please type \"help\" for command line options");
                    }
                }
            }

            Console.WriteLine("Exiting...");
        }

        static List<string> parseArguments(string[] args)
        {
            List<string> argsToReturn = new List<string>();
            foreach(string arg in args)
            {
                argsToReturn.Add(arg.TrimStart('-').TrimEnd(' ').ToLower());
            }

            return argsToReturn;
        }
        static List<string> parseArguments(string args)
        {
            var splitArgs = args.Split(' ');

            return parseArguments(splitArgs);
        }

        static void generateStandup(int offset = 0)
        {
            KanbanFlow kb = new KanbanFlow();
            kb.SignIn();
            kb.populateBoard();
            kb.populateDueDates();

            CalendarServices cs = new CalendarServices();
            cs.SignIn();

            StandupReport report = new StandupReport(cs, kb);
            report.GenerateReport(offset);
        }
    }
}
