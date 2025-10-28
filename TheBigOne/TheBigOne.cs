using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace x
{
    [PluginName("The Big One (Pixel Dimensions)")]
    public class TheBigOne : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public TheBigOne() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

                [Property("X Divisor (Important) (Hover Over The Textbox)"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Accounts for the discrepancy between tablet aspect ratio and screen aspect ratio?\n" +
            "Default value is 1.0\n" +
            "If you are reading this, you should contact me. discord: shreddism"
        )]
        public double xDivisor
        {
            get { return xDiv; }
            set { xDiv = System.Math.Clamp(value, 0.01f, 100.0f); }
        }
        private double xDiv;

        [Property("Outer Radius (Important)"), DefaultPropertyValue(500.0d), Unit("px"), ToolTip
        (
            "This scales with cursor velocity by default. \n\n" +
            "Outer radius defines the max distance the cursor can lag behind the actual reading.\n\n" +
            "Unit of measurement is pixels.\n" +
            "The value should be >= 0 and inner radius.\n" +
            "If smoothing leak is used, defines the point at which smoothing will be reduced,\n" +
            "instead of hard clamping the max distance between the tablet position and a cursor.\n\n" +
            "That was the original description. This should be equal to inner radius in most cases.\n\n" +
            "Default value is 500 px"
        )]
        public double OuterRadius
        {
            get { return rOuter; }
            set { rOuter = System.Math.Clamp(value, 0.0f, 1000000.0f); }
        }
        private double rOuter = 0;  // I have no idea why this is like this but it still works.

        [Property("Inner Radius (Important)"), DefaultPropertyValue(500.0d), Unit("px"), ToolTip
        (
            "This scales with cursor velocity by default. \n\n" +
            "Inner radius defines the max distance the tablet reading can deviate from the cursor without moving it.\n" +
            "This effectively creates a deadzone in which no movement is produced.\n\n" +
            "Unit of measurement is pixels.\n" +
            "The value should be >= 0 and <= outer radius.\n\n" +
            "That was the original description. This should be equal to outer radius in most cases.\n\n" +
            "Default value is 500 px"
        )]
       public double InnerRadius
        {
            get { return rInner; }
            set { rInner = System.Math.Clamp(value, 0.0f, 1000000.0f); }
        }
        private double rInner = 0;  // I have no idea why this is like this but it still works.

        [Property("Initial Smoothing Coefficient"), DefaultPropertyValue(0d), ToolTip
        (
            "Smoothing coefficient determines how fast or slow the cursor will descend from the outer radius to the inner.\n\n" +
            "Possible value range is 0.0001..1, higher values mean more smoothing (slower descent to the inner radius).\n\n" +
            "Default value is 0"
        )]
       public double SmoothingCoefficient
        {
            get { return smoothCoef; }
            set { smoothCoef = System.Math.Clamp(value, 0.0001f, 1.0f); }
        }
        private double smoothCoef;

        [Property("Soft Knee Scale"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Soft knee scale determines how soft the transition between smoothing inside and outside the outer radius is.\n\n" +
            "Possible value range is 0..100, higher values mean softer transition.\n" +
            "The effect is somewhat logarithmic, i.e. most of the change happens closer to zero.\n\n" +
            "Default value is 1"
        )]
        public double SoftKneeScale
        {
            get { return knScale; }
            set { knScale = System.Math.Clamp(value, 0.0f, 100.0f); }
        }
        private double knScale;

        [Property("Smoothing Leak Coefficient"), DefaultPropertyValue(0.0d), ToolTip
        (
            "Smoothing leak coefficient allows for input smoothing to continue past outer radius at a reduced rate.\n\n" +
            "Possible value range is 0..1, 0 means no smoothing past outer radius, 1 means 100% of the smoothing gets through.\n\n" +
            "Note that this probably shouldn't be above 0.\n\n" +
            "Default value is 0.0"
        )]
        public double SmoothingLeakCoefficient
        {
            get { return leakCoef; }
            set { leakCoef = System.Math.Clamp(value, 0.01f, 1.0f); }
        }
        private double leakCoef;

        [Property("Velocity Divisor (Important)"), DefaultPropertyValue(20.0d), ToolTip
        (
            "Radius will be multiplied by the cursor's velocity divided by this number up to 1 * the radius value.\n\n" +
            "Unit of measurement is pixels per report?" +
            "Default value is 20.0"
        )]
       public double VelocityDivisor
        {
            get { return vDiv; }
            set { vDiv = System.Math.Clamp(value, 0.01f, 1000000.0f); }
        }
        private double vDiv;

        [Property("Minimum Radius Multiplier"), DefaultPropertyValue(0.0d), ToolTip
        (
            "Set this to a low number that multiplies your radii to like 1px."
        )]
        public double MinimumRadiusMultiplier
        {
            get { return minMult; }
            set { minMult = System.Math.Clamp(value, 0.0f, 1.0f); }
        }
        private double minMult;

        [Property("Radial Mult Power"), DefaultPropertyValue(25.0d), ToolTip
        (
            "Velocity / the velocity divisor returns a radial multiplier, which is raised to this power.\n\n" +
            "Possible value range is 1 and up, 1 means radius will scale linearly with velocity up to 1 * radius, 2 means it will be squared, 3 means it will be cubed, and so on.\n" +
            "Numbers that low are just for explanation, I recommend going higher.\n" + 
            "Default value is 25.0"
        )]
        public double RadialMultPower
        {
            get { return radPower; }
            set { radPower = System.Math.Clamp(value, 1.0f, 1000000.0f); }
        }
        private double radPower;

        [Property("Minimum Smoothing Divisor"), DefaultPropertyValue(25.0d), ToolTip
        (
            "As velocity along with an acceleration factor becomes lower than max radius threshold,\n" +
            "initial smoothing coefficient approaches being divided by this number * some constant. It might be slightly more complex than that but you don't have to worry about it.\n\n" +
            "Possible value range is 2 and up.\n\n" +
            "Default value is 25.0"
        )]
       public double MinimumSmoothingDivisor
        {
            get { return minSmooth; }
            set { minSmooth = System.Math.Clamp(value, 2.0f, 1000000.0f); }
        }
        public double minSmooth;

        [Property("Raw Accel Threshold"), DefaultPropertyValue(-1.5d), ToolTip
        (
            "If decel (negative value) is sharp enough, then cursor starts to approach snapping to raw report. Velocity divisor adjusts for this.\n" +
            "You can put this above 0 if you feel like it, but be aware that this overrides most other processing.\n" + 
            "Look in the console for the Sharp Decel Lerp value (read the option below) if you want to do that.\n\n" +
            "Default value is -1.5"
        )]
       public double RawAccelThreshold
        {
            get { return rawThreshold; }
            set { rawThreshold = System.Math.Clamp(value, -1000000.0f, 1000.0f); }
        }
        public double rawThreshold;

        [BooleanProperty("Console Logging", ""), DefaultPropertyValue(false), ToolTip
        (
            "Each report, info will be printed in console.\n\n" +
            "If the rate of prints exceeds report rate, then that is bad and this filter is not working. Screenshot your area, reset all settings, then re-enable this filter first.\n" +
            "You can use this to make sure that your parameters and thresholds are right.\n\n" +
            "Default value is false"
        )]
        public bool ConsoleLogging
        {
            get { return cLog; }
            set { cLog = value; }
        }
        private bool cLog;

        [Property("Accel Mult Power"), DefaultPropertyValue(9.0d), ToolTip
        (
            "Enable Console Logging above and look at the console. This specific setting affects only radius scaling. but is pretty important.\n\n" +
            "Default value is 9.0"
        )]
        public double AccelMultPower
        {
            get { return accPower; }
            set { accPower = System.Math.Clamp(value, 1.0f, 100.0f); }
        }
        public double accPower;

        [BooleanProperty("2.0 behavior (below options)", ""), DefaultPropertyValue(true), ToolTip
        (
            "Enables the options below, and some other behavioral changes.\n\n" +
            "Default value is true"
        )]
        public bool Advanced
        {
            get { return aToggle; }
            set { aToggle = value; }
        }
        public bool aToggle;

        [Property("Raw Velocity Threshold (Important)"), DefaultPropertyValue(7.5d), ToolTip
        (
            "Regardless of acceleration, being above this velocity for 2 consecutive reports will override and max out radius.\n" +
            "Only active if 2.0 behavior is enabled.\n\n" +
            "Default value is 7.5"
        )]
        public double rvt
        {
            get { return rawv; }
            set { rawv =  System.Math.Clamp(value, 0.01f, 1000000.0f); }
        }
        public double rawv;

        [Property("Angle Index Confidence"), DefaultPropertyValue(4.5d), ToolTip
        (
            "Controls angle index confidence. Higher is weaker. Gets buggy around 1 and below. Usually best to leave this alone.\n" +
            "Only active if 2.0 behavior is enabled.\n\n" +
            "Default value is 4.5"
        )]
        public double aidx
        {
            get { return angidx; }
            set { angidx = System.Math.Clamp(value, 0.1f, 1000000.0f); }
        }
        public double angidx;

        [Property("Angle Index Decel Confidence"), DefaultPropertyValue(9.0d), ToolTip
        (
            "No idea how to describe this well. It's similar to above but acts in the same vein as raw accel threshold. Usually best to leave this alone.\n" +
            "Only active if 2.0 behavior is enabled.\n\n" +
            "Default value is 9.0"
        )]
        public double xlerpconf
        {
            get { return explerpconf; }
            set { explerpconf = System.Math.Clamp(value, 0.1f, 1000000.0f); }
        }
        public double explerpconf;

        [Property("Accel Mult Velocity Override (Important)"), DefaultPropertyValue(10.0d), ToolTip
        (
            "Velocity divisor plays a role in accel mult calculation. This is a manual override. Nothing changes if this is the same as the velocity divisor\n" +
            "Only active if 2.0 behavior is enabled.\n\n" +
            "Default value is 10.0"
        )]
        public double accelMultVelocityOverride
        {
            get { return amvDiv; }
            set { amvDiv = vDiv;

                // amvDiv = vDiv unless specified to an override
            if (aToggle == true)
            amvDiv = System.Math.Clamp(value, 0.1f, 1000000.0f);  }
        }
        public double amvDiv;

        [Property("Spin Check Confidence"), DefaultPropertyValue(1d), ToolTip
        (
            "Checks if raw velocity has been above ((this number) * raw velocity threshold) enough with no snaps.\n" +
            "Only active if 2.0 behavior is enabled.\n\n" +
            "Default value is 1"
        )]
        public double spinCheckConfidence
        {
            get { return scConf; }
            set { scConf = System.Math.Clamp(value, 0.1f, 10000.0f); }
        }
        public double scConf;

        [BooleanProperty("Grounded Radius (Default Behavior)", ""), DefaultPropertyValue(true), ToolTip
        (
            "Radius behavior where the position of the first radius max in a series of consecutive radius max reports dictates the center of the radius.\n" +
            "Only active if 2.0 behavior is enabled.\n\n" +
            "Default value is true"
        )]
        public bool groundedBehavior
        {
            get { return rToggle; }
            set { rToggle = value; }
        }
        public bool rToggle;

        
        [Property("extra number 1"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff1
        {
            get { return xn1; }
            set { xn1 = value; }
        }
        public double xn1;

        [Property("extra number 2"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff2
        {
            get { return xn2; }
            set { xn2 = value; }
        }
        public double xn2;

        [Property("extra number 3"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff3
        {
            get { return xn3; }
            set { xn3 = value; }
        }
        public double xn3;
        
        [Property("extra number 4"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff4
        {
            get { return xn4; }
            set { xn4 = value; }
        }
        public double xn4;
        
        [Property("extra number 5"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff5
        {
            get { return xn5; }
            set { xn5 = value; }
        }
        public double xn5;
        
        [Property("extra number 6"), DefaultPropertyValue(1.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff6
        {
            get { return xn6; }
            set { xn6 = value; }
        }
        public double xn6;
        
        [Property("extra number 7"), DefaultPropertyValue(-2d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff7
        {
            get { return xn7; }
            set { xn7 = value; }
        }
        public double xn7;
        
        [Property("extra number 8"), DefaultPropertyValue(1d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff8
        {
            get { return xn8; }
            set { xn8 = value; }
        }
        public double xn8;
        
        [Property("extra number 9"), DefaultPropertyValue(-4d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuff9
        {
            get { return xn9; }
            set { xn9 = value; }
        }
        public double xn9;
        
        [Property("extra number a"), DefaultPropertyValue(1d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuffa
        {
            get { return xna; }
            set { xna = value; }
        }
        public double xna;
        
        [Property("Report MS"), DefaultPropertyValue(7.5d), ToolTip
        (
            "Report MS"
        )]
        public double extrastuffb
        {
            get { return xnb; }
            set { xnb = value; }
        }
        public double xnb;
        
        [Property("extra number c"), DefaultPropertyValue(0.35d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuffc
        {
            get { return xnc; }
            set { xnc = value; }
        }
        public double xnc;
        
        [Property("extra number d"), DefaultPropertyValue(0.25d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuffd
        {
            get { return xnd; }
            set { xnd = value; }
        }
        public double xnd;

        [Property("Accel Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float accelWeight { get; set; }

        [Property("Normal Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float normalWeight { get; set; }

        [Property("Decel Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float decelWeight { get; set; }

        [Property("Accel Weight Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float a2ccelWeight { get; set; }

        [Property("Normal Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float n2ormalWeight { get; set; }

        [Property("Decel Weight"), DefaultPropertyValue(1f), ToolTip
        (
            "Filter template:\n\n" +
            "A property that appear as an input box.\n\n" +
            "Has a numerical value."
        )]
        public float d2ecelWeight { get; set; }

        [Property("extra number e"), DefaultPropertyValue(-1d), ToolTip
        (
            "Read the source code."
        )]
        public float extrastuffe
        {
            get { return xne; }
            set { xne = value; }
        }
        public float xne;

        [Property("extra number f"), DefaultPropertyValue(5.0d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastufff
        {
            get { return xnf; }
            set { xnf = value; }
        }
        public double xnf;

        [Property("extra number g"), DefaultPropertyValue(2d), ToolTip
        (
            "Read the source code."
        )]
        public double extrastuffg
        {   
            get { return xng; }
            set { xng = value; }
        }
        public double xng;

        [BooleanProperty("Extra Toggle 1", ""), DefaultPropertyValue(true), ToolTip
        (
            "String"
        )]
        public bool extratoggle1
        {
            get { return xt1; }
            set { xt1 = value; }
        }
        public bool xt1;

        protected override void ConsumeState()  // Report
        {
            if (State is ITabletReport report)
            {
                var consumeDelta = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (consumeDelta < 150)
                    reportMsAvg += ((consumeDelta - reportMsAvg) * 0.1f);
                    
                lastPosition = currPosition;
                currPosition = vec2IsFinite(currPosition) ? currPosition : report.Position;
                currPosition = Filter(State, report.Position);

                lastVelocity = velocity;
                velocity = (float)Math.Sqrt(Math.Pow(currPosition.X - lastPosition.X, 2) + Math.Pow(currPosition.Y - lastPosition.Y, 2));
                accel = velocity - lastVelocity;

                //if (!(velocity == 0))
               // pow = (float)Math.Clamp(1 + (arf_accel / arf_holdVel), 0, 1);
               // else pow = 1;

                if (accel < 0)
                emaWeight = ClampedLerp(decelWeight, normalWeight, Smootherstep((float)arf_accel / (float)arf_holdVel, xne, 0));
                else emaWeight = ClampedLerp(normalWeight, accelWeight, Smootherstep((float)arf_accel / (float)Math.Log(Math.Pow((float)arf_holdVel / xnf + 1, xnf) + 1), 0, 1));

                calc1Pos = vec2IsFinite(calc1Pos) ? calc1Pos : currPosition;

                UpdateState();

                //Console.WriteLine(currPosition);
            }
            else OnEmit();
        }

        protected override void UpdateState()   // Interpolation
        {
            float alpha = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);

            if (State is ITabletReport report && PenIsInRange())
            {
                alpha = (float)Math.Clamp(alpha, 0, 1);
                calc2Pos = Vector2.Lerp(lastPosition, currPosition, alpha);
                calc1Pos += emaWeight * (calc2Pos - calc1Pos);
                calc1Pos = vec2IsFinite(calc1Pos) ? calc1Pos : calc2Pos;
                if (velocity != 0)
                report.Position = calc1Pos;
                else {
                    calc2Pos = currPosition;
                    calc1Pos = currPosition;
                     report.Position = currPosition;
                }
                State = report;
                OnEmit();
            }
        }

        [TabletReference]
        public TabletReference TabletReference
        {
            set
            {
                var digitizer = value.Properties.Specifications.Digitizer;
                mmScale = new Vector2
                {
                    X = digitizer.Width / digitizer.MaxX,
                    Y = digitizer.Height / digitizer.MaxY
                };
            }
        }
        private Vector2 mmScale = Vector2.One;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
        Vector2 currPosition, lastPosition;
        float velocity, lastVelocity, accel;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = (1 / 300);

        public float arf_SampleRadialCurve(IDeviceReport value, float dist) => (float)arf_deltaFn(value, dist, arf_xOffset(value), arf_scaleComp(value));
        public double arf_ResetMs = 1;
        public double arf_GridScale = 1;

        public Vector2 Filter(IDeviceReport value, Vector2 target)
        {
                // Timing system from BezierInterpolator to standardize velocity
            double holdTime = stopwatch.Restart().TotalMilliseconds;
                var consumeDelta = holdTime;
                if (consumeDelta < 150)
                    arf_reportMsAvg += ((consumeDelta - arf_reportMsAvg) * 0.1f);

                // Produce numbers (velocity, accel, etc)
            UpdateReports(value, target);


                // Self explanatory
            if (aToggle == true)
            {
                AdvancedBehavior();
                if (rToggle == true)
                {
                    GroundedRadius(value, target); // Grounded radius behavior
                }
            }
            arf_holdCursor = arf_cursor;    // Don't remember why this is a thing

            Vector2 direction = target - arf_cursor;
            float distToMove = arf_SampleRadialCurve(value, direction.Length());    // Where all the magic happens

            LerpChecks();

                        
            direction = Vector2.Normalize(direction);
            arf_cursor = arf_cursor + Vector2.Multiply(direction, distToMove);
            arf_cursor = arf_LerpedCursor((float)arf_lerpScale, arf_cursor, target);    // Jump to raw report if certain conditions are fulfilled

                // Catch NaNs and pen redetection
            if (!(float.IsFinite(arf_cursor.X) & float.IsFinite(arf_cursor.Y) & holdTime < 50))
                arf_cursor = target;


            if (cLog == true)
            {
                Console.WriteLine("Start of report ----------------------------------------------------");
                Console.WriteLine("Raw Velocity:");
                Console.WriteLine(arf_holdVel);
                Console.WriteLine("Raw Acceleration:");
                Console.WriteLine(arf_accel);
                Console.WriteLine("Accel Mult (this is an additional factor that multiplies velocity, should be close to or 0 on sharp decel, hovering around 1 when neutral, and close to or 2 on sharp accel. Only affected by power on radius scaling, so not shown.):");
                Console.WriteLine(arf_accelMult);
                Console.WriteLine("Outside Radius:");
                Console.WriteLine(arf_rOuterAdjusted(value, arf_cursor, rOuter, rInner));
                Console.WriteLine("Inner Radius:");
                Console.WriteLine(arf_rInnerAdjusted(value, arf_cursor, rInner));
                Console.WriteLine("Smoothing Coefficient:");
                Console.WriteLine(smoothCoef / (1 + (arf_Smoothstep(arf_vel * arf_accelMult, vDiv, 0) * (minSmooth - 1))));
                Console.WriteLine("Sharp Decel Lerp (With sharp decel, cursor is lerped between calculated value and raw report using this scale):");
                Console.WriteLine(arf_lerpScale);

                if (aToggle == true)
                {
                    Console.WriteLine("A bunch of random numbers...");
                    Console.WriteLine(arf_jerk);
                    Console.WriteLine(arf_snap);
                    Console.WriteLine(arf_indexFactor);
                    Console.WriteLine((arf_indexFactor - arf_lastIndexFactor) / arf_holdVel);
                    Console.WriteLine(arf_spinCheck);
                    Console.WriteLine(arf_sinceSnap);
                }
    
                Console.WriteLine("End of report ----------------------------------------------------");
            }

            arf_lerpScale = 0;  // Reset value
            arf_lastCursor = arf_holdCursor;    // Don't remember why this is a thing

                // Reset possibly changed values
            if (aToggle == true)
            AdvancedReset();

            return arf_cursor;
        }


            // Stats from reports
        void UpdateReports(IDeviceReport value, Vector2 target)
        {
            if (value is ITabletReport report)
            {

                arf_last3Report = arf_lastLastReport;
                arf_lastLastReport = arf_lastReport;
                arf_lastReport = arf_currReport;
                arf_currReport = report.Position;

                arf_diff = arf_currReport - arf_lastReport;
                arf_seconddiff = arf_lastReport - arf_lastLastReport;
                arf_thirddiff = arf_lastLastReport - arf_last3Report;

                arf_lastVel = arf_vel;
                arf_vel =  ((Math.Sqrt(Math.Pow(arf_diff.X / xDiv, 2) + Math.Pow(arf_diff.Y, 2)) / 1) / xnb);
                arf_holdVel = arf_vel;

                arf_lastAccel = arf_accel;
                arf_accel = arf_vel - ((Math.Sqrt(Math.Pow(arf_seconddiff.X / xDiv, 2) + Math.Pow(arf_seconddiff.Y, 2)) / 1) / xnb);

                    // Has less use than it probably should.
                arf_lastJerk = arf_jerk;
                arf_jerk = arf_accel - arf_lastAccel;

                arf_snap = arf_jerk - arf_lastJerk;

                    // Angle index doesn't even use angles directly.
                arf_angleIndexPoint = 2 * arf_diff - arf_seconddiff - arf_thirddiff;
                arf_lastIndexFactor = arf_indexFactor;
                arf_indexFactor = (Math.Sqrt(Math.Pow(arf_angleIndexPoint.X / xDiv, 2) + Math.Pow(arf_angleIndexPoint.Y, 2)) / 1) / xnb;

                if (xt1)
                arf_accelMult = arf_Smoothstep(arf_accel, -1 / (6 / amvDiv), 0) + arf_Smoothstep(arf_accel / (Math.Log(Math.Pow((float)arf_lastVel / xng + 1, xng)) + 1), 0, 1 / (6 / amvDiv));
                else
                arf_accelMult = arf_Smoothstep(arf_accel, -1 / (6 / amvDiv), 0) + arf_Smoothstep(arf_accel, 0, 1 / (6 / amvDiv));   // Usually 1, reaches 0 and 2 under sufficient deceleration and acceleration respecctively
                
            /// You can uncomment for advanced diagnostics.
            //    Console.WriteLine(vel);
            //    Console.WriteLine(accel);
            //    Console.WriteLine(jerk);
            //    Console.WriteLine(snap);
            //    Console.WriteLine("-----------");
            //    Console.WriteLine(angleIndex);
            //    Console.WriteLine(angleIndex - lastIndex);
            //    Console.WriteLine(rOuterAdjusted(value, cursor, rOuter, rInner));
            //    Console.WriteLine("-------------------------------------------");
            }
        }

            // 2.0 behavior.
        void AdvancedBehavior()
        {

            arf_sinceSnap += 1;
            arf_doubt = 0;
            if ((Math.Abs(arf_indexFactor) > arf_vel * 2 | (arf_accel / arf_vel > xnc)) && (arf_vel / rawv > xnd))
            {
            //    Console.WriteLine("snapping?");
            //    Console.WriteLine(accel / vel);
                arf_sinceSnap = 0;
            }

            arf_last9Vel = arf_last8Vel;

            arf_last8Vel = arf_last7Vel;

            arf_last7Vel = arf_last6Vel;

            arf_last6Vel = arf_last5Vel;

            arf_last5Vel = arf_last4Vel;

            arf_last4Vel = arf_last3Vel;

            arf_last3Vel = arf_last2Vel;

            arf_last2Vel = arf_lastVel;

            arf_spinCheck = Math.Clamp(Math.Pow(arf_vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_lastVel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last2Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last3Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last4Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last5Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last6Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last7Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last8Vel / (rawv * scConf), 5), 0, 1) +
                        Math.Clamp(Math.Pow(arf_last9Vel / (rawv * scConf), 5), 0, 1);

        //    if (indexFactor > Math.Max(1 / (6 / rawv), angidx * vel))
        //    {
        //        if (!((vel > rawv & lastVel > rawv) || 
        //        (accel > (1 / (6 / rawv)) & jerk > (1 / (6 / rawv)) & snap > (1 / (6 / rawv)))))
        //        {
        //        Console.WriteLine("OH MY GOD BRUH");
        //        Console.WriteLine(vel);
        //        }
        //    }

            if ( // (vel > rawv & lastVel > rawv) || 
                (arf_accel > (xn3 / (6 / rawv)) & arf_jerk > (xn4 / (6 / rawv)) & arf_snap > (xn5 / (6 / rawv))) ||
                (arf_indexFactor > Math.Max(xn6 / (6 / rawv), angidx * arf_vel)))
            {
                arf_vel *= 10 * vDiv;
                arf_accelMult = 2;
                arf_doubt = 1;
            }

            arf_holdVel2 = arf_vel;

            if ((arf_distanceGround < rOuter) & (arf_vel > rawv & arf_lastVel > rawv))
            {
                arf_vel *= 10 * vDiv;
                arf_accelMult = 2;
            }

            if ((arf_spinCheck > 8) && arf_sinceSnap > 30)
            {
            //    Console.WriteLine("spinning?");
                arf_vel = 0;
                arf_accel = -10 * rawThreshold;
            }


        }

            // Grounded radius behavior
        void GroundedRadius(IDeviceReport value, Vector2 target)
        {
                // Not radius max
            if (arf_holdVel2 * Math.Pow(arf_accelMult, accPower) < vDiv)
            {
                arf_radiusGroundCount = 0;
                arf_distanceGround = 0;
              //  Console.WriteLine(holdVel2);
            }
                else arf_radiusGroundCount += 1;

            if (arf_accelMult < 1.99)
            {
                arf_sinceAccelTop = 0;
            }
            else arf_sinceAccelTop += 1;

                // Radius max
            if (arf_holdVel2 * Math.Pow(arf_accelMult, accPower) >= vDiv)
            {
                if ((arf_radiusGroundCount <= 1) || 
                
                (arf_vel > rawv & arf_lastVel > rawv) && 
                ((arf_accel > (xn3 / (6 / rawv)) & arf_jerk > (xn4 / (6 / rawv)) & arf_snap > (xn5 / (6 / rawv))) ||
                (arf_indexFactor > Math.Max(xn6 / (6 / rawv), angidx * arf_vel)) ||
                (arf_sinceAccelTop > 0)))
                {
                    arf_groundedPoint = arf_cursor;
                }
                    arf_groundedDiff = target - arf_groundedPoint;
                    arf_distanceGround = Math.Sqrt(Math.Pow(arf_groundedDiff.X, 2) + Math.Pow(arf_groundedDiff.Y, 2));
                    
            }
               //   Console.WriteLine(radiusGroundCount);
               //   Console.WriteLine(distanceGround);
                  //  Console.WriteLine(holdVel2 * Math.Pow(accelMult, accPower));


                // Cursor is outside max outer radius while radius is usually maxed? Act as if radius doesn't exist for smooth movement. Also exopts bs
            if (arf_distanceGround > rOuter)
            {
                arf_vel = 0;
                arf_accel = -10 * rawThreshold;
            }

        }

        void LerpChecks()
        {
                // rawThreshold should be negative (or not.) Sets lerpScale to a smootherstep from accel = rawThreshold to accel = something lower
            if (arf_accel / (6 / vDiv) < rawThreshold)
            arf_lerpScale = arf_Smootherstep(arf_accel / (6 / vDiv), rawThreshold, rawThreshold - (xn1 / (6 / vDiv)));

            if ((aToggle == true) && (arf_indexFactor - arf_lastIndexFactor > (arf_holdVel * explerpconf)))
            arf_lerpScale = Math.Max(arf_lerpScale, arf_Smootherstep(arf_indexFactor - arf_lastIndexFactor, (arf_holdVel * explerpconf), (arf_holdVel * explerpconf) + (xn2 / (6 / rawv))));  // Don't exactly remember why this is the way it is but it looks like it works

            if (arf_doubt == 0)
            {

            if (arf_jerk < xn7)
            arf_lerpScale = Math.Max(arf_lerpScale, arf_Smootherstep(arf_jerk, xn7, xn7 - xn8));

            if (arf_snap < xn9)
            arf_lerpScale = Math.Max(arf_lerpScale, arf_Smootherstep(arf_snap, xn9, xn9 - xna));

            }
        }

        void AdvancedReset()
        {
            arf_vel =  ((Math.Sqrt(Math.Pow(arf_diff.X / xDiv, 2) + Math.Pow(arf_diff.Y, 2)) / 1) / xnb);
            arf_accel = arf_vel - ((Math.Sqrt(Math.Pow(arf_seconddiff.X / xDiv, 2) + Math.Pow(arf_seconddiff.Y, 2)) / 1) / xnb);   // This serves no use but might later on. (Now does)
            arf_accelMult = arf_Smoothstep(arf_accel, -1 / (6 / amvDiv), 0) + arf_Smoothstep(arf_accel, 0, 1 / (6 / amvDiv));
        }




        /// Math functions
        
        double arf_kneeFunc(double x) => x switch
        {
            < -3 => x,
            < 3 => Math.Log(Math.Tanh(Math.Exp(x)), Math.E),
            _ => 0,
        };

        public static double arf_Smoothstep(double x, double start, double end) // Copy pasted out of osu! pp. Thanks StanR 
        {
            x = Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return x * x * (3.0 - 2.0 * x);
        }

        public static double arf_Smootherstep(double x, double start, double end) // this too
        {
            x = Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return x * x * x * (x * (6.0 * x - 15.0) + 10.0);
        }

        public static float Smootherstep(float x, float start, float end) // Float version
        {
            x = (float)Math.Clamp((x - start) / (end - start), 0.0, 1.0);

            return (float)(x * x * x * (x * (6.0 * x - 15.0) + 10.0));
        }

        public static double arf_Lerp(double x, double start, double end)
        {
            x = Math.Clamp(x, 0, 1);
            return start + (end - start) * x;
        }

        public static Vector2 arf_LerpedCursor(float x, Vector2 cursor, Vector2 target)
        {
            x = Math.Clamp(x, 0.0f, 1.0f);
    
         return new Vector2
         (
             cursor.X + (target.X - cursor.X) * x,
             cursor.Y + (target.Y - cursor.Y) * x
         );
        }

        double arf_kneeScaled(IDeviceReport value, double x) 
        {
            return knScale switch
            {
                > 0.0001f => (knScale) * arf_kneeFunc(x / (knScale)) + 1,
                _ => x > 0 ? 1 : 1 + x,
            };
        }
        
        double arf_inverseTanh(double x) => Math.Log((1 + x) / (1 - x), Math.E) / 2;

        double arf_inverseKneeScaled(IDeviceReport value, double x) 
        {
            double velocity = 1;
            return (velocity * knScale) * Math.Log(arf_inverseTanh(Math.Exp((x - 1) / (knScale * velocity))), Math.E);
        }

        double arf_derivKneeScaled(IDeviceReport value, double x)
        {
            var e = Math.Exp(x / (knScale));
            var tanh = Math.Tanh(e);
            return (e - e * (tanh * tanh)) / tanh;
        }

        double arf_getXOffset(IDeviceReport value) => arf_inverseKneeScaled(value, 0);

        double arf_getScaleComp(IDeviceReport value) => arf_derivKneeScaled(value, arf_getXOffset(value));

        public double arf_rOuterAdjusted(IDeviceReport value, Vector2 cursor, double rOuter, double rInner)
        {
            if (value is ITabletReport report)
            {
                double velocity = arf_vel * Math.Pow(arf_accelMult, accPower);
                return Math.Max(Math.Min(Math.Pow(velocity / vDiv, radPower), 1), minMult) * Math.Max(rOuter, rInner + 0.0001f);
            }
            else
            return 0;
        }

        public double arf_rInnerAdjusted(IDeviceReport value, Vector2 cursor, double rInner)
        {
            if (value is ITabletReport report)
            {
                double velocity = arf_vel * Math.Pow(arf_accelMult, accPower);
                return Math.Max(Math.Min(Math.Pow(velocity / vDiv, radPower), 1), minMult) * rInner;
            }
            else
            {
            return 0;
            }
        }

        double arf_leakedFn(IDeviceReport value, double x, double offset, double scaleComp)
        => arf_kneeScaled(value, x + offset) * (1 - leakCoef) + x * leakCoef * scaleComp;

        double arf_smoothedFn(IDeviceReport value, double x, double offset, double scaleComp)
        {
            double velocity = 1;
            double LowVelocityUnsmooth = 1;
            if (value is ITabletReport report)
            {
                velocity = arf_vel;
                LowVelocityUnsmooth = 1 + (arf_Smoothstep(arf_vel * arf_accelMult, vDiv, 0) * (minSmooth - 1));
            }

            return arf_leakedFn(value, x * (smoothCoef / LowVelocityUnsmooth) / scaleComp, offset, scaleComp);
        }

        double arf_scaleToOuter(IDeviceReport value, double x, double offset, double scaleComp)
        {
            if (value is ITabletReport report)
            {
                return (arf_rOuterAdjusted(value, arf_cursor, rOuter, rInner) - arf_rInnerAdjusted(value, arf_cursor, rInner)) * arf_smoothedFn(value, x / (arf_rOuterAdjusted(value, arf_cursor, rOuter, rInner) - arf_rInnerAdjusted(value, arf_cursor, rInner)), offset, scaleComp);
            }
            else
            {
                return (rOuter - rInner) * arf_smoothedFn(value, x / (rOuter - rInner), offset, scaleComp);
            } 
        }

        double arf_deltaFn(IDeviceReport value, double x, double offset, double scaleComp)
        {
            if (value is ITabletReport report)
            {
                return x > arf_rInnerAdjusted(value, arf_cursor, rInner) ? x - arf_scaleToOuter(value, x - arf_rInnerAdjusted(value, arf_cursor, rInner), offset / 1, scaleComp / 1) - arf_rInnerAdjusted(value, arf_cursor, rInner) : 0;
            }
            else
            {
                return x > rInner ? x - arf_scaleToOuter(value, x - rInner, offset, scaleComp) - arf_rInnerAdjusted(value, arf_cursor, rInner) : 0;
            }
        }


        public static float ClampedLerp(float start, float end, float scale)
        {
            scale = (float)Math.Clamp(scale, 0, 1);

            return start + scale * (end - start);
        }

        Vector2 arf_cursor;
        Vector2 arf_holdCursor;
        Vector2 arf_lastCursor;
         HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch(true);

        double arf_xOffset(IDeviceReport value) => arf_getXOffset(value);
        
        double arf_scaleComp(IDeviceReport value) => arf_getScaleComp(value);

        public Vector2 arf_last3Report;
        
        public Vector2 arf_lastLastReport;

        public Vector2 arf_lastReport;

        public Vector2 arf_currReport;

        public Vector2 arf_diff;

        public Vector2 arf_seconddiff;

        public Vector2 arf_thirddiff;

        public double arf_vel;

        public double arf_holdVel;

        public double arf_holdVel2;

        public double arf_lastVel;

        public double arf_last2Vel;

        public double arf_last3Vel;

        public double arf_last4Vel;

        public double arf_last5Vel;

        public double arf_last6Vel;

        public double arf_last7Vel;

        public double arf_last8Vel;

        public double arf_last9Vel;

        public double arf_spinCheck;

        public double arf_lastAccel;

        public double arf_accel;

        public double arf_lastJerk;

        public double arf_jerk;

        public double arf_snap;

        public double arf_accelMult;

        public double arf_lerpScale;

        public Vector2 arf_angleIndexPoint;

        public double arf_lastIndexFactor;

        public double arf_indexFactor;

        public double arf_angleIndex;

        public double arf_sinceSnap;

        public double arf_radiusGroundCount;

        public double arf_radiusGroundPosition;

        public Vector2 arf_groundedPoint;

        public Vector2 arf_groundedDiff;

        public double arf_distanceGround;

        public double arf_doubt;

        public double arf_sinceAccelTop;

        private double arf_reportMsAvg = 5;

        public float emaWeight;

        public Vector2 calc1Pos;

        public Vector2 calc2Pos;
    }
}