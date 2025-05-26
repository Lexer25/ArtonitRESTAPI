using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenAPIArtonit.DB;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using OpenAPIArtonit.Legasy_Service;
using FluentValidation.AspNetCore;
using ArtonitRESTAPI.APIControllers;
using System.IO;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Default", LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File(
        path: "C:\\ArtonitRestApi\\log\\log-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true)
    .CreateLogger();

Log.Information("Start Artonit Rest API");

var builder = WebApplication.CreateBuilder(args);

Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"Serilog: {msg}"));

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);
});



builder.Services.AddWindowsService();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(c =>
{
    c.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    c.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(c =>
{
    c.SaveToken = true;
    c.RequireHttpsMetadata = true;
    c.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this is my custom Secret key for authentication"))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
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
            new string[] {}
        }
    });
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Artonit Rest API", Version = "1.0.6" });
    c.EnableAnnotations();
    c.ExampleFilters();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
    options.SuppressMapClientErrors = true;
});

builder.Services.AddControllers()
    .AddFluentValidation(fv =>
    {
        fv.RegisterValidatorsFromAssemblyContaining<IdentifierPostSeeValidator>();
        fv.AutomaticValidationEnabled = true;
    });

// Регистрация сервисов
builder.Services.AddSingleton<SettingsService>();

var urls = builder.Configuration["Urls"];
builder.WebHost.ConfigureKestrel(options =>
{
    options.Configure(builder.Configuration.GetSection("Kestrel"));
});

var app = builder.Build();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var shutdownLogger = new LoggerConfiguration()
        .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "C:\\ArtonitRestApi\\log\\log-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            shared: true)
        .CreateLogger();
    shutdownLogger.Information("End Artonit Rest API");
    shutdownLogger.Dispose();
});

// Инициализация DatabaseService
using (var scope = app.Services.CreateScope())
{
    DatabaseService.Initialize(scope.ServiceProvider);
}


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();