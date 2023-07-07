
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using FileStorageApi.DataWrapper;
using FileStorageApi.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;

namespace FileStorageApi
{
        public class Program
        {
                public static void Main(string[] args)
                {



                        var builder = WebApplication.CreateBuilder(args);

                        // Add services to the container.

                        builder.Services.AddControllers();
                        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                        builder.Services.AddEndpointsApiExplorer();
                        builder.Services.AddSwaggerGen();
                        builder.Services.AddMemoryCache();
                        builder.Services.AddSingleton(x => new BlobContainerClient(builder.Configuration.GetConnectionString("AzureFileStorageConnectionString"), builder.Configuration.GetConnectionString("AzureBlobContainerName")));

                        builder.Services.AddSingleton(x => new TableServiceClient(builder.Configuration.GetConnectionString("AzureFileStorageConnectionString")));

                        builder.Services.AddSingleton(x => CloudStorageAccount.Parse(builder.Configuration.GetConnectionString("AzureFileStorageConnectionString")));

                        builder.Services.AddScoped<IFileRepository, AzureRepositoryWrapper>();

                        var app = builder.Build();

                        // Configure the HTTP request pipeline.
                        if (app.Environment.IsDevelopment())
                        {
                                app.UseSwagger();
                                app.UseSwaggerUI();
                        }

                        app.UseHttpsRedirection();

                        app.UseAuthorization();


                        app.MapControllers();

                        app.Run();
                }
        }
}