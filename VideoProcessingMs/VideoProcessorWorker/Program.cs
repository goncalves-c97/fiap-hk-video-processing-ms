using Amazon.S3;
using Core.Entities;
using Core.Gateways;
using Core.Handlers;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Gateways;
using Dapper;
using Infra.Data.SqlServer;
using Infra.ObjectStorageService;
using RabbitMQ.Client;
using VideoProcessorWorker;

var builder = Host.CreateApplicationBuilder(args);

// -----------------------------
// Configurações
// -----------------------------
var configuration = builder.Configuration;

// -----------------------------
// Banco de dados
// -----------------------------
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var rawConnectionString = config["DB_CONNECTION_STRING"];
    var databaseName = config["DB_NAME"];

    if (string.IsNullOrWhiteSpace(rawConnectionString))
        throw new KeyNotFoundException("Chave 'DB_CONNECTION_STRING' não encontrada.");

    if (string.IsNullOrEmpty(databaseName))
        throw new KeyNotFoundException("Chave 'DB_NAME' não encontrada.");

    // Keep existing initializer call for compatibility.
    DatabaseInitializer.EnsureDatabaseExists(rawConnectionString, databaseName);

    // Dapper type mappings
    SqlMapper.SetTypeMap(typeof(VideoUpload), new SnakeCaseTypeMapper<VideoUpload>());

    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(rawConnectionString)
    {
        InitialCatalog = databaseName
    };

    var finalConnectionString = builder.ToString();

    return new SqlServerConnection(finalConnectionString);
});


builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = builder.Configuration.GetSection("OBJ_STORAGE");

    var s3Config = new AmazonS3Config
    {
        ServiceURL = config["SERVICE_URL"],
        ForcePathStyle = config["FORCE_PATH_STYLE"]?.ToLower() == "true"
    };

    return new AmazonS3Client(
        config["ACCESS_KEY"],
        config["SECRET_KEY"],
        s3Config);
});

builder.Services.AddScoped<IObjectStorageService, S3ObjectStorageService>();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory()
    {
        HostName = builder.Configuration["MESSAGING:HOST"],
        UserName = builder.Configuration["MESSAGING:USER"],
        Password = builder.Configuration["MESSAGING:PASSWORD"]
    };

    return factory.CreateConnection();
});

builder.Services.AddScoped<IMessagingService, RabbitMqEventBus>();

builder.Services.AddSingleton<IVideoProcessingHandler, VideoProcessingHandler>();

builder.Services.AddSingleton<IVideoProcessingGateway, VideoProcessingGateway>();

builder.Services.AddSingleton<IFrameExtractor, FfmpegFrameExtractor>();

builder.Configuration.AddEnvironmentVariables();

// -----------------------------
// Worker
// -----------------------------
builder.Services.AddHostedService<VideoProcessingWorker>();

// -----------------------------
var host = builder.Build();

host.Run();