namespace KwiatLuxeRESTAPI.Services.Logger
{
    public enum Severity
    {
        INFO,
        WARN,
        DEBUG,
        ERROR,
    }

    public static class Logger
    {
        private static bool debugOutput = false;

        public static void setDebugOutput(bool setdebugOutput) 
        {
            debugOutput = setdebugOutput;
        }

        public static void Log(this Severity severity, string message)
        {
            if ((severity == Severity.DEBUG) && !debugOutput) return;
            string severityString = " [ " + severity.ToString() + " ] ";
            DateTime timenow = DateTime.Now;
            string formatted = timenow.ToString("dd/MM/yyyy HH:mm:ss");

            switch (severity)
            {
                case Severity.INFO:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case Severity.WARN:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;

                case Severity.DEBUG:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case Severity.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                default:
                    Console.ResetColor();
                    break;
            }

            Console.WriteLine(formatted + severityString + message);
            Console.ResetColor();
        }
    }
}
