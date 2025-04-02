using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace AzureFunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a mock HTTP request
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Method = "POST";
            request.ContentType = "application/json";

            // Create request body
            var requestBody = new
            {
                resourceGroupName = "myResourceGroup",
                location = "westus"
            };
            var json = JsonConvert.SerializeObject(requestBody);
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Create a logger
            ILogger logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");

            // Call the Run method
            IActionResult result = await CreateResourceGroup.Run(request, logger);

            // Process the result
            if (result is OkObjectResult okResult)
            {
                Console.WriteLine($"Success: {okResult.Value}");
            }
            else if (result is BadRequestObjectResult badRequestResult)
            {
                Console.WriteLine($"Bad Request: {badRequestResult.Value}");
            }
            else if (result is StatusCodeResult statusCodeResult)
            {
                Console.WriteLine($"Error: {statusCodeResult.StatusCode}");
            }
        }
    }
}
