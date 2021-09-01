﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace LocalAgent.Models
{
    public class Pipeline
    {
        [JsonProperty("trigger")]
        public IList<string> Trigger { get; set; }

        [JsonProperty("variables")]
        public IList<IVariableExpectation> Variables { get; set; }

        [JsonProperty("stages")]
        public IList<IStageExpectation> Stages { get; set; }

        [JsonProperty("jobs")]
        public IList<IJobExpectation> Jobs { get; set; }
    }
}