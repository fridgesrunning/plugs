using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace AbsoluteDot
{
    [PluginName("AbsDot")]
    public class AbsDot : IPositionedPipelineElement<IDeviceReport>
    {
        public AbsDot() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PreTransform;

       public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                raw3Pos = raw2Pos;
                raw2Pos = raw1Pos;
                raw1Pos = report.Position;
                dir2 = dir1;
                dir1 = (raw1Pos - raw2Pos);
                lastVelocity = velocity;
                velocity = (float)Math.Sqrt(Math.Pow(raw1Pos.X - raw2Pos.X, 2) + Math.Pow(raw1Pos.Y - raw2Pos.Y, 2));
                scale = (float)Smootherstep(velocity / lastVelocity, 1f, 0f) * (float)(1 - Smootherstep(Math.Abs(Vector2.Dot(Vector2.Normalize(dir1), Vector2.Normalize(dir2))), 1f, 0));
                if ((lastVelocity != 0) & (velocity != 0))
                {
                dir1 = Vector2.Lerp(dir1, Vector2.Normalize(dir2) * (float)Math.Sqrt(Math.Pow(dir1.X, 2) + Math.Pow(dir1.Y, 2)), scale * 0.5f);
                calc1Pos = raw2Pos + dir1;
                report.Position = calc1Pos;
                }
            }
            Emit?.Invoke(value);
        }

        public static float Smootherstep(float x, float start, float end) // Copy pasted from osu! pp. Thanks StanR
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return (float)(x * x * x * (x * (6.0 * x - 15.0) + 10.0));
        }

        public static float ClampedLerp(float start, float end, float scale)
        {
            scale = (float)Math.Clamp(scale, 0, 1);

            return start + scale * (end - start);
        }

        public float velocity, lastVelocity, scale;
        public Vector2 raw3Pos, raw2Pos, raw1Pos, dir2, dir1, calc1Pos;
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}