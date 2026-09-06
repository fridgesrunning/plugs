using System;
using System.Numerics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin20260717
{
    [PluginName("20260717")]
    public class Filter : IPositionedPipelineElement<IDeviceReport>
    {
        public Filter() : base()
        {
        }

        public class ConfigFile {
            public int setting1 { get; set; } = 1;
            public double setting2 { get; set; } = 2.0;
            public bool setting3 { get; set; } = false;
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport>? Emit;

        public void Consume(IDeviceReport value)
        {
            
            if (value is not ITabletReport && value is not IAuxReport) return;

                if (!init) {
                    init = true;
                    string exPath = Path.Combine(AppContext.BaseDirectory, "SaturnConfig", name + ".json");     // Guess this animal
                    var config = new ConfigFile();
                    if (File.Exists(exPath) && value is not IAuxReport auxReport) {
                        Console.WriteLine("yep");
                        try {
                            string json = File.ReadAllText(exPath);
                            var loaded = JsonSerializer.Deserialize<ConfigFile>(json);
                            
                            if (loaded != null) {
                                config = loaded;
                                Console.WriteLine("success");
                            }
                        }
                        catch (JsonException ex) {
                            Log.WriteNotify("oops", $"{ex}", LogLevel.Error);
                        }
                    }
                    else {
                        Console.WriteLine("nope");
                    }

                }
            
            Emit?.Invoke(value);
        }

        bool init;

        [TabletReference]
        public TabletReference TabletReference { set { name = value.Properties.Name; } }
        public string name = string.Empty;
    }
}