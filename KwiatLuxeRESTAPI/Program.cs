using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.BackgroundJobs;
using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.FileManagement;
using KwiatLuxeRESTAPI.Services.Logger;
using KwiatLuxeRESTAPI.Services.Security.Password;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace KwiatLuxeRESTAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Custom Logger Settings
            var debugLoggerSettings = builder.Configuration["CustomLogger:DebugOutput"];
            if (debugLoggerSettings != null) Logger.setDebugOutput(bool.Parse(debugLoggerSettings));
            var consoleLoggerSettings = builder.Configuration["CustomLogger:ConsoleOutput"];
            if (consoleLoggerSettings != null) Logger.setConsoleOutput(bool.Parse(consoleLoggerSettings));

            //API Options Configuration
            var cookieAppSettings = builder.Configuration["APIOptions:USE_COOKIES"];
            if (cookieAppSettings != null) SetAPIOptions.USE_COOKIES = bool.Parse(cookieAppSettings);
            Logger.Log(Severity.INFO, "Cookie usage is " + (SetAPIOptions.USE_COOKIES ? "enabled" : "disabled"));
            if (SetAPIOptions.USE_COOKIES)
            {
                var cookieNameAppSettings = builder.Configuration["APIOptions:COOKIE_NAME"];
                if (cookieNameAppSettings != null) SetAPIOptions.COOKIE_NAME = cookieNameAppSettings;
                Logger.Log(Severity.INFO, $"Cookie name is configured as: {SetAPIOptions.COOKIE_NAME}");
            }
            var hashAppSettings = builder.Configuration["APIOptions:SET_ITERATION_COUNT"];
            if (hashAppSettings != null) SetAPIOptions.SET_ITERATION_COUNT = int.Parse(hashAppSettings);
            Logger.Log(Severity.INFO, $"Password hashing iteration count set to {SetAPIOptions.SET_ITERATION_COUNT} " + (SetAPIOptions.SET_ITERATION_COUNT == 1 ? "iteration" : "iterations"));
            var saltAppSettings = builder.Configuration["APIOptions:SET_SALT_BIT_SIZE"];
            if (saltAppSettings != null) SetAPIOptions.SET_SALT_BIT_SIZE = int.Parse(saltAppSettings);
            Logger.Log(Severity.INFO, $"Password salt bit size set to {SetAPIOptions.SET_SALT_BIT_SIZE} " + (SetAPIOptions.SET_SALT_BIT_SIZE == 1 ? "bit" : "bits"));
            var roleAppSettings = builder.Configuration["APIOptions:DEFAULT_ROLE"];
            if (roleAppSettings != null) SetAPIOptions.DEFAULT_ROLE = roleAppSettings;
            Logger.Log(Severity.INFO, $"Default user role is configured as: {SetAPIOptions.DEFAULT_ROLE}");
            var specialRoleAppSettings = builder.Configuration["APIOptions:SET_SPECIAL_ROLE"];
            if (specialRoleAppSettings != null) SetAPIOptions.SET_SPECIAL_ROLE = specialRoleAppSettings;
            Logger.Log(Severity.INFO, $"Special user role is configured as: {SetAPIOptions.SET_SPECIAL_ROLE}");

            // Add services to the container.
            builder.Services.AddControllers();

            //use memory caching
            builder.Services.AddMemoryCache();

            //swagger configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KwiatLuxe API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });
            //end configuration for swagger

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<KwiatLuxeDb>(opt => opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var result = JsonConvert.SerializeObject(new { error = "Authentication failed! Try again..." });
                            Logger.Log(Severity.ERROR, "Authentication failed: " + context.Exception.Message);
                            return context.Response.WriteAsync(result);
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Logger.Log(Severity.INFO, "Token Validated");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies[SetAPIOptions.COOKIE_NAME];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var result = JsonConvert.SerializeObject(new { error = "You are not authenticated!" });
                            return context.Response.WriteAsync(result);
                        }
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            var result = JsonConvert.SerializeObject(new { error = "You don't have access to this content" });
                            Logger.Log(Severity.ERROR, "You lack the privileges to access this content");
                            return context.Response.WriteAsync(result);
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            if (SetAPIOptions.USE_COOKIES)
            {
                builder.Services.Configure<CookiePolicyOptions>(options =>
                {
                    options.CheckConsentNeeded = context => true;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });

                builder.Services.ConfigureApplicationCookie(options =>
                {
                    // Cookie settings
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                });
            }

            // Cors Settings
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Channels
            builder.Services.AddSingleton(_ =>
            {
                var registerChannel = Channel.CreateBounded<UserRegisterJob>(new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });
                return registerChannel;
            });

            builder.Services.AddSingleton(_ =>
            {
                var uploadChannel = Channel.CreateBounded<ImageUploadJob>(new BoundedChannelOptions(100)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });
                return uploadChannel;
            });

            //Singletons
            builder.Services.AddSingleton(pass => 
            {
                Password _passwordService = new Password();
                return _passwordService;
            });

            builder.Services.AddSingleton(user =>
            {
                UserInformation _userInformationService = new();
                return _userInformationService;
            });

            builder.Services.AddSingleton(user =>
            {
                ImageFileService _imageUploadService = new();
                return _imageUploadService;
            });

            //Concurrent Dictionary
            builder.Services.AddSingleton<ConcurrentDictionary<string, BackgroundJobStatus>>();

            //Background Services
            builder.Services.AddHostedService<UserBackgroundService>();
            builder.Services.AddHostedService<ImageUploadBackgroundService>();

            //Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RefreshToken", policy => policy.RequireClaim("Purpose", "RefreshToken"));
                options.AddPolicy("AccessToken", policy => policy.RequireClaim("Purpose", "AccessToken"));
            });

            var app = builder.Build();

            // Cors middleware
            app.UseCors("AllowAll");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Middleware for authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpsRedirection();

            app.MapControllers();

            app.Run();
        }
    }

    public static class SetAPIOptions
    {
        //defaults
        public static bool USE_COOKIES = false;
        public static string COOKIE_NAME = "Identity";
        public static int SET_ITERATION_COUNT = 100000;
        public static int SET_SALT_BIT_SIZE = 256;
        public static string DEFAULT_ROLE = "Customer";
        public static string SET_SPECIAL_ROLE = "Admin";
    }
}
