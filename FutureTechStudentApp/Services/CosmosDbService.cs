using FutureTechStudentApp.Models;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace FutureTechStudentApp.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        // The CosmosDbService class is responsible for interacting with Azure Cosmos DB.
        // It uses the CosmosClient to perform CRUD operations on the Student entities stored in a specific container.
        // The constructor initializes the container reference, and the methods provide functionality to get students, add a student, get a student by ID, update a student, delete a student, and count active students.
        private readonly Container _container;
        
        public CosmosDbService(CosmosClient dbClient, string databaseName, string containerName)
        {
            this._container = dbClient.GetContainer(databaseName, containerName);
        }
        
        public async Task<IEnumerable<Student>> GetStudentsAsync(QueryDefinition queryDefinition)
        {
            var iterator = this._container.GetItemQueryIterator<Student>(queryDefinition);
            List<Student> results = new List<Student>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        // Helper for Pagination
        public async Task<int> GetCountAsync(string? searchString)
        {
           
            string sql = "SELECT VALUE COUNT(1) FROM c WHERE 1=1";

            if (!string.IsNullOrEmpty(searchString))
            {
                sql += " AND (CONTAINS(LOWER(c.firstName), @search) OR CONTAINS(LOWER(c.lastName), @search) OR CONTAINS(LOWER(c.id), @search))";
            }

            QueryDefinition queryDef = new QueryDefinition(sql);

            if (!string.IsNullOrEmpty(searchString))
            {
                queryDef = queryDef.WithParameter("@search", searchString.ToLower());
            }

            var iterator = this._container.GetItemQueryIterator<int>(queryDef);
            int count = 0;
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                count += response.FirstOrDefault();
            }
            return count;
        }

        public async Task AddStudentAsync(Student student)
        {
            await this._container.CreateItemAsync<Student>(student, new PartitionKey(student.Id));
        }
        
        public async Task<Student?> GetStudentAsync(string id)
        {
            try
            {
          
                ItemResponse<Student> response = await this._container.ReadItemAsync<Student>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpdateStudentAsync(string id, Student student)
        {
            await this._container.UpsertItemAsync<Student>(student, new PartitionKey(id));
        }

        public async Task DeleteStudentAsync(string id)
        {
            await this._container.DeleteItemAsync<Student>(id, new PartitionKey(id));
        }

        public async Task<int> GetActiveCountAsync()
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.enrolmentStatus = 'Active'");

            var iterator = this._container.GetItemQueryIterator<int>(query);
            int count = 0;

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                count += response.FirstOrDefault();
            }

            return count;
        }

    }
}