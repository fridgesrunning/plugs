using System;
using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace PostTransformCenterMode;
public abstract class OutputModeAware : IPositionedPipelineElement<IDeviceReport>
{ 
    public OutputMode GetOutputMode() {
        TryResolveOutputMode();
        return outputMode;
    }

    public Vector2 getDisplayArea() {
        if (outputMode.Type == OutputType.absolute && absoluteOutputMode != null) {
            return new Vector2(absoluteOutputMode.Output.Width, absoluteOutputMode.Output.Height);
        }

        TryResolveOutputMode();
        return default;
    }

    [Resolved]
    public IDriver? driver;
    private OutputMode outputMode;
    private AbsoluteOutputMode? absoluteOutputMode;
    private RelativeOutputMode? relativeOutputMode;
    private void TryResolveOutputMode()
    {
        if (driver is Driver drv)
        {
            IOutputMode? output = drv.InputDevices
                .Where(dev => dev?.OutputMode?.Elements?.Contains(this) ?? false)
                .Select(dev => dev?.OutputMode).FirstOrDefault();

            if (output is AbsoluteOutputMode absOutput) {
                absoluteOutputMode = absOutput;
                outputMode.Type = OutputType.absolute;
                return;
            }
            if (output is RelativeOutputMode relOutput) {
                relativeOutputMode = relOutput;
                outputMode.Type = OutputType.relative;
                return;
            }
            outputMode.Type = OutputType.unknown;
        }
    }

    public abstract event Action<IDeviceReport> Emit;
    public abstract void Consume(IDeviceReport value);
    public abstract PipelinePosition Position { get; }
}

public enum OutputType {
    absolute,
    relative,
    unknown
}

public struct OutputMode {
    public OutputType Type;
}