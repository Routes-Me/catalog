using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APIGateway.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace APIGateway
{
    public class Startup
    {
        public IConfiguration _configuration { get; }
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // configure strongly typed settings objects
            var appSettingsSection = _configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            JwtBearerEvents jwtBearerEvents = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    logger.LogError("Authentication failed.", context.Exception);
                    context.Response.StatusCode = 401;
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    context.Response.OnStarting(async () =>
                    {
                        context.Response.ContentType = "application/json";
                        ErrorMessage errorMessage = new ErrorMessage();
                        errorMessage.Message = "Authentication failed.";
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(errorMessage));
                    });
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    logger.LogError("OnChallenge error", context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                }
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer("clientSecretKey", x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = appSettings.ValidAudience,
                    ValidIssuer = appSettings.ValidIssuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Secret)),
                    // verify signature to avoid tampering
                    ValidateLifetime = true, // validate the expiration
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(5) // tolerance for the expiration date
                };
                x.Events = jwtBearerEvents;
            }).AddJwtBearer("accessToken", x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudiences = new List<string>
                    {
                        "https://dashboard.routesme.com",
                        "https://screen.routesme.com",
                        "https://app.routesme.com"
                    },
                    ValidIssuer = appSettings.SessionTokenIssuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.AccessSecretKey)),
                    // verify signature to avoid tampering
                    ValidateLifetime = true, // validate the expiration
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(5) // tolerance for the expiration date
                };
                x.Events = jwtBearerEvents;
            });

            services.AddOcelot(_configuration);
            //services.AddHttpsRedirection(options =>
            //{
            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            //    options.HttpsPort = 443;
            //});

            //services.AddHsts(options =>
            //{
            //    options.Preload = false;
            //    options.IncludeSubDomains = false;
            //    options.MaxAge = TimeSpan.FromDays(30);
            //});

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //else
            //{
            //    app.UseHsts();
            //}

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors("MyPolicy");
            await app.UseOcelot();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
