using Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace FileStorageApi.Models
{
        public class StorageFileTable : TableEntity
        {
                //public string RowKey { get; set; } = default!;

                //public string PartitionKey { get; set; } = default!;

                public string? FileNamesJson { get; set; }

                public string? FileGuidsJson { get; set; }

                public string? Tags { get; set; }

                public string? Password { get; set; }

                //public ETag ETag { get; set; } = default!;

                //public DateTimeOffset? Timestamp { get; set; } = default!;

        }
}
