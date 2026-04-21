using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace FutureTechStudentApp.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "student-images";

        public BlobStorageService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["BlobStorage:ConnectionString"]);
        }

        // --- UPLOAD: Returns clean filename for Cosmos DB storage ---
        public async Task<string> UploadImageAsync(IFormFile file, string studentId)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("No file provided.");

            // Validation
            if (file.Length > 2 * 1024 * 1024) throw new ArgumentException("File exceeds 2MB limit.");
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") throw new ArgumentException("Only JPEG/PNG files are allowed.");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            string fileName = $"student-{studentId}{ext}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return fileName;
        }

        // --- GET SECURE URL: Generates fresh 1-hour SAS tokens ---
        public string GetSecureImageUrl(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "https://ui-avatars.com/api/?name=Student&background=random";
            }

            // Cleanup: Ensure we are only working with the filename, not an old expired URI
            if (fileName.Contains("?sv="))
            {
                try
                {
                    fileName = Path.GetFileName(new Uri(fileName).LocalPath);
                }
                catch
                {
                    return "https://ui-avatars.com/api/?name=Error&background=f00";
                }
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            // Generate SAS Token
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        // --- DELETE: Removes orphaned blobs from Azure ---
        public async Task DeleteImageAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            // Handle cases where a full URI might have been passed
            if (fileName.Contains("?sv="))
            {
                try
                {
                    fileName = Path.GetFileName(new Uri(fileName).LocalPath);
                }
                catch { /* Ignore */ }
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                // Use DeleteIfExists to prevent 404 errors if the file is already gone
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            }
            catch (Exception ex)
            {
                // Log failure to clean up blob
                throw new Exception($"Failed to delete blob {fileName}: {ex.Message}");
            }
        }
    }
}