using System.Collections.Generic;

namespace FuriganaGlossing.Models
{
    public class OcrResult
    {
        public List<OcrTextLine> Lines { get; set; } = new List<OcrTextLine>();
    }

    public class OcrTextLine
    {
        public string Text { get; set; }
        public Rect BoundingBox { get; set; }
    }

    public struct Rect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
