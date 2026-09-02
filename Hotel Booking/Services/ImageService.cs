using CodegateTest.Services.IServices;

namespace CodegateTest.Services
{
    public class ImageService : IImageService
    {
        private readonly string[] _allowedExtensions =
        {
        ".png",
        ".jpg",
        ".jpeg"
    };

        public async Task<string> UploadImageAsync(
            IFormFile image,
            string folderName
        )
        {
            var extension =
                Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (!_allowedExtensions.Contains(extension))
            {
                throw new Exception(
                    "Only PNG and JPG images are allowed"
                );
            }

            var newFile =
                Guid.NewGuid().ToString()[..7] +
                DateTime.UtcNow.ToString("yyyy-MM-dd") +
                extension;

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "img",
                folderName,
                newFile
            );

            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!
            );

            using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            return newFile;
        }

        public bool DeleteImage(
      string fileName,
      string folderName
  )
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            var oldPhotoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "img",
                folderName,
                fileName
            );

            if (System.IO.File.Exists(oldPhotoPath))
            {
                System.IO.File.Delete(oldPhotoPath);

                return true;
            }

            return false;
        }
    }
}
