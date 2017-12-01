using System;

namespace KanbanFlowClient.Classes
{
    public class Board
    {
        public string _id { get; set; }
        public string name { get; set; }
        public Column[] columns { get; set; }
        public Color[] colors { get; set; }
    }
    public class Column
    {
        public string columnId { get; set; } // UniqueId if Getting Board
        public string columnName { get; set; } // Name if Getting Board
        public bool tasksLimited { get; set; }
        public string swimlaneId { get; set; }
        public string swimlaneName { get; set; }
        public Task[] tasks { get; set; }
    }
    public class Task
    {
        public string _id { get; set; }
        public string name { get; set; }
        public string columnId { get; set; }
        public string swimlaneId { get; set; }
        public int position { get; set; }
        public string description { get; set; }
        public string color { get; set; }
        public string responsibleUserId { get; set; }
        public int totalSecondsEstimate { get; set; }
        public int totalSecondsSpent { get; set; }
        public string groupingDate { get; set; }
        public SubTask[] subTasks { get; set; }
        public Label[] labels { get; set; }
        public DateTime? dueDate { get; set; }

    }
    public class Color
    {
        public string name { get; set; }
        public string description { get; set; }
        public string value { get; set; }
    }
    public class SubTask
    {
        public string name { get; set; }
        public bool finished { get; set; }

    }
    public class Label
    {
        public string name { get; set; }
        public bool pinned { get; set; }
    }
    public class Date
    {
        public string status { get; set; }
        public string dateType { get; set; }
        public DateTime dueTimestamp { get; set; }
        public DateTime dueTimestampLocal { get; set; }
        public string targetColumnId { get; set; }
    }
}
