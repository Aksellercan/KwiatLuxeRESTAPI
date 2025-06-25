using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Security.Password;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace KwiatLuxeRESTAPI.Services.BackgroundJobs
{
    public class PasswordBackgroundService : BackgroundService
    {
        private Password _passwordHasher;
        private ConcurrentDictionary<string, PasswordHasherEnum.Status> _jobStatus;
        private Channel<PasswordHasherJob> _jobChannel;
        private ILogger<PasswordBackgroundService> _logger;

        public PasswordBackgroundService(Password passwordHasher, Channel<PasswordHasherJob> jobChannel, 
            ConcurrentDictionary<string, PasswordHasherEnum.Status> jobStatus, ILogger<PasswordBackgroundService> logger) 
        {
            _passwordHasher = passwordHasher;
            _jobChannel = jobChannel;
            _jobStatus = jobStatus;
            _logger = logger;

        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("at ExecuteAsync");
            while (!cancellationToken.IsCancellationRequested) 
            {
                _logger.LogInformation("passed while loop");

                await foreach (var job in _jobChannel.Reader.ReadAllAsync(cancellationToken))
                    try
                    {
                        _logger.LogInformation("in try catch");
                        _jobStatus[job.Id] = PasswordHasherEnum.Status.Processing;
                        byte[] salt = _passwordHasher.createSalt();
                        _logger.LogInformation("salt created");
                        string? hashed = _passwordHasher.HashPassword(job.Input, salt);
                        if (hashed != null)
                        {
                            _logger.LogInformation("success");
                            _jobStatus[job.Id] = PasswordHasherEnum.Status.Completed;
                        }
                        _logger.LogInformation($"info: {_jobStatus[job.Id]} and id {job.Id}, hash {hashed}");
                        //var user = new User
                        //{
                        //    Username = userRegister.Username,
                        //    Password = _passwordService.HashPassword(userRegister.Password, salt),
                        //    Salt = Convert.ToBase64String(salt),
                        //    Role = SetAPIOptions.DEFAULT_ROLE,
                        //    Email = userRegister.Email
                        //};
                        //_db.Users.Add(user);
                        //await _db.SaveChangesAsync();
                    }
                    catch (OperationCanceledException cancelErr) 
                    {
                        _logger.LogInformation(cancelErr, $"Job cancelled for Job ID {job.Id}");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error with Job ID {job.Id}");
                        _jobStatus[job.Id] = PasswordHasherEnum.Status.Failed;
                    }
            }
        }
    }
}
