using Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace FileStorageApi.Models
{
        public class StorageFileTable : TableEntity
        {
                public string? FileNamesJson { get; set; }

                public string? FileGuidsJson { get; set; }

                public string? Tags { get; set; }

                public string? Password { get; set; }

        }
}
