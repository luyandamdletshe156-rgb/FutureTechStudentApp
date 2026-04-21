using Microsoft.AspNetCore.Http;

namespace FutureTechStudentApp.Services
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a file and returns the blob filename.
        /// </summary>
        Task<string> UploadImageAsync(IFormFile file, string studentId);

        /// <summary>
        /// Generates a SAS token URL for a given blob file name.
        /// </summary>
        string GetSecureImageUrl(string? fileName);

        /// <summary>
        /// Removes a blob from storage.
        /// </summary>
        Task DeleteImageAsync(string fileName);
    }
}