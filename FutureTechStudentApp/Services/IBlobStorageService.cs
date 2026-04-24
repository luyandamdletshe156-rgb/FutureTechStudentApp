using Microsoft.AspNetCore.Http;

namespace FutureTechStudentApp.Services
{
    public interface IBlobStorageService
    {
        // The IBlobStorageService interface defines the contract for interacting with Azure Blob Storage.
        // It includes methods for uploading an image, retrieving a secure URL for an image, and deleting an image.
        // The implementation of this interface will handle the actual communication with Azure Blob Storage, including validation of files and generation of secure URLs.
        Task<string> UploadImageAsync(IFormFile file, string studentId);

        string GetSecureImageUrl(string? fileName);

        Task DeleteImageAsync(string fileName);
    }
}