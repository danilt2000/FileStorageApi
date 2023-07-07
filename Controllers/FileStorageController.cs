using FileStorageApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Azure.Storage.Blobs;
using System.IO;
using System;
using Azure.Storage.Blobs.Models;
using System.IO.Pipes;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FileStorageApi.Interfaces;
using System.Reflection;
using Azure;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;

namespace FileStorageApi.Controllers
{
        [ApiController]
        [Route("[controller]")]
        public class FileStorageController : ControllerBase
        {
                private IFileRepository FileRepository;

                private IMemoryCache MemoryCache;

                public FileStorageController(IFileRepository fileRepository, IMemoryCache memoryCache)
                {
                        FileRepository = fileRepository;

                        MemoryCache = memoryCache;
                }

                [HttpPost("AddFiles")]
                public IActionResult Post([FromForm] DateModel data)
                {

                        if (FileRepository.IsIdOccupied(data.Id))
                        {
                                return BadRequest("This ID is not available please try another one");
                        }

                        var uploudResult = FileRepository.UploadFiles(data.MainDateModel.Files, data.Id, data.Tags, data.MainDateModel.SecureByPassword ?? false);

                        IList<FileResponce> fileResponseUrls = data.MainDateModel.Files
                                .Select((file, index) => new FileResponce
                                {
                                        FileName = file.FileName,
                                        FileLink = uploudResult.links[index]
                                }).ToList();

                        var response = new RootResponce
                        {
                                Status = "Success",
                                Password = uploudResult.password,
                                ResponceDateModel = new ResponceDateModel
                                {
                                        Id = data.Id,
                                        FileResponce = fileResponseUrls
                                }
                        };

                        return Ok(response);

                }

                [HttpGet("GetData")]
                public IActionResult Get([FromQuery] int id, [FromQuery] string? password)
                {
                        if (!FileRepository.IsPasswordValid(id, password))
                        {
                                return BadRequest("Not the correct id or password ");
                        }

                        if (FileRepository.IsIdOccupied(id))
                        {
                                var entity = FileRepository.GetFiles(id);

                                if (FileRepository.IsPasswordValid(id, password))
                                {
                                        IList<FileResponce> fileResponsesUrls = FileRepository.UpdateFilesLinks(entity);

                                        var response = new RootResponce
                                        {
                                                Status = "Success",

                                                Password = entity.Password,

                                                ResponceDateModel = new ResponceDateModel
                                                {
                                                        Id = id,
                                                        FileResponce = fileResponsesUrls
                                                }
                                        };

                                        return Ok(response);
                                }
                        }

                        return BadRequest("Not the correct id or password ");
                }
                [HttpPut("UpdateData")]
                public IActionResult Put([FromForm] UpdatedDateModel data)
                {
                        if (FileRepository.IsIdOccupied(data.Id))
                        {
                                if (FileRepository.IsPasswordValid(data.Id, data.Password))
                                {
                                        var result = FileRepository.UpdateFilesAsync(data);

                                        IList<FileResponce> fileResponseUrls = data.MainDateModel.Files
                                         .Select((file, index) => new FileResponce
                                         {
                                                 FileName = file.FileName,
                                                 FileLink = result.links[index]
                                         }).ToList();

                                        var response = new RootResponce
                                        {
                                                Status = "Success",
                                                Password = result.password,
                                                ResponceDateModel = new ResponceDateModel
                                                {
                                                        Id = data.Id,
                                                        FileResponce = fileResponseUrls
                                                }
                                        };

                                        return Ok(response);
                                }

                                return BadRequest("Not correct id or password ");
                        }

                        return BadRequest("Not the correct id or password ");
                }

        }
}
