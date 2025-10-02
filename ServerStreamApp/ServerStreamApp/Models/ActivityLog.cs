using System;

namespace ServerStreamApp.Models
{
    public enum ActivityType
    {
        LOGIN,
        AUTH_FAILED,
        CONNECT,
        DISCONNECT,
        SERVER_START,
        SERVER_STOP
    }

    public class ActivityLog
    {
        public int LogId { get; set; }
        public int? UserId { get; set; }
        public ActivityType ActivityType { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}