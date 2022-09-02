using TaskManager;

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;

builder.Services.ConfigureServices(configuration);

var app = builder.Build();

app.ConfigureApp()
   .MapEndpoints()
   .Run();