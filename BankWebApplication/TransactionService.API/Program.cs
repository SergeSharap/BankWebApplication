using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionService.Application.Services;
using TransactionService.Application.Validators;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Data;
using TransactionService.Infrastructure.Repositories;
using TransactionService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add Entity Framework with SQL Server
builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<TransactionRequestValidator>();

// Add Domain Services
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IClientService, ClientService>();

// Add Application Services
builder.Services.AddScoped<ITransactionApplicationService, TransactionApplicationService>();

// Add TimeProvider
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

// Add console logging
builder.Logging.AddConsole();

var app = builder.Build();

// Add middleware in correct order
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ValidationExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
