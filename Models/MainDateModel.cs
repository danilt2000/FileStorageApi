namespace FileStorageApi.Models
{
        public class MainDateModel
        {
                public List<IFormFile>? Files { get; set; }

                public bool? SecureByPassword { get; set; }
        }
}
