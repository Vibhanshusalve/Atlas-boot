using System;
using System.Collections.Generic;

namespace SystemAtlas.Core
{
    public class Manifest
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Complete";
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, string> CollectorStatuses { get; set; } = new Dictionary<string, string>();
    }
}
