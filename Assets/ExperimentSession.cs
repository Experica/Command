using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experica.Command
{
    /// <summary>
    /// Holds all information that define an experiment session
    /// </summary>
    public class ExperimentSession
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string Designer { get; set; } = "";
        public string Experimenter { get; set; } = "";
        public string Log { get; set; } = "";

        public string ExSessionLogicPath { get; set; } = "";
        public double ReadyWait { get; set; }
        public double StopWait { get; set; }
        public Dictionary<string, object> Param { get; set; } = new Dictionary<string, object>();

        public bool SendMail { get; set; } = false;
        public bool IsFullScreen { get; set; } = false;
        public bool IsFullViewport { get; set; } = false;
        public bool IsGuideOn { get; set; } = true;
    }
}