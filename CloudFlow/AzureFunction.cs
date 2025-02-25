using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using HttpMultipartParser;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace AzzurFunctionApp
{
    public class AzureFunction
    {
        private readonly ILogger<AzureFunction> _logger;
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;
        private string queueConnectionString;

        public AzureFunction(ILogger<AzureFunction> logger, IConfiguration configuration, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _configuration = configuration;
            _telemetryClient = telemetryClient;
            queueConnectionString = _configuration["AzureWebJobsStorageConnection"];
        }

        [Function("DataSync")]
        public async Task Timer([TimerTrigger("0 */2 * * * *")] TimerInfo timerInfo)
        {
            _logger.BeginScope("Timer Trigger");
        }

        [Function("SendMsgToQueue")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "SendMsgToQueue")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("C# HTTP trigger function processed a request.");
                var queueName = _configuration["messageQueue"];

                // read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string message = data?.message;

                if (string.IsNullOrEmpty(message))
                {
                    return new BadRequestObjectResult("Please provide a 'message' in the request body.");
                }

                // Create QueueClient
                QueueClient queueClient = new QueueClient(queueConnectionString, queueName);
                await queueClient.CreateIfNotExistsAsync();

                string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));

                // Send the message to the queue
                await queueClient.SendMessageAsync(base64Message);

                return new OkObjectResult($"Message '{message}' sent successfully.");
            }
            catch (Exception ex)
            {
                return new StatusCodeResult(500);
            }
        }

        [Function("sendMsgToServiceBus")]
        public async Task<IActionResult> SendMessageToServiceBus([HttpTrigger(AuthorizationLevel.Function, "post", Route = "sendMsgToServiceBus")] HttpRequest request)
        {
            try
            {
                // read request body
                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string message = data?.message;

                if (string.IsNullOrEmpty(message))
                {
                    return new BadRequestObjectResult("Please provide a 'message' in the request body.");
                }

                // Create a Service Bus client
                await using var client = new ServiceBusClient(_configuration["ServiceBusConnectionString"]);
                await using var sender = client.CreateSender(_configuration["ServiceBusQueueName"]);
                ServiceBusMessage serviceBusMessage = new ServiceBusMessage(message);
                await sender.SendMessageAsync(serviceBusMessage);
                return new OkObjectResult($"Message '{message}' sent successfully.");
            }
            catch (Exception ex)
            {
                return new StatusCodeResult(500);
            }
        }

        //[Function("QueueTriggerFunction")]
        public async Task QueueMessage([QueueTrigger("messagequeue", Connection = "AzureWebJobsStorageConnection")] string message)
        {
            _logger.LogInformation($"Queue trigger function processed: {message}");
        }

        [Function("ServiceBusTriggerFunction")]
        public async Task ServiceBusMessage([ServiceBusTrigger("busqueue", Connection = "ServiceBusConnectionString")] string message)
        {
            _logger.LogInformation($"Service Bus trigger function processed: {message}");
        }

        /// <summary>
        /// Read message from the queue manually
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Function("ReadManuallyQueue")]
        public async Task<IActionResult> ReadManualQueue([HttpTrigger(AuthorizationLevel.Function, "post", Route = "ReadManuallyQueue")] HttpRequest request)
        {
            try
            {
                QueueClient client = new QueueClient(queueConnectionString, "messagequeue");

                if (await client.ExistsAsync())
                {
                    var response = await client.ReceiveMessageAsync();

                    if (response != null && response.Value != null)
                    {
                        // get the Base64-encoded message text
                        string base64Message = response.Value.MessageText;

                        // decode Base64 to string
                        string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(base64Message));

                        // read request body
                        string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                        dynamic data = JsonConvert.DeserializeObject(requestBody);
                        bool isDelete = data?.delete ?? false;

                        if (isDelete)
                        {
                            // Delete message after processing
                            await client.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                            Console.WriteLine("Message deleted successfully.");
                        }

                        return new OkObjectResult($"Message: {decodedMessage}");
                    }
                }

                return new OkObjectResult("No message in the queue");
            }
            catch (Exception)
            {
                return new OkObjectResult("No message in the queue");
            }
        }

        [Function("UploadFile")]
        public async Task<IActionResult> UploadFile([HttpTrigger(AuthorizationLevel.Function, "post", Route = "UploadFile")] HttpRequest request)
        {
            try
            {
                string connectionString = _configuration["AzureStorageConnectionString"];
                string containerName = _configuration["containerName"]; ;

                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var data = await MultipartFormDataParser.ParseAsync(request.Body);
                var file = data.Files.FirstOrDefault();

                BlobClient blobClient = containerClient.GetBlobClient(file.FileName);
                await blobClient.UploadAsync(file.Data, true);

                return new OkObjectResult($"File uploaded Successfully, FileName: {file.FileName}");
            }
            catch (Exception)
            {
                return new OkObjectResult("File not uploaded!");
            }
        }

        [Function("downloadFile")]
        public async Task<IActionResult> DownloadFileFromAzureBlobStorage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "downloadFile")] HttpRequest request)
        {
            try
            {
                _telemetryClient.TrackEvent("Processing the download file");
                string connectionString = _configuration["AzureStorageConnectionString"];
                string containerName = _configuration["containerName"];

                // Read request body
                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject<dynamic>(requestBody);

                if (data == null || data.fileName == null)
                {
                    _logger.LogError("File Name is required.");
                    return new BadRequestObjectResult("File Name is required.");
                }

                _logger.LogInformation($"File name: {data.fileName} to be download");
                string fileName = data.fileName.ToString();
                string downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");

                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                string filePath = Path.Combine(downloadsPath, fileName);

                _logger.LogInformation($"Initialize blob client");

                // Initialize Blob Client
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                _logger.LogInformation($"Start downloading the file");

                // Download the file
                BlobDownloadInfo download = await blobClient.DownloadAsync();
                await using FileStream downloadFileStream = File.OpenWrite(filePath);
                await download.Content.CopyToAsync(downloadFileStream);
                await downloadFileStream.FlushAsync();

                _logger.LogInformation($"File downloaded successfully");

                return new OkObjectResult($"File downloaded successfully at path: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                return new ObjectResult($"Error downloading file: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
