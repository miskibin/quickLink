using System;
using System.Collections.Generic;

namespace quickLink.Models
{
    /// <summary>
    /// Tracks usage statistics for items to improve search ranking
    /// </summary>
    public class ItemUsageStats
    {
        public Dictionary<string, UsageInfo> Items { get; set; } = new();
    }

    public class UsageInfo
    {
        public int UseCount { get; set; }
        public DateTime LastUsed { get; set; }
    }
}
