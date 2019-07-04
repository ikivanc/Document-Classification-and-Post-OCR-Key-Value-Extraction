using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace KeyValueExtraction.Model
{

    public class CustomVision
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Project")]
        public string Project { get; set; }

        [JsonProperty(PropertyName = "Iteration")]
        public string Iteration { get; set; }

        [JsonProperty(PropertyName = "Created")]
        public DateTime Created { get; set; }

        [JsonProperty(PropertyName = "Predictions")]
        public Prediction[] Predictions { get; set; }
    }

    public class Prediction
    {
        [JsonProperty(PropertyName = "TagId")]
        public string TagId { get; set; }

        [JsonProperty(PropertyName = "TagName")]
        public string TagName { get; set; }

        [JsonProperty(PropertyName = "Probability")]
        public float Probability { get; set; }
    }
}
