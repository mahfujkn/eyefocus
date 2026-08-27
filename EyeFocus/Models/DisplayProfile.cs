using System;

namespace EyeFocus.Models
{
    public class DisplayProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Custom";
        public int Kelvin { get; set; } = 5000;
        public int Brightness { get; set; } = 70; // 0 - 100%
        public int Red { get; set; } = 100; // 0 - 100%
        public int Green { get; set; } = 100; // 0 - 100%
        public int Blue { get; set; } = 100; // 0 - 100%
        public int SoftwareDim { get; set; } = 0; // 0 - 100%
        public int? Contrast { get; set; } = null; // Optional 0 - 100%
        public int TransitionDurationSeconds { get; set; } = 5;
        public string Description { get; set; } = string.Empty;
        public string IconKey { get; set; } = "IconComfort";
        public bool IsBuiltIn { get; set; } = false;
        public bool IsUserModified { get; set; } = false;

        public DisplayProfile Clone(string? newName = null)
        {
            return new DisplayProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = newName ?? $"{Name} (Copy)",
                Kelvin = this.Kelvin,
                Brightness = this.Brightness,
                Red = this.Red,
                Green = this.Green,
                Blue = this.Blue,
                SoftwareDim = this.SoftwareDim,
                Contrast = this.Contrast,
                TransitionDurationSeconds = this.TransitionDurationSeconds,
                Description = this.Description,
                IconKey = this.IconKey,
                IsBuiltIn = false,
                IsUserModified = false
            };
        }

        public void CopyFrom(DisplayProfile other)
        {
            this.Kelvin = other.Kelvin;
            this.Brightness = other.Brightness;
            this.Red = other.Red;
            this.Green = other.Green;
            this.Blue = other.Blue;
            this.SoftwareDim = other.SoftwareDim;
            this.Contrast = other.Contrast;
            this.TransitionDurationSeconds = other.TransitionDurationSeconds;
            this.Description = other.Description;
            if (!string.IsNullOrEmpty(other.IconKey))
            {
                this.IconKey = other.IconKey;
            }
            this.IsUserModified = true;
        }
    }
}
