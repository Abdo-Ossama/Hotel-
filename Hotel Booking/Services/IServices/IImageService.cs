namespace CodegateTest.Services.IServices
{
    public interface IImageService
    {
        
        Task<string> UploadImageAsync(
            IFormFile image,
            string folderName
        );

        bool DeleteImage(
            string fileName,
            string folderName
        );
    }
}

