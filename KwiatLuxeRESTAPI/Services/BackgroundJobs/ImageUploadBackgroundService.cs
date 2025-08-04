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
        private ImageFileService _imageFileService = imageFileService;
        private Channel<ImageUploadJob> _uploadChannel = uploadChannel;
        private ConcurrentDictionary<string, BackgroundJobStatus> _uploadStatus = uploadStatus;
        private ILogger<ImageUploadBackgroundService> _logger = logger;
        private IServiceProvider _serviceProvider = serviceProvider;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (var job in _uploadChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        _uploadStatus[job.Id] = BackgroundJobStatus.Processing;
                        // string fileName = await ImageUpload(job.FileUpload);
                        string fileName = await _imageFileService.FileUpload(job.FileUpload);
                        if (job.ProductDto == null) throw new NullReferenceException("Product Data is null");
                        await CreateProduct(fileName, job.ProductDto);
                        _uploadStatus[job.Id] = BackgroundJobStatus.Completed;
                        _logger.LogInformation($"info: {_uploadStatus[job.Id]} and id {job.Id}");
                    }
                    catch (OperationCanceledException cancelErr)
                    {
                        _logger.LogWarning(cancelErr, $"Job cancelled for Job ID {job.Id}");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error with Job ID {job.Id}");
                        _uploadStatus[job.Id] = BackgroundJobStatus.Failed;
                    }
                }
            }
        }

        private async Task<string> ImageUpload(IFormFile uploadedFile)
        {
            if (uploadedFile == null)
            {
                throw new NullReferenceException("File is null");
            }
            return await _imageFileService.FileUpload(uploadedFile);
        }

        private async Task CreateProduct(string fileName, ProductDTO productDto)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
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
            return;
        }
    }
}
