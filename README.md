
FutureTech Student Management System

Project Overview

This is a secure, cloud-based Student Management System (SMS) developed for the APDA 301 module. 
The application is built with .NET Core and is designed to provide authorized administrators with a robust dashboard to manage student records efficiently. 
The system emphasizes security, scalability, and cloud-native architecture by leveraging Microsoft Azure services.

Key Features

Secure Authentication: Multi-provider OAuth 2.0 (Google & GitHub) for admin-only access.
Student CRUD Operations: Full lifecycle management including creation, reading, updating, and two-stage deletion (Soft/Permanent).
Cloud Data Storage: Scalable, NoSQL data persistence using Azure Cosmos DB.
Secure File Handling: Profile images stored in Azure Blob Storage with restricted, time-limited access via SAS (Shared Access Signature) tokens.
User Interface: Intuitive Admin Dashboard with data pagination, search functionality, and responsive design.
Security & Monitoring: Form validation, CSRF protection, and real-time performance/health monitoring via Azure Application Insights.

Live Deployment

The application is live and hosted on Microsoft Azure App Service.
🔗 https://futuretech-academy-bnc7ckf9bzgngyck.southafricanorth-01.azurewebsites.net/

Technical Architecture
Framework: .NET Core (MVC Pattern)
Cloud Provider: Microsoft Azure
Database: Azure Cosmos DB (SQL API)
Storage: Azure Blob Storage
Monitoring: Azure Application Insights

How to Run Locally

Prerequisites
.NET 8.0 SDK or higher.
Visual Studio 2022 (with Azure development workload).
An active Azure Subscription for backend services.

Setup Instructions
Clone the repository:
code
Bash
git clone [https://www.google.com/url?sa=E&q=https%3A%2F%2Fgithub.com%2Fluyandamdletshe156-rgb%2FFutureTechStudentApp]
Restore Dependencies: Open the solution in Visual Studio; NuGet packages will restore automatically.

Configure Secrets:
For security, sensitive credentials (Azure/OAuth keys) are not committed to source control.
Right-click the project in Solution Explorer -> Manage User Secrets.
Add your local connection strings and OAuth Client IDs in the following format:
code

JSON
{
  "CosmosDb": { "Account": "...", "Key": "...", "DatabaseName": "...", "ContainerName": "..." },
  "BlobStorage": { "ConnectionString": "..." },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  }
}

Run: Press F5 to build and debug the application.

Project Structure

Controllers: Manages authentication flows and CRUD business logic.
Services: Houses Azure-specific logic (CosmosDbService, BlobStorageService).
Models: Defines data structures for student records.
Views: Contains Razor templates for the Admin Dashboard and login interface.

Security Disclosure
This project implements role-based access control. 
Only administrators whose emails are explicitly whitelisted within the application configuration can access the system.
Unauthorized login attempts are handled with secure error messaging.

Project developed for APDA 301.