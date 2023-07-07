using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using FileStorageApi.Interfaces;
using FileStorageApi.Models;
using FileStorageApi.Helpers;
using System.IO;
using Azure;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Concurrent;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.WindowsAzure.Storage;

namespace FileStorageApi.DataWrapper
{
        public class AzureRepositoryWrapper : IFileRepository
        {
                private BlobContainerClient BlobContainerClient;

                private TableServiceClient TableServiceClient;

                private CloudStorageAccount CloudStorageAccount;

                private CloudTableClient СloudTableClient;

                private CloudTable CloudTable;

                private TableClient TableClient;

                private const string ConstTableName = "filestoragetable";

                private const int ConstPasswordLength = 12;

                private const string ConstPartitionKey = "File";

                private IMemoryCache MemoryCache;

                public AzureRepositoryWrapper(BlobContainerClient blobServiceClient, TableServiceClient tableServiceClient, CloudStorageAccount cloudStorageAccount, IMemoryCache memoryCache)
                {
                        BlobContainerClient = blobServiceClient;

                        TableServiceClient = tableServiceClient;

                        CloudStorageAccount = cloudStorageAccount;

                        TableClient = TableServiceClient.GetTableClient(tableName: ConstTableName);

                        TableClient.CreateIfNotExists();

                        MemoryCache = memoryCache;

                        СloudTableClient = CloudStorageAccount.CreateCloudTableClient();

                        CloudTable = СloudTableClient.GetTableReference(ConstTableName);
                }

                public (IList<string>? links, string password) UploadFiles(List<IFormFile> files, int id, string[] tags, bool secureByPassword)
                {
                        List<string> FileUrls, fileGuids;

                        StorageFileTable azureStorageFileTable;

                        string password = InitDbFile(files, id, tags, secureByPassword, out FileUrls, out fileGuids, out azureStorageFileTable);

                        //var responce = TableClient.AddEntity(azureStorageFileTable);
                        this.CloudTable.ExecuteAsync(TableOperation.Insert(azureStorageFileTable));

                        //if (!responce.IsError)
                        FileUrls = AddFilesToAzureBlobStorage(files, fileGuids);

                        SaveLinksCache(id, FileUrls);

                        return (FileUrls, password);
                }

                private static string InitDbFile(List<IFormFile> files, int id, string[] tags, bool secureByPassword, out List<string> FileUrls, out List<string> fileGuids, out StorageFileTable azureStorageFileTable)
                {
                        string fileNamesJson, fileGuidsJson;

                        InitJsonObjects(files, out FileUrls, out fileGuids, out fileNamesJson, out fileGuidsJson);

                        string tag = ParseTags(tags);

                        string generatedPassword = secureByPassword ? StringHelper.GenerateRandomString(ConstPasswordLength) : null;

                        azureStorageFileTable = new StorageFileTable()
                        {
                                PartitionKey = ConstPartitionKey,

                                RowKey = id.ToString(),

                                FileNamesJson = fileNamesJson,

                                FileGuidsJson = fileGuidsJson,

                                Tags = tag,

                                Password = generatedPassword
                        };

                        return generatedPassword;
                }

                private static void InitJsonObjects(List<IFormFile> files, out List<string> FileUrls, out List<string> fileGuids, out string fileNamesJson, out string fileGuidsJson)
                {
                        FileUrls = new List<string>();

                        fileGuids = new List<string>();

                        fileNamesJson = string.Empty;

                        fileGuidsJson = string.Empty;

                        if (files != null && files.Any())
                        {
                                List<string> fileNames = files.Select(x => x.FileName).ToList();

                                fileGuids = Enumerable.Range(0, files.Count)
                                    .Select(_ => Guid.NewGuid().ToString())
                                    .ToList();

                                fileNamesJson = JsonConvert.SerializeObject(fileNames);

                                fileGuidsJson = JsonConvert.SerializeObject(fileGuids);
                        }
                }

                private void SaveLinksCache(int id, List<string> FileUrls)
                {
                        MemoryCache.Set(id.ToString(), FileUrls, new MemoryCacheEntryOptions
                        {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                        });
                }

                private List<string> AddFilesToAzureBlobStorage(List<IFormFile> files, List<string> fileGuids)
                {
                        List<string> FileUrls = new List<string>();

                        if (files == null)
                                return FileUrls;


                        for (int i = 0; i < files.Count; i++)
                        {
                                var client = BlobContainerClient.GetBlobClient(fileGuids[i]);

                                using (var stream = files[i].OpenReadStream())
                                {
                                        client.Upload(stream);
                                }

                                var fileUrl = CreateFileUrl(client, files[i].FileName);

                                FileUrls.Add(fileUrl);
                        }

                        return FileUrls;
                }

                public bool IsIdOccupied(int id)
                {
                        try
                        {
                                var retrieveOperation = TableOperation.Retrieve<StorageFileTable>(ConstPartitionKey, id.ToString());

                                var result = this.CloudTable.ExecuteAsync(retrieveOperation).Result;

                                var existingEntity = (StorageFileTable)result.Result;

                                if (existingEntity != null)
                                        return true;

                                return false;
                        }
                        catch (RequestFailedException ex) when (ex.Status == 404)
                        {
                                return false;
                        }
                }
                public string CreateFileUrl(string fileId, string fileName)
                {
                        var blobClient = BlobContainerClient.GetBlobClient(fileId);

                        return CreateFileUrl(blobClient, fileName);
                }
                public string CreateFileUrl(BlobClient blobClient, string fileName)
                {

                        // Check if BlobContainerClient object has been authorized with Shared Key
                        if (blobClient.CanGenerateSasUri)
                        {

                                var blobHttpHeaders = new BlobHttpHeaders
                                {
                                        ContentDisposition = $"attachment; filename=\"{fileName}\""
                                };
                                // Create a SAS token that's valid for one day
                                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                                {
                                        BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                                        BlobName = blobClient.Name,
                                        Resource = "b"
                                };

                                sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddDays(31);

                                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);

                                blobClient.SetHttpHeaders(blobHttpHeaders);
                                //Uri sasURI = blobClient.GenerateSasUri(sasBuilder);

                                return blobClient.GenerateSasUri(sasBuilder).OriginalString;
                        }
                        else
                        {
                                // Client object is not authorized via Shared Key
                                return null;
                        }
                }

                public bool IsPasswordValid(int id, string password)
                {
                        var retrieveOperation = TableOperation.Retrieve<StorageFileTable>(ConstPartitionKey, id.ToString());

                        var result = this.CloudTable.ExecuteAsync(retrieveOperation).Result;

                        var existingEntity = (StorageFileTable)result.Result;

                        //var entity = TableServiceClient.GetTableClient(ConstTableName).GetEntity<StorageFileTable>(ConstPartitionKey, id.ToString());

                        if (existingEntity.Password == null || existingEntity.Password == password)
                                return true;

                        return false;
                }

                public StorageFileTable? GetFiles(int id)
                {
                        try
                        {
                                var retrieveOperation = TableOperation.Retrieve<StorageFileTable>(ConstPartitionKey, id.ToString());

                                var result = this.CloudTable.ExecuteAsync(retrieveOperation).GetAwaiter().GetResult();

                                var existingEntity = (StorageFileTable)result.Result;

                                //var entity = TableServiceClient.GetTableClient(ConstTableName).GetEntity<StorageFileTable>(ConstPartitionKey, id.ToString());

                                return existingEntity;
                        }
                        catch (RequestFailedException ex) when (ex.Status == 404)
                        {
                                return null;
                        }
                }

                public IList<FileResponce> UpdateFilesLinks(StorageFileTable? entity)
                {
                        List<string> deserializedFileNames = JsonConvert.DeserializeObject<List<string>>(entity.FileNamesJson);

                        List<string> deserializedFileIds = JsonConvert.DeserializeObject<List<string>>(entity.FileGuidsJson);

                        List<string> fileUrls;

                        fileUrls = TryToGetFromCache(entity.RowKey);

                        if (fileUrls == null)
                                fileUrls = deserializedFileIds.Select((fileId, index) => CreateFileUrl(fileId, deserializedFileNames[index])).ToList();


                        IList<FileResponce> fileResponsesUrls = deserializedFileNames
                          .Zip(fileUrls, (fileName, FileUrls) => new FileResponce { FileName = fileName, FileLink = FileUrls })
                          .ToList();

                        return fileResponsesUrls;
                }

                private List<string> TryToGetFromCache(string idUrl)
                {

                        var fileLinks = (List<string>)MemoryCache.Get(idUrl);

                        return fileLinks;
                }

                public (IList<string>? links, string password) UpdateFilesAsync(DateModel dateModel)
                {

                        var retrieveOperation = TableOperation.Retrieve<StorageFileTable>(ConstPartitionKey, dateModel.Id.ToString());

                        var result = CloudTable.ExecuteAsync(retrieveOperation).Result;

                        var entity = (StorageFileTable)result.Result;

                        List<string> FileUrls, fileGuids;

                        string fileNamesJson, fileGuidsJson;

                        InitJsonObjects(dateModel.MainDateModel.Files, out FileUrls, out fileGuids, out fileNamesJson, out fileGuidsJson);

                        string tag = ParseTags(dateModel.Tags);

                        entity.FileNamesJson = fileNamesJson;

                        entity.FileGuidsJson = fileGuidsJson;

                        entity.Tags = tag;

                        entity.Password = dateModel.MainDateModel.SecureByPassword.GetValueOrDefault() ? StringHelper.GenerateRandomString(ConstPasswordLength) : null;

                        var updateOperation = TableOperation.Replace(entity);

                        _ = this.CloudTable.ExecuteAsync(updateOperation).Result;

                        FileUrls = AddFilesToAzureBlobStorage(dateModel.MainDateModel.Files, fileGuids);

                        SaveLinksCache(dateModel.Id, FileUrls);

                        return (FileUrls, entity.Password);
                }

                private static string ParseTags(string[] tags)
                {
                        string tag;

                        if (tags == null)
                                tag = "Tags not selected";
                        else
                                tag = tags.Length > 0 ? string.Join(" ", tags) : "Tags not selected";
                        return tag;
                }
        }
}
