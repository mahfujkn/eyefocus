using System;

namespace EyeFocus.Models
{
    public class DisplayScheduleItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0); // e.g. 08:00
        public string ProfileId { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int Order { get; set; } = 0;

        public DisplayScheduleItem Clone()
        {
            return new DisplayScheduleItem
            {
                Id = Guid.NewGuid().ToString(),
                StartTime = this.StartTime,
                ProfileId = this.ProfileId,
                IsEnabled = this.IsEnabled,
                Order = this.Order
            };
        }
    }
}
