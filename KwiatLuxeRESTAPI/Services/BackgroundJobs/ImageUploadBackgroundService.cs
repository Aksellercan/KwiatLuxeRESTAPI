using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.FileManagement;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace KwiatLuxeRESTAPI.Services.BackgroundJobs
{
    public class ImageUploadBackgroundService(Channel<ImageUploadJob> uploadChannel, ConcurrentDictionary<string, BackgroundJobStatus> uploadStatus,
        ILogger<ImageUploadBackgroundService> logger, IServiceProvider serviceProvider, ImageFileService imageFileService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (var job in uploadChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        uploadStatus[job.Id] = BackgroundJobStatus.Processing;
                        string fileName = await imageFileService.FileUpload(job.FileUpload);
                        if (job.ProductDto == null) throw new NullReferenceException("Product Data is null");
                        await CreateProduct(fileName, job.ProductDto);
                        uploadStatus[job.Id] = BackgroundJobStatus.Completed;
                        logger.LogInformation($"info: {uploadStatus[job.Id]} and id {job.Id}");
                    }
                    catch (OperationCanceledException cancelErr)
                    {
                        logger.LogWarning(cancelErr, $"Job cancelled for Job ID {job.Id}");
                        break;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Error with Job ID {job.Id}");
                        uploadStatus[job.Id] = BackgroundJobStatus.Failed;
                    }
                }
            }
        }

        private async Task CreateProduct(string fileName, ProductDTO productDto)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<KwiatLuxeDb>();

            var product = new Product
            {
                ProductName = productDto.ProductName,
                ProductDescription = productDto.ProductDescription,
                ProductPrice = productDto.ProductPrice,
                FileImageUrl = fileName
            };
            dbcontext.Set<Product>().Add(product);
            await dbcontext.SaveChangesAsync();
        }
    }
}
