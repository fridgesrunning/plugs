using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin20260714
{
    [PluginName("20260714")]
    public class Filter : IPositionedPipelineElement<IDeviceReport>
    {
        public Filter() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public event Action<IDeviceReport>? Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                InsertAtFirst(pos, report.Position);
                InsertAtFirst(dir, pos[0] - pos[1]);
                InsertAtFirst(vel, dir[0].Length());
                InsertAtFirst(accel, vel[0] - vel[1]);

                float fac = 1f;

                if (vel[0] > pk) pk = vel[0];

                if (vel[0] < 5f) pk = 0;

                if ((pk > 100) && (vel[0] < pk * 0.5f) && (vel[0] > pk * 0.25f) && (accel[0] < -10f)) fac = 0.5f;

                if ((vel[0] < 30) && (accel[0] > 10f) && (accel[0] > 10f + accel[1])) wt = 0;

                wt = 0.75f * wt + 0.25f * fac;
                
                if (wt > 0.99f) wt = 1.0f;

                o = Vector2.Lerp(o, report.Position, wt);

                report.Position = o;
                lo = o;
            }
            Emit?.Invoke(value);
        }

        float wt = 1.0f;

        public static void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        Vector2[] pos = new Vector2[6];
        Vector2[] dir = new Vector2[6];
        float pk;
        float[] vel = new float[6];
        float[] accel = new float[6];
    }
}