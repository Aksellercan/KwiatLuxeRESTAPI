using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Security.Password;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace KwiatLuxeRESTAPI.Services.BackgroundJobs
{
    public class UserBackgroundService(Channel<UserRegisterJob> jobChannel, ConcurrentDictionary<string, BackgroundJobStatus> jobStatus,
        ILogger<UserBackgroundService> logger, IServiceProvider serviceProvider, Password password) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (var job in jobChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        jobStatus[job.Id] = BackgroundJobStatus.Processing;
                        await RegisterUser(job.UserRegisterDto);
                        jobStatus[job.Id] = BackgroundJobStatus.Completed;
                        logger.LogInformation($"info: {jobStatus[job.Id]} and id {job.Id}");
                    }
                    catch (OperationCanceledException cancelErr)
                    {
                        logger.LogWarning(cancelErr, $"Job cancelled for Job ID {job.Id}");
                        break;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Error with Job ID {job.Id}");
                        jobStatus[job.Id] = BackgroundJobStatus.Failed;
                    }
                }
            }
        }

        private async Task RegisterUser(UserRegisterDTO userRegisterDTO)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var dbcontext = scope.ServiceProvider.GetRequiredService<KwiatLuxeDb>();
                byte[] salt = password.CreateSalt();
                var user = new User
                {
                    Username = userRegisterDTO.Username,
                    Password = password.HashPassword(userRegisterDTO.Password, salt),
                    Salt = Convert.ToBase64String(salt),
                    Role = SetApiOptions.DefaultRole,
                    Email = userRegisterDTO.Email
                };
                dbcontext.Set<User>().Add(user);
                await dbcontext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Error when registering. {e}");
            }
        }
    }
}
