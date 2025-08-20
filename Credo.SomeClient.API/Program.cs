using Credo.Core.Minio.DI;
using Credo.Core.FileStorage;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();


builder.Services.AddFileStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
    new CredoMinioStorageConfiguration
    {
        Endpoint = "s3.minio.credo.ge",
        AccessKey = "minioadmin",
        SecretKey = "securepassword123!"
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();