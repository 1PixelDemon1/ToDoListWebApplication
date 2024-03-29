using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskManager.Api.Config;
using TaskManager.Api.ResponseProvider;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // CORS options.
            var allowedOrigins = "AllowedOrigins";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: allowedOrigins,
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:3000", "http://localhost:4200", "http://localhost:8080", "http://localhost:80", "http://localhost").AllowAnyMethod().AllowAnyHeader();
                    });
            });
            // Allows us to pass JWT in swagger.
            builder.Services.AddSwaggerGen(option =>
            {
                option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference= new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id=JwtBearerDefaults.AuthenticationScheme
                            }
                        }, new string[]{}
                    }
                });
            });

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddAutoMapper(typeof(MapperProfile));
            builder.Services.AddScoped<IResponseProvider, ResponseProvider.ResponseProvider>();

            var jwtOptions = builder.Configuration.GetSection("JwtOptions");

            // JWT options.
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions["Issuer"],
                    ValidAudience = jwtOptions["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions["Secret"]))
                };
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();

            app.UseCors(allowedOrigins);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            ApplyMigration(app);

            app.Run();
        }
        
        public static void ApplyMigration(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<TaskManagerDbContext>();

                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
        }
    }
}
