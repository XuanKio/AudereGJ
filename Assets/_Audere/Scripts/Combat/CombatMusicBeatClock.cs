using System;
namespace Audere.Combat
{
    /// <summary>Schedules telegraphs so activation lands on the music grid; skips missed beats.</summary>
    public sealed class CombatMusicBeatClock
    {
        private bool initialized;
        private double previousTime, nextLaunch;
        public static double NextLaunch(double time, double period, double offset, double lead)
        {
            return Math.Ceiling((time + lead - offset - 0.000001) / period) * period + offset - lead;
        }
        public bool Tick(double time, double period, double offset, double lead, double activeStep = .1)
        {
            if (period <= 0 || double.IsNaN(time)) return false;
            if (!initialized || time < previousTime - .05 || time - nextLaunch > period ||
                time - previousTime > Math.Max(.1, activeStep * 1.5))
            {
                nextLaunch = NextLaunch(time, period, offset, lead);
                initialized = true;
            }
            previousTime = time;
            if (time + .000001 < nextLaunch) return false;
            nextLaunch = NextLaunch(time + .001, period, offset, lead);
            return true;
        }
    }
}
