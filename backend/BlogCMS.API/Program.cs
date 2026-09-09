using BlogCMS.API.Data;
using BlogCMS.API.Middleware;
using BlogCMS.API.Models;
using BlogCMS.API.Repositories;
using BlogCMS.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Asp.Versioning;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace BlogCMS.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog for logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/blogcms-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting Blog CMS API");
                
                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog
                builder.Host.UseSerilog();

                // Add services to the container
                builder.Services.AddControllers();
                
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();

                // Configure API Versioning
                builder.Services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

                // ** UPDATED CORS CONFIGURATION - FIXED **
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend", policy =>
                    {
                        policy.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:5500",
                            "http://127.0.0.1:5500",
                            "http://localhost:5000",
                            "https://localhost:5001",
                            "http://localhost:8080",
                            "http://127.0.0.1:8080",
                            "null",  // For file:// protocol
                            "file://" // For local files
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed(origin => true) // Allow any origin in development
                        .WithExposedHeaders("Content-Disposition"); // Expose necessary headers
                    });
                });

                // Configure Entity Framework with SQLite
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

                // Configure Identity
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.User.RequireUniqueEmail = true;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

                // Configure JWT Authentication
                var jwtSettings = builder.Configuration.GetSection("Jwt");
                var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwtSettings["Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            
                            return Task.CompletedTask;
                        }
                    };
                });

                // Configure caching - Use in-memory cache
                builder.Services.AddDistributedMemoryCache();
                Log.Information("Using in-memory cache");

                // Register repositories and services
                builder.Services.AddScoped<IPostRepository, PostRepository>();
                builder.Services.AddScoped<IPostService, PostService>();
                builder.Services.AddScoped<IAuthService, AuthService>();

                // Configure HTTP Client
                builder.Services.AddHttpClient();

                // Configure Response Caching
                builder.Services.AddResponseCaching();

                // Configure Swagger/OpenAPI
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Blog CMS API",
                        Version = "v1",
                        Description = "A comprehensive blog content management system API",
                        Contact = new OpenApiContact
                        {
                            Name = "Admin",
                            Email = "admin@blogcms.com"
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT License"
                        }
                    });

                    // Configure JWT authentication in Swagger
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
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
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                var app = builder.Build();

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c => 
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blog CMS API v1");
                        c.RoutePrefix = "swagger";
                    });
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseHsts();
                    app.UseHttpsRedirection();
                }

                // Use custom exception handling middleware
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                // ** IMPORTANT: Use CORS before other middleware **
                app.UseCors("AllowFrontend");

                // Use response caching
                app.UseResponseCaching();

                // Use authentication and authorization
                app.UseAuthentication();
                app.UseAuthorization();

                // Map controllers
                app.MapControllers();

                // Seed database with initial data
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<ApplicationDbContext>();
                        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                        
                        await context.Database.EnsureCreatedAsync();
                        await DbInitializer.InitializeAsync(context, userManager, roleManager);
                        
                        Log.Information("Database seeded successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while seeding the database");
                    }
                }

                Log.Information("Blog CMS API started successfully");
                
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Blog CMS API terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}