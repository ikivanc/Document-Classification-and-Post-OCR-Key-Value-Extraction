using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CognitiveFunction.Model
{
    class RecognizeText
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "recognitionResult")]
        public Region RecognitionResult { get; set; }
    }

    public class Region
    {
        [JsonProperty(PropertyName = "lines")]
        public Line[] Lines { get; set; }
    }

    public class Line
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public int[] BoundingBox { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "words")]
        public Word[] Words { get; set; }
    }

    public class Word
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public int[] BoundingBox { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "Confidence")]
        public string Confidence { get; set; }
    }

}
