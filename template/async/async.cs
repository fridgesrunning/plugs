using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace async
{
    [PluginName("asyncfilter")]
    public class FilterPlugin : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public FilterPlugin() : base()
        {
        }

        public HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch(true);

        public override PipelinePosition Position => PipelinePosition.PreTransform;

        protected override void ConsumeState()
        {
            if ((State is ITabletReport report && PenIsInRange())) {
                position = report.Position;
            }
            else {
                OnEmit();
            }
        }

        protected override void UpdateState()
        {
            if ((State is ITabletReport report && PenIsInRange())) {
                report.Position = position;
            }
        }

        Vector2 position;
    }
}