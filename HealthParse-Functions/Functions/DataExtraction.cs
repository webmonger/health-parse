using HealthParse.Mail;
using HealthParse.Standard.Mail;
using HealthParseFunctions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace HealthParse
{
    public static class DataExtraction
    {
        [FunctionName("ExtractData")]
        public static void Run(
            [QueueTrigger(queueName: Fn.Qs.IncomingMail, Connection = Fn.ConnectionKeyName)]CloudQueueMessage message,
            [Queue(queueName: Fn.Qs.OutgoingMail, Connection = Fn.ConnectionKeyName)]ICollector<string> outputQueue,
            [Queue(queueName: Fn.Qs.ErrorNotification, Connection = Fn.ConnectionKeyName)]ICollector<string> errorQueue,
            TraceWriter log)
        {
            var storageConfig = Fn.StorageConfig.Load();
            var storageAccount = CloudStorageAccount.Parse(storageConfig.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var incomingContainer = blobClient.GetContainerReference(storageConfig.IncomingMailContainerName);
            var outgoingContainer = blobClient.GetContainerReference(storageConfig.OutgoingMailContainerName);
            var errorContainer = blobClient.GetContainerReference(storageConfig.ErrorMailContainerName);
            var originalEmail = EmailStorage.LoadEmailFromStorage(message.AsString, incomingContainer);

            var reply = MailUtility.ProcessEmail(originalEmail, Fn.EmailConfig.Load().FromEmailAddress);

            if (!reply.WasSuccessful)
            {
                var erroredFile = EmailStorage.SaveEmailToStorage(originalEmail, errorContainer);
                errorQueue.Add(erroredFile);
                log.Info($"enqueueing error - {erroredFile}");
            }
            EmailStorage.DeleteEmailFromStorage(message.AsString, incomingContainer);

            var filename = EmailStorage.SaveEmailToStorage(reply.Value, outgoingContainer);
            outputQueue.Add(filename);
            log.Info($"extracted data, enqueueing reply - {reply.Value.To} - {filename}");
        }

    }
}