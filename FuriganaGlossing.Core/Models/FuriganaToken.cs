using System;

namespace FuriganaGlossing.Models
{
    public class FuriganaToken
    {
        public string OriginalText { get; set; } = string.Empty;
        public string Reading { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }
}
