using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading.Channels;

using KwiatLuxeRESTAPI.Services.Security.Authorization;
using KwiatLuxeRESTAPI.Services.Security.Password;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace KwiatLuxeRESTAPI.Services.BackgroundJobs
{
    public class UserBackgroundService(Channel<UserDetailsJob> jobChannel, ConcurrentDictionary<string, BackgroundJobStatus> jobStatus,
        ILogger<UserBackgroundService> logger, UserInformation userInformation, IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private ConcurrentDictionary<string, BackgroundJobStatus> _jobStatus = jobStatus;
        private Channel<UserDetailsJob> _jobChannel = jobChannel;
        private ILogger<UserBackgroundService> _logger = logger;
        private UserInformation _userInformation = userInformation;

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
                        _jobStatus[job.Id] = BackgroundJobStatus.Processing;
                        _logger.LogInformation("salt created");
                        UserDTO? testObject = await GetUserDetails(job.UserId);
                        if (testObject != null)
                        {
                            _logger.LogInformation("success");
                            _jobStatus[job.Id] = BackgroundJobStatus.Completed;
                        }
                        else
                        {
                            throw new Exception("User not found");
                        }
                        _logger.LogInformation($"info: {_jobStatus[job.Id]} and id {job.Id}, Data: {testObject.Username}");
                    }
                    catch (OperationCanceledException cancelErr)
                    {
                        _logger.LogInformation(cancelErr, $"Job cancelled for Job ID {job.Id}");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error with Job ID {job.Id}");
                        _jobStatus[job.Id] = BackgroundJobStatus.Failed;
                    }
            }
        }

        public async Task<UserDTO?> GetUserDetails(int userId)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbcontext = scope.ServiceProvider.GetRequiredService<KwiatLuxeDb>();
            var user = await dbcontext.Set<User>().Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role
            }).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;
            UserDTO userCache = new()
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };
            return userCache;
        }
    }
}
