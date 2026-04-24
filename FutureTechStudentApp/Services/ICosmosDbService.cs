using FutureTechStudentApp.Models;
using Microsoft.Azure.Cosmos;

public interface ICosmosDbService
{
    // The ICosmosDbService interface defines the contract for interacting with Azure Cosmos DB in the context of managing Student entities.
    Task<IEnumerable<Student>> GetStudentsAsync(QueryDefinition queryDefinition);
    Task<int> GetCountAsync(string? searchString);
    Task<int> GetActiveCountAsync();
    Task<Student?> GetStudentAsync(string id);
    Task AddStudentAsync(Student student);
    Task UpdateStudentAsync(string id, Student student);
    Task DeleteStudentAsync(string id);
}