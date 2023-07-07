using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FileStorageApi.Models
{
        public class ResponceDateModel
        {
                public int Id { get; set; }
                public IList<FileResponce>? FileResponce { get; set; }
        }

        public class FileResponce
        {
                public string? FileName { get; set; }
                public string? FileLink { get; set; }
        }

        public class RootResponce
        {
                public string? Status { get; set; }
                public string? Tags { get; set; }
                public string? Password { get; set; }
                public ResponceDateModel ResponceDateModel { get; set; }
        }
}
