using FileStorageApi.Models;

namespace FileStorageApi.Interfaces
{
        public interface IFileRepository
        {
                (IList<string>? links, string password) UploadFiles(List<IFormFile> files, int id, string[] tags, bool secureByPassword);

                StorageFileTable? GetFiles(int id);

                (IList<string>? links, string password) UpdateFilesAsync(DateModel dateModel);

                bool IsPasswordValid(int id, string password);

                string CreateFileUrl(string fileId, string fileName);

                IList<FileResponce> UpdateFilesLinks(StorageFileTable? entity);

                bool IsIdOccupied(int id);
        }
}
