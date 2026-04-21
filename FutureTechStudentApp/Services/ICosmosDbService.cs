using FutureTechStudentApp.Models;
using Microsoft.Azure.Cosmos;

public interface ICosmosDbService
{
    // 1. Updated to accept QueryDefinition for secure, parameterized queries
    Task<IEnumerable<Student>> GetStudentsAsync(QueryDefinition queryDefinition);

    // 2. Helper to get the count of total records (for pagination UI)
    Task<int> GetCountAsync(string? searchString);

    // --- ADDED THIS METHOD ---
    Task<int> GetActiveCountAsync();

    // 3. Keep these as they are
    Task<Student?> GetStudentAsync(string id);
    Task AddStudentAsync(Student student);
    Task UpdateStudentAsync(string id, Student student);
    Task DeleteStudentAsync(string id);
}