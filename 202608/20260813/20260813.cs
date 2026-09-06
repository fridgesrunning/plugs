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

namespace Plugin20260813
{
    [PluginName("20260813")]
    public class Filter : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public Filter() : base()
        {
        }

        public HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch(true);

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        [Property("a"), DefaultPropertyValue(1.0f), ToolTip
        (
            "a"
        )]
        public float weight
        {
            set => _weight = Math.Clamp(value, 0.001f, 1.0f);
            get => _weight;
        }
        public float _weight;

        protected override void ConsumeState()
        {
            if ((State is ITabletReport report && PenIsInRange())) {
                rPos = report.Position;
                Console.WriteLine("---------------");
                upd = 0f;
            }
            else {
                OnEmit();
            }
        }

        protected override void UpdateState()
        {
            if ((State is ITabletReport report && PenIsInRange())) {
                upd += 1f;
                ls = oPos;
                float x = MathF.Pow(weight, (1.0f / upd));
                oPos = (1.0f - x) * oPos + (x) * rPos;
                report.Position = oPos;
                Console.WriteLine(Vector2.Distance(oPos, rPos));
                Console.WriteLine(Vector2.Distance(oPos, ls));
                Console.WriteLine("--");
                OnEmit();
            }
        }

        Vector2 rPos;
        Vector2 oPos, ls;
        float upd;
    }
}