using Confluent.Kafka;
using GraphQL.AspNet.Configuration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using PlanningService.Database;
using PlanningService.Kafka;
using Serilog;
using Serilog.Events;

public class Program
{
    private const string DbConnectionStringName = "Database";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureBuilder(builder);

        var app = builder.Build();
        InitializeDatabase(app);
        ConfigureApplication(app);

        app.Run();
    }

    private static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        ConfigureApplication(builder);
        ConfigureHttpClients(builder);
        ConfigureLogging(builder);
        ConfigureKafka(builder);
        ConfigureOpenApi(builder);
        ConfigureDatabase(builder);
        ConfigureMetrics(builder);
    }

    private static void ConfigureApplication(WebApplicationBuilder builder)
    {
        builder.Services.AddGraphQL();
        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
    }

    private static void ConfigureHttpClients(WebApplicationBuilder builder) {
        builder.Services.AddHeaderPropagation(options => {
            options.Headers.Add("X-Correlation-Id");
        });
    }


    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) => {
            config.ReadFrom.Configuration(builder.Configuration)
                .Enrich.WithCorrelationIdHeader("X-Correlation-Id");
        });
    }

    private static void ConfigureOpenApi(WebApplicationBuilder builder)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        string? connectionString =
            builder.Configuration.GetConnectionString(DbConnectionStringName);

        builder.Services.AddDbContext<PlanningDbContext>(options => {
            if (builder.Environment.IsDevelopment()) {
                options.UseSqlite(connectionString);
            }
            else {
                options.UseNpgsql(connectionString);
            }
        });
    }

    private static void ConfigureKafka(WebApplicationBuilder builder)
    {
        string? kafkaUrl = builder.Configuration["KAFKA_URL"];
        builder.Services.AddKafkaClient()
            .Configure(options => {
                options.Configure(new ProducerConfig {
                    BootstrapServers = kafkaUrl
                }).Serialize(new JsonMessageSerializer<KafkaMessage>())
                  .Deserialize(new JsonMessageSerializer<KafkaMessage>());
            });
    }

    private static void ConfigureMetrics(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(builder => {
                builder.AddPrometheusExporter();

                builder.AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel");
            });
    }

    private static void InitializeDatabase(WebApplication app)
    {
        IServiceScopeFactory scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        using IServiceScope scope = scopeFactory.CreateScope();
        DbContext dbContext = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();
        ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database set to connection string {0}",
                              app.Configuration.GetConnectionString(DbConnectionStringName));
        logger.LogInformation("Ensuring database is created.");
        bool wasCreated = dbContext.Database.EnsureCreated();
        if (wasCreated) {
            logger.LogInformation("Database was created.");
        }
    }

    private static void ConfigureApplication(WebApplication app)
    {
        string app_base = app.Configuration["APP_BASE"] ?? "/";
        app.UsePathBase(app_base);
        app.UseRouting();

        ConfigureSwaggerUI(app);

        app.MapPrometheusScrapingEndpoint();
        app.UseHeaderPropagation();

        ConfigureWebApplication(app);
        ConfigureApplicationLogging(app);
    }

    private static void ConfigureSwaggerUI(WebApplication app)
    {
        string app_base = app.Configuration["APP_BASE"] ?? "/";
        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"{app_base}/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = "openapi";
            options.DocumentTitle = "OpenAPI documentation";
        });
    }

    private static void ConfigureWebApplication(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExceptionHandler(a => a.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            if (exception is not null) {
                context.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>()
                    .LogError(exception, "An error has occurred while processing request");
            }
           
            await context.Response.WriteAsJsonAsync(new { error = "An error has occurred while processing request" });
        }));
        app.UseGraphQL();
        app.MapControllers();
    }

    private static void ConfigureApplicationLogging(WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Customize the message template
            options.MessageTemplate = "Handled {RequestPath}";

            // Emit debug-level events instead of the defaults
            options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;

            // Attach additional properties to the request completion event
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });
    }
}
