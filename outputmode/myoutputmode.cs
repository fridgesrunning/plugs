using System;
using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using OpenTabletDriver.Plugin.DependencyInjection;      
using OpenTabletDriver.Plugin.Platform.Pointer; 
using System.Linq;
using System.Collections.Generic;

namespace myoutputmode;

[PluginName("myoutputmode")]
public class myoutputmode : AbsoluteOutputMode
{
    [Resolved]
    public override IAbsolutePointer? Pointer { set; get; }

    // Handle a report that just got parsed
    public override void Read(IDeviceReport deviceReport)
    {
        base.Read(deviceReport);
    }

    protected override IAbsolutePositionReport Transform(IAbsolutePositionReport report)
    {
        base.Transform(report);

        Console.WriteLine(mybinding.pressingbutton);
        return report;
    }

    // Handle a report that has gone through the pipeline
    protected override void OnOutput(IDeviceReport report)
    {
        
        base.OnOutput(report);
    }
}

[PluginName("mybinding")]
public class mybinding : IStateBinding
{
    [Property("Property"), PropertyValidated(nameof(ValidProperties))]
    public string Property { set; get; } = string.Empty;

    public void Press(TabletReference tablet, IDeviceReport report)
    {
        pressingbutton = true;
    }
    
    internal static bool pressingbutton { set; get; }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
        pressingbutton = false;
    }

    [Property("Numerical Input Box Property"),
        Unit("Some Unit Here"),
        DefaultPropertyValue(727),
        ToolTip("Filter template:\n\n" +
                "A property that appear as an input box.\n\n" +
                "Has a numerical value.")
    ]
    public int ExampleNumericalProperty { get; set; }

    [Property("String Type Input Box Property"),
        DefaultPropertyValue("727"),
        ToolTip("Filter template:\n\n" +
                "A property that appear as an input box.\n\n" +
                "Has a string value.")
    ]
    public string ExampleStringProperty { get; set; }

    [BooleanProperty("Boolean Property", ""),
        DefaultPropertyValue(true),
        ToolTip("Area Randomizer:\n\n" +
                "A property that appear as a check box.\n\n" +
                "Has a Boolean value")
    ]
    public bool ExampleBooleanProperty { set; get; }

    [Property("Validated Property"),
        DefaultPropertyValue("Two"),
        PropertyValidated(nameof(SomeChoice))
    ]
    public string SomeValidatedProperty { get; set; }

    public static IEnumerable<string> SomeChoice { get; set; } = new List<string> { "One", "Two", "Three" };
}


