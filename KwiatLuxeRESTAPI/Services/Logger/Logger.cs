using KwiatLuxeRESTAPI.Services.FileManagement;

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
        private static bool ConsoleOutput = true;
        private static FileUtil fileUtil = new();

        public static void setDebugOutput(bool setdebugOutput) 
        {
            debugOutput = setdebugOutput;
        }

        public static void setConsoleOutput(bool setConsoleOutput)
        {
            ConsoleOutput = setConsoleOutput;
        }

        public static void Log(this Severity severity, string message)
        {
            if ((severity == Severity.DEBUG) && !debugOutput && !ConsoleOutput) return;
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
            if (!ConsoleOutput) 
            {
                fileUtil.WriteFiles(formatted + severityString + message);
                return;
            }
            Console.WriteLine(formatted + severityString + message);
            Console.ResetColor();
        }
    }
}
