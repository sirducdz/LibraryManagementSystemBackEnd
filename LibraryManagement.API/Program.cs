using FluentValidation;
using FluentValidation.AspNetCore;
using LibraryManagement.API.Configuration;
using LibraryManagement.API.Data;
using LibraryManagement.API.Data.Repositories.Implementations;
using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Helpers;
using LibraryManagement.API.Services.Implementations;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

namespace LibraryManagement.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors();
            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<LibraryDbContext>(options =>
                options.UseSqlServer(connectionString).UseLazyLoadingProxies());
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddSingleton<PasswordHasher>();
            builder.Services.AddControllers();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            builder.Services.AddEndpointsApiExplorer();
            var jwtSettings = new JwtSettings();
            builder.Configuration.Bind("JwtAppsettings", jwtSettings); // Hoặc tên section bạn đặt
            builder.Services.AddSingleton(jwtSettings); // Dùng Singleton vì JwtSettings không thay đổi khi chạy

            var secretKey = builder.Configuration["JwtAppsettings:SecretKey"];
            var secretKeyByte = Encoding.UTF8.GetBytes(secretKey);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidAudience = builder.Configuration["JwtAppsettings:Audience"],
                    ValidIssuer = builder.Configuration["JwtAppsettings:Issuer"],
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKeyByte),
                    ClockSkew = TimeSpan.Zero
                };
            });
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: ",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                            In = ParameterLocation.Header
                        },
                        new string[] {}
                    }
                });
            });
            var app = builder.Build();
            app.UseCors(option => option.AllowAnyHeader().
              AllowAnyMethod().AllowAnyOrigin());
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
