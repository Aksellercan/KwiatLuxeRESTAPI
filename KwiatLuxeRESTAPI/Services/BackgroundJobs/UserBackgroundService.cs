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
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private Password _password = password;
        private ConcurrentDictionary<string, BackgroundJobStatus> _jobStatus = jobStatus;
        private Channel<UserRegisterJob> _jobChannel = jobChannel;
        private ILogger<UserBackgroundService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (var job in _jobChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        _jobStatus[job.Id] = BackgroundJobStatus.Processing;
                        await RegisterUser(job.UserRegisterDto);
                        _jobStatus[job.Id] = BackgroundJobStatus.Completed;
                        _logger.LogInformation($"info: {_jobStatus[job.Id]} and id {job.Id}");
                    }
                    catch (OperationCanceledException cancelErr)
                    {
                        _logger.LogWarning(cancelErr, $"Job cancelled for Job ID {job.Id}");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error with Job ID {job.Id}");
                        _jobStatus[job.Id] = BackgroundJobStatus.Failed;
                    }
                }
            }
        }

        public async Task RegisterUser(UserRegisterDTO userRegisterDTO)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbcontext = scope.ServiceProvider.GetRequiredService<KwiatLuxeDb>();
                byte[] salt = _password.createSalt();
                var user = new User
                {
                    Username = userRegisterDTO.Username,
                    Password = _password.HashPassword(userRegisterDTO.Password, salt),
                    Salt = Convert.ToBase64String(salt),
                    Role = SetApiOptions.DefaultRole,
                    Email = userRegisterDTO.Email
                };
                dbcontext.Set<User>().Add(user);
                await dbcontext.SaveChangesAsync();
                return;
            }
            catch (Exception e)
            {
                throw new Exception($"Error when registering. {e}");
            }
        }
    }
}
