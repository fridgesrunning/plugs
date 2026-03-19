using System;
using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace PostTransformCenterMode
{
    [PluginName("Post Transform Center Mode (Standalone)")]
    public class Standalone : OutputModeAware
    {
        public Standalone() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

        [Property("Reset Time"), DefaultPropertyValue(25f), ToolTip
        (
            "This plugin only applies to absolute mode and if time > 0.\n" +
            "For this to work, you have to right click on the tablet part of absolute settings\n" +
            "and uncheck both Clamp/Ignore input outside of tablet area.\n" +
            "You probably want to use a small area with this."
        )]
        public float resetTime
        {
            set => _resetTime = Math.Max(value, 0f);
            get => _resetTime;
        }
        public float _resetTime;

        [BooleanProperty("Initialize At Center", ""), DefaultPropertyValue(true), ToolTip
        (
            "If disabled, output obeys settings at the first report.\n" +
            "If enabled, then it just takes over fully at initialization."
        )]
        public bool initCenter { set; get; }

        public override event Action<IDeviceReport>? Emit;

        public override void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {   
                HandleOutputMode(report.Position);
                if (!passthroughFlag) {
                    report.Position = pos[0];
                }
            }
            Emit?.Invoke(value);
        }

        void HandleOutputMode(Vector2 input) {
            float reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
            OutputMode outputMode = GetOutputMode();
            if (outputMode.Type == OutputType.absolute) {
                if (!initFlag) {
                    screenCenter = getDisplayArea() / 2;
                }
                if (resetTime == 0f) {
                    passthroughFlag = true;
                }
                else {
                    InsertAtFirst(dir, input - fRelPoint);
                    fRelPoint = input;
                    if (initFlag) {
                        if (reportTime < resetTime) Continue();
                        else Reset(); 
                    }
                    else {
                        initFlag = true;
                        if (!initCenter) Continue();
                        else Reset();
                    }
                }
            }
            else {
                passthroughFlag = true;
            } 
        }

        void Continue() {
            InsertAtFirst(pos, pos[0] + dir[0]);
        }

        void Reset() {
            ResetAction(pos, screenCenter);
        }

        void InsertAtFirst<T>(T[] arr, T element) {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        void ResetAction<T>(T[] arr, T element) {
            for (int p = arr.Length - 1; p > 1; p--) arr[p] = arr[p - 2];
            arr[0] = element;
            arr[1] = element;
        }

        const int HMAX = 4;

        Vector2[] pos = new Vector2[HMAX];
        Vector2[] dir = new Vector2[HMAX];

        bool initFlag, passthroughFlag;

        Vector2 screenCenter, fRelPoint;
        
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}