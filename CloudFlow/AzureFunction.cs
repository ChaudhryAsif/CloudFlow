using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using HttpMultipartParser;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

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
            queueConnectionString = _configuration["AzureQueueConnectionString"];
        }

        /// <summary>
        /// Azure Function triggered on a schedule to perform periodic tasks.
        /// </summary>
        /// <param name="timerInfo"></param>
        /// <returns></returns>
        [Function("ProcessScheduledTask")]
        public async Task ProcessScheduledTask([TimerTrigger("0 */2 * * * *")] TimerInfo timerInfo)
        {
            try
            {
                _logger.LogInformation("Timer function executed at: {Time}", DateTime.UtcNow);

                // Simulate async processing (e.g., database update, API call, etc.)
                // Yield execution to allow other tasks to run before continuing. This helps prevent blocking and ensures better responsiveness.
                await Task.Yield();

                _logger.LogInformation("Timer function execution completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing the timer function.");
            }
        }

        [Function("SendDataToQueue")]
        public async Task<IActionResult> QueueDataAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "SendDataToQueue")] HttpRequest req)
        {
            _logger.LogInformation("Processing HTTP request to enqueue a message.");

            try
            {
                string queueName = _configuration["messageQueue"];
                if (string.IsNullOrEmpty(queueName))
                {
                    _logger.LogError("Queue name configuration is missing.");
                    return new StatusCodeResult(500);
                }

                // Read the request body
                string requestBody;
                using (StreamReader reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string message = data?.message;

                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Invalid request: 'message' is required in the body.");
                    return new BadRequestObjectResult("Please provide a 'message' in the request body.");
                }

                // Create and initialize the queue client
                QueueClient queueClient = new QueueClient(queueConnectionString, queueName);
                await queueClient.CreateIfNotExistsAsync();

                // Encode the message in Base64 format
                string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));

                // Send the message to the queue
                await queueClient.SendMessageAsync(base64Message);
                _logger.LogInformation("Message successfully enqueued: {Message}", message);

                return new OkObjectResult($"Message '{message}' sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return new StatusCodeResult(500);
            }
        }

        [Function("SendDataToServiceBus")]
        public async Task<IActionResult> SendDataToServiceBusAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "sendDataToServiceBus")] HttpRequest request)
        {
            _logger.LogInformation("Processing HTTP request to send data to Service Bus.");

            try
            {
                string connectionString = _configuration["ServiceBusConnectionString"];
                string queueName = _configuration["ServiceBusQueueName"];

                if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(queueName))
                {
                    _logger.LogError("Service Bus configuration is missing.");
                    return new StatusCodeResult(500);
                }

                // Read the request body
                string requestBody;
                using (StreamReader reader = new StreamReader(request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string message = data?.message;

                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Invalid request: 'message' is required in the body.");
                    return new BadRequestObjectResult("Please provide a 'message' in the request body.");
                }

                // Send the message to Service Bus
                await using var client = new ServiceBusClient(connectionString);
                await using var sender = client.CreateSender(queueName);
                ServiceBusMessage serviceBusMessage = new ServiceBusMessage(message);

                await sender.SendMessageAsync(serviceBusMessage);
                _logger.LogInformation("Message successfully sent to Service Bus: {Message}", message);

                return new OkObjectResult($"Message '{message}' sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending the message to Service Bus.");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Azure Function that triggers when a new message arrives in the specified queue.
        /// Processes the message asynchronously.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Function("ProcessQueueMessage")]
        public async Task ProcessQueueMessageAsync([QueueTrigger("messagequeue", Connection = "AzureQueueConnectionString")] string message)
        {
            _logger.LogInformation("Queue trigger function started processing a message.");

            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Received an empty or null message from the queue.");
                    return;
                }

                _logger.LogInformation($"Processing queue message: {message}");

                // Simulate async processing (e.g., database update, API call, etc.)
                // Yield execution to allow other tasks to run before continuing. This helps prevent blocking and ensures better responsiveness.
                await Task.Yield();

                _logger.LogInformation("Queue message processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the queue message.");
                throw; // Ensure the message is retried in case of failure
            }
        }

        /// <summary>
        /// Azure Function that triggers when a new message arrives in the specified Service Bus queue.
        /// Logs and processes the received message asynchronously.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Function("ProcessServiceBusMessage")]
        public async Task ProcessServiceBusMessage([ServiceBusTrigger("busqueue", Connection = "ServiceBusConnectionString")] string message)
        {
            try
            {
                _logger.LogInformation($"Processing Service Bus message: {message}");

                // Simulate async processing (e.g., database update, API call, etc.)
                await Task.Yield();

                _logger.LogInformation("Message processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Manually reads a message from the Azure Storage Queue and optionally deletes it after processing.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Function("ReadQueueMessageManually")]
        public async Task<IActionResult> ReadQueueMessageManually(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ReadQueueManually")] HttpRequest request)
        {
            try
            {
                _logger.LogInformation("Starting manual queue message retrieval...");

                QueueClient queueClient = new QueueClient(queueConnectionString, "messagequeue");

                // Ensure the queue exists before attempting to read
                if (!await queueClient.ExistsAsync())
                {
                    _logger.LogWarning("Queue does not exist.");
                    return new NotFoundObjectResult("Queue does not exist.");
                }

                // Retrieve a message from the queue
                QueueMessage retrievedMessage = (await queueClient.ReceiveMessageAsync())?.Value;

                if (retrievedMessage == null)
                {
                    _logger.LogInformation("No messages found in the queue.");
                    return new OkObjectResult("No messages available in the queue.");
                }

                // Decode Base64-encoded message text to string format for processing
                string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(retrievedMessage.MessageText));

                // Read request body to check if the message should be deleted after processing
                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                bool shouldDelete = false;

                if (!string.IsNullOrEmpty(requestBody))
                {
                    try
                    {
                        dynamic data = JsonConvert.DeserializeObject(requestBody);
                        shouldDelete = data?.delete ?? false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Invalid request body: {ex.Message}");
                    }
                }

                // Optionally delete the message after processing
                if (shouldDelete)
                {
                    await queueClient.DeleteMessageAsync(retrievedMessage.MessageId, retrievedMessage.PopReceipt);
                    _logger.LogInformation("Message deleted successfully.");
                }

                _logger.LogInformation("Message retrieved successfully.");
                return new OkObjectResult($"Message: {decodedMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading message from queue: {ex.Message}", ex);
                return new StatusCodeResult(500);
            }
        }

        [Function("UploadFileToBlob")]
        public async Task<IActionResult> UploadFileToBlobAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "UploadFileToBlob")] HttpRequest request)
        {
            try
            {
                // Retrieve configuration values
                string containerName = _configuration["ContainerName"];

                if (string.IsNullOrWhiteSpace(queueConnectionString) || string.IsNullOrWhiteSpace(containerName))
                {
                    _logger.LogError("Azure Storage configuration is missing.");
                    return new BadRequestObjectResult("Storage configuration is missing.");
                }

                // Create Blob Service and Container Client
                var blobServiceClient = new BlobServiceClient(queueConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Parse multipart form data
                var data = await MultipartFormDataParser.ParseAsync(request.Body);
                var file = data.Files.FirstOrDefault();

                if (file == null)
                {
                    _logger.LogWarning("No file was found in the request.");
                    return new BadRequestObjectResult("No file uploaded.");
                }

                // Upload file to blob storage
                BlobClient blobClient = containerClient.GetBlobClient(file.FileName);
                await blobClient.UploadAsync(file.Data, overwrite: true);

                _logger.LogInformation($"File uploaded successfully: {file.FileName}");
                return new OkObjectResult($"File uploaded successfully, FileName: {file.FileName}");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Blob Storage request failed.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during file upload.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function("DownloadBlobFile")]
        public async Task<IActionResult> DownloadBlobFileAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "DownloadBlobFile")] HttpRequest request)
        {
            try
            {
                _telemetryClient.TrackEvent("Processing file download request.");

                // Retrieve configuration values
                string containerName = _configuration["ContainerName"];

                if (string.IsNullOrWhiteSpace(queueConnectionString) || string.IsNullOrWhiteSpace(containerName))
                {
                    _logger.LogError("Azure Storage configuration is missing.");
                    return new BadRequestObjectResult("Storage configuration is missing.");
                }

                // Read request body
                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(requestBody);

                if (data?.fileName == null)
                {
                    _logger.LogError("File name is required.");
                    return new BadRequestObjectResult("File name is required.");
                }

                string fileName = data.fileName.ToString();
                _logger.LogInformation($"Attempting to download file: {fileName}");

                // Initialize Blob Client
                var blobServiceClient = new BlobServiceClient(queueConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                // Check if file exists
                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning($"File not found in blob storage: {fileName}");
                    return new NotFoundObjectResult($"File '{fileName}' not found.");
                }

                _logger.LogInformation($"Starting file download: {fileName}");

                // Download the file as stream
                BlobDownloadInfo download = await blobClient.DownloadAsync();
                MemoryStream memoryStream = new MemoryStream();
                await download.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset stream position

                _logger.LogInformation($"File successfully downloaded: {fileName}");

                // Return file as a downloadable stream
                return new FileStreamResult(memoryStream, "application/octet-stream")
                {
                    FileDownloadName = fileName
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Blob Storage request failed.");
                return new ObjectResult("Failed to retrieve file from Azure Blob Storage.") { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during file download.");
                return new ObjectResult("An error occurred while downloading the file.") { StatusCode = 500 };
            }
        }
    }
}
