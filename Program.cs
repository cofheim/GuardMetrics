using GuardMetrics.Data;
using GuardMetrics.Jobs;
using GuardMetrics.Models;
using GuardMetrics.Services;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Основные сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настройка Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GuardMetrics API",
        Version = "v1",
        Description = "API для системы мониторинга безопасности",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@guardmetrics.com"
        }
    });

    // JWT аутентификация в Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true)
            .WithExposedHeaders("Token-Expired");
    });
});

// Настройка PostgreSQL с обработкой ошибок
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Warning("Строка подключения к PostgreSQL не настроена. Некоторые функции базы данных будут недоступны.");
        return;
    }

    try
    {
        // Проверка подключения
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        connection.Close();
        
        options.UseNpgsql(connectionString);
        Log.Information("Настроено подключение к PostgreSQL");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось подключиться к PostgreSQL. Некоторые функции базы данных будут недоступны.");
    }
});

// Настройка Redis с обработкой ошибок
ConfigureRedis();

// Настройка Hangfire с обработкой ошибок
ConfigureHangfire();

// Настройка JWT аутентификации
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? 
                throw new InvalidOperationException("JWT Secret not configured"))),
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }
            return Task.CompletedTask;
        }
    };
});

// Регистрация сервисов приложения
RegisterApplicationServices();

var app = builder.Build();

// Глобальная обработка ошибок
ConfigureErrorHandling(app);

// Настройка Swagger для разработки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GuardMetrics API V1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "GuardMetrics API Documentation";
        options.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");

// Поддержка статических файлов
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Настройка Hangfire Dashboard
ConfigureHangfireDashboard(app);

app.MapControllers();

// Применение миграций базы данных
ApplyDatabaseMigrations(app);

// Настройка периодических задач
ConfigureRecurringJobs(app);

// Запуск приложения
RunApplication(app);

// Методы настройки и запуска

void ConfigureRedis()
{
    try
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            Log.Warning("Строка подключения Redis не настроена. Кэширование будет недоступно.");
            ConfigureDummyRedis();
            return;
        }

        // Добавление параметра для отказоустойчивости
        if (!redisConnectionString.Contains("abortConnect=false"))
        {
            redisConnectionString += ",abortConnect=false";
        }
        
        try
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnectionString));
            Log.Information("Подключение к Redis успешно настроено");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Не удалось подключиться к Redis. Кэширование будет недоступно.");
            ConfigureDummyRedis();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось настроить подключение к Redis. Кэширование будет недоступно.");
        ConfigureDummyRedis();
    }
}

void ConfigureDummyRedis()
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
        ConnectionMultiplexer.Connect("127.0.0.1:0,abortConnect=false,allowAdmin=true"));
}

void ConfigureHangfire()
{
    try
    {
        var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection");
        if (string.IsNullOrEmpty(hangfireConnectionString))
        {
            Log.Warning("Строка подключения Hangfire не настроена. Фоновые задачи будут недоступны.");
            return;
        }

        try
        {
            // Проверка доступности базы данных
            using var connection = new NpgsqlConnection(hangfireConnectionString);
            connection.Open();
            connection.Close();
            
            // Настройка Hangfire
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => 
                {
                    options.UseNpgsqlConnection(hangfireConnectionString);
                }));
                
            // Запуск Hangfire Server в режиме разработки
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddHangfireServer(options =>
                {
                    options.WorkerCount = 1; // Уменьшаем количество workers
                });
            }
            
            Log.Information("Hangfire успешно настроен");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Не удалось подключиться к базе данных Hangfire. Фоновые задачи будут недоступны.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось настроить Hangfire. Фоновые задачи будут недоступны.");
    }
}

void RegisterApplicationServices()
{
    builder.Services.AddSingleton<AnalysisSettings>();
    builder.Services.AddSingleton<AnomalyDetectionService>();
    builder.Services.AddScoped<IMetricAnalyzer, MetricAnalyzer>();
    builder.Services.AddScoped<VirusTotalService>();
    builder.Services.AddScoped<TelegramNotificationService>();
    builder.Services.AddScoped<MetricAnalysisJob>();
}

void ConfigureErrorHandling(WebApplication app)
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            Log.Error(exception, "Необработанная ошибка");

            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = 500,
                Message = "Внутренняя ошибка сервера"
            });
        });
    });
}

void ConfigureHangfireDashboard(WebApplication app)
{
    try
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = app.Environment.IsDevelopment()
                ? new[] { new AllowAllConnectionsFilter() }
                : new[] { new HangfireAuthorizationFilter() }
        });
        Log.Information("Hangfire Dashboard успешно запущен");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось запустить Hangfire Dashboard. Проверьте подключение к базе данных.");
    }
}

void ApplyDatabaseMigrations(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        Log.Information("Миграции базы данных успешно применены");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось применить миграции. Проверьте подключение к базе данных.");
    }
}

void ConfigureRecurringJobs(WebApplication app)
{
    try
    {
        var recurringJobManager = app.Services.GetService<IRecurringJobManager>();
        
        if (recurringJobManager != null)
        {
            try
            {
                RecurringJob.AddOrUpdate<MetricAnalysisJob>(
                    "analyze-metrics",
                    job => job.AnalyzeLatestMetricsAsync(),
                    "*/15 * * * *"); // Каждые 15 минут
                    
                Log.Information("Периодические задачи Hangfire успешно настроены");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Не удалось добавить периодические задачи в Hangfire.");
            }
        }
        else
        {
            Log.Warning("Планировщик задач Hangfire недоступен. Периодические задачи не будут выполняться.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Не удалось настроить периодические задачи.");
    }
}

void RunApplication(WebApplication app)
{
    try
    {
        Log.Information("Запуск приложения");
        app.Run();
    }
    catch (Exception ex)
    {
        if (ex is PostgresException pgEx && pgEx.SqlState == "3D000")
        {
            Log.Warning("Невозможно подключиться к базе данных. Проверьте настройки подключения и убедитесь, что база данных существует.");
        }
        else
        {
            Log.Fatal(ex, "Приложение остановлено из-за ошибки");
        }
    }
    finally
    {
        Log.CloseAndFlush();
    }
}

// Классы для Hangfire Dashboard

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Разрешаем доступ в режиме разработки или если пользователь авторизован
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            return true;
        }
        
        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}

public class AllowAllConnectionsFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
