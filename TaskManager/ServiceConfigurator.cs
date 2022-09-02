using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;

namespace TaskManager
{
    public static class ServiceConfigurator
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("SqlConnectionString");

            #region DbContext

            services.AddDbContext<EfContext>(config =>
            {
                config.UseSqlServer(connectionString, options =>
                {
                    options.EnableRetryOnFailure(maxRetryCount: 3,
                                                 maxRetryDelay: TimeSpan.FromSeconds(5),
                                                 errorNumbersToAdd: null);
                });
            });

            #endregion

            #region Auth

            services.AddIdentityEntityFrameworkContextConfiguration(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly("TaskManager"));
            });

            services.AddIdentityConfiguration();

            services.AddJwtConfiguration(configuration);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteTask", policy => policy.RequireClaim("DeleteTask"));
            });

            #endregion

            #region Swagger

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Task Manager with Minimal APIs",
                    Description = "Developed by João Pedro Fernandes",
                    Contact = new OpenApiContact { Name = "João Pedro", Email = "joao.fernandes.trabalho@outlook.com" }
                });

                setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Set the JWT token like this: Bearer {token}",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            #endregion

            return services;
        }
    }
}
