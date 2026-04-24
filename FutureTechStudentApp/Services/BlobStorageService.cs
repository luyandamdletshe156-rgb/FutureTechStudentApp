using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace FutureTechStudentApp.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        // The BlobServiceClient is the main client for interacting with Azure Blob Storage.
        // It is initialized in the constructor using a connection string from the configuration.

        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "student-images";

        public BlobStorageService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration["BlobStorage:ConnectionString"]);
        }

   
        public async Task<string> UploadImageAsync(IFormFile file, string studentId)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("No file provided.");

            
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


        public string GetSecureImageUrl(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "https://ui-avatars.com/api/?name=Student&background=random";
            }

        
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

      
        public async Task DeleteImageAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

    
            if (fileName.Contains("?sv="))
            {
                try
                {
                    fileName = Path.GetFileName(new Uri(fileName).LocalPath);
                }
                catch { }
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

       
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            }
            catch (Exception ex)
            {
              
                throw new Exception($"Failed to delete blob {fileName}: {ex.Message}");
            }
        }
    }
}