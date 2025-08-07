namespace KwiatLuxeRESTAPI.Services.FileManagement
{
    public class FileUtil
    {
        private readonly string _fullPath = $"{Directory.GetParent(Directory.GetCurrentDirectory())}{Path.DirectorySeparatorChar}Logs";
        private void CreateFolders()
        {
            try
            {
                Directory.CreateDirectory(_fullPath);
            }
            catch (Exception e) {
                throw new Exception($"Cannot create folders {e}");
            }
        }

        private bool FolderExists()
        {
            return Directory.Exists(_fullPath);
        }

        private string FilenameFormat()
        {
            DateTime timenow = DateTime.Now;
            string formatted = "log_" + timenow.ToString("dd-MM-yyyy");
            return formatted;
        }
        public void WriteFiles(string message)
        {
            try
            {
                if (!FolderExists())
                {
                    CreateFolders();
                }
                StreamWriter streamWriter = new(_fullPath + Path.DirectorySeparatorChar + FilenameFormat() + ".log", true);
                if (message == null)
                {
                    throw new Exception("Message is null");
                }
                streamWriter.WriteLine(message);
                streamWriter.Close();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {e}");
                Console.ResetColor();
            }
        }
    }
}
