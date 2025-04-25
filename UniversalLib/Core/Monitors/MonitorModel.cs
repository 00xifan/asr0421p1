using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace UniversalLib.Core.Monitors
{
    [JsonSerializable(typeof(MonitorModel))]
    internal class MonitorModel
    {
        [System.Text.Json.Serialization.JsonPropertyName("Monitor")]
        public List<MonitorItem> Monitor { get; set; }
    }

    internal class MonitorItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("DPI")]
        public int DPI { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("DeviceId")]
        public string DeviceId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("DeviceName")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 0 0 degrees 1 90 degrees 2 180 degrees 3 270 degrees; 0 and 180 are landscape screens, 90 and 270 are vertical screens
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("Orientation")]
        public int Orientation { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Position")]
        public string Position { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Resolution")]
        public string Resolution { get; set; }

    }
}
