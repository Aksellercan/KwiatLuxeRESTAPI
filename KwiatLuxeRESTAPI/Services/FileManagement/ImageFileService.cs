using KwiatLuxeRESTAPI.Models;

namespace KwiatLuxeRESTAPI.Services.FileManagement
{
    public class ImageFileService
    {
        public async Task<string> FileUpload(IFormFile imageFile)
        {
            if (imageFile == null)
            {
                throw new ArgumentNullException("Image is NULL");
            }
            string[] allowedExtensions = { ".jpeg", ".jpg", ".png" };
            bool allowed = false;
            foreach (string extension in allowedExtensions)
            {
                if(string.Equals(Path.GetExtension(imageFile.FileName), extension)) allowed = true;
            }
            if(!allowed) throw new Exception($"Only allowed extensions are {string.Join(", ", allowedExtensions)}, what was uploaded: {Path.GetExtension(imageFile.FileName)}");
            var contentPath = $"{Directory.GetParent(Directory.GetCurrentDirectory())}{Path.DirectorySeparatorChar}Uploads";
            if (!Directory.Exists(contentPath))
            {
                Directory.CreateDirectory(contentPath);
            }
            var fileName = $"{Guid.NewGuid().ToString()}{Path.GetExtension(imageFile.FileName)}";
            var fileNameWithPath = Path.Combine(contentPath, fileName);
            using var stream = new FileStream(fileNameWithPath, FileMode.Create);
            await imageFile.CopyToAsync(stream);
            return fileName;
        }

        public void DeleteFile(string fileToDelete)
        {
            string fullPath = $"{Directory.GetParent(Directory.GetCurrentDirectory())}{Path.DirectorySeparatorChar}Uploads{Path.DirectorySeparatorChar}{fileToDelete}";
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else
            {
                throw new ArgumentNullException("File Not Found");
            }
        }
    }
}
