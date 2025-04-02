using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Rest;

namespace AzureFunctionApp
{
    public static class CreateResourceGroup   //this is a function implementation when evoked this creates a resource group, this picks the resouce group name and location from POST API request body
    {
        [FunctionName("CreateResourceGroup")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request to create a resource group.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Get parameters from request
            string resourceGroupName = data?.resourceGroupName;
            string location = data?.location;

            // Validate inputs
            if (string.IsNullOrEmpty(resourceGroupName) || string.IsNullOrEmpty(location))
            {
                return new BadRequestObjectResult("Please pass resourceGroupName and location in the request body");
            }

            try
            {
                // Get the managed identity token
                var azureServiceTokenProvider = new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

                // Create credentials using the token
                var tokenCredentials = new TokenCredentials(accessToken);

                // Create ResourceManagementClient with token credentials
                var resourceManagementClient = new ResourceManagementClient(tokenCredentials)
                {
                    SubscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")
                };

                // Check if resource group exists
                var exists = await resourceManagementClient.ResourceGroups.CheckExistenceAsync(resourceGroupName);

                if (exists)
                {
                    log.LogInformation($"Resource group {resourceGroupName} already exists.");
                    return new OkObjectResult($"Resource group {resourceGroupName} already exists in {location}");
                }

                // Create resource group
                var resourceGroup = await resourceManagementClient.ResourceGroups.CreateOrUpdateAsync(
                    resourceGroupName,
                    new ResourceGroup(location));

                log.LogInformation($"Created resource group {resourceGroupName} in {location}");

                // Return success
                return new OkObjectResult($"Successfully created resource group {resourceGroupName} in {location}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error creating resource group: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
