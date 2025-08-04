using KwiatLuxeRESTAPI.Services.FileManagement;

namespace KwiatLuxeRESTAPI.Services.Logger
{
    public enum Logger
    {
        INFO,
        WARN,
        DEBUG,
        ERROR,
    }

    public static class LoggerClass
    {
        private static bool _debugOutput = false;
        private static bool _consoleOutput = true;

        public static void SetDebugOutput(bool setdebugOutput)
        {
            _debugOutput = setdebugOutput;
        }

        public static void SetConsoleOutput(bool setConsoleOutput)
        {
            _consoleOutput = setConsoleOutput;
        }

        private static string FormatMessage(Logger severity, string message)
        {
            string severityString = " [ " + severity.ToString() + " ] ";
            DateTime timeNow = DateTime.Now;
            string formatted = timeNow.ToString("dd/MM/yyyy HH:mm:ss");
            switch (severity)
            {
                case Logger.INFO:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case Logger.WARN:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;

                case Logger.DEBUG:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case Logger.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ResetColor();
                    break;
            }

            return formatted + severityString + message;
        }

        public static void Log(this Logger severity, string message)
        {
            if ((severity == Logger.DEBUG) && !_debugOutput && !_consoleOutput) goto writeFile;
            if ((severity == Logger.DEBUG) && !_debugOutput) return;
            writeFile:
            string outputMessage = FormatMessage(severity, message);
            if (!_consoleOutput)
            {
                FileUtil fileUtil = new();
                fileUtil.WriteFiles(outputMessage);
                return;
            }

            Console.WriteLine(outputMessage);
            Console.ResetColor();
        }
    }
}