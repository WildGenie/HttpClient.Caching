namespace CacheCow
{
    using System;
    using System.Diagnostics;

    internal static class TraceWriter
    {
        public const string CacheCowTraceSwitch = "CacheCow";
        private static readonly TraceSwitch _switch = new TraceSwitch(CacheCowTraceSwitch, "CacheCow Trace Switch");


        public static void WriteLine(string message, TraceLevel level, params object[] args)
        {
            if(_switch.Level < level)
                return;

            var dateTimeOfEvent = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff");
            var callingMethod = string.Empty;
            try
            {
                callingMethod = new StackFrame(1).GetMethod().Name;
            }
            catch
            {
                // swallow 
            }


            Trace.WriteLine(string.Format("{0} - {1}: {2}",
                dateTimeOfEvent,
                callingMethod,
                args.Length == 0 ? message : string.Format(message, args)
            ));
        }
    }
}