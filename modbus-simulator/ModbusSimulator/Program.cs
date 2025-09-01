using Microsoft.Data.Sqlite;
using ModbusSimulator.Repositories;
using ModbusSimulator.Services;
using ModbusSimulator.Tcp;
using ModbusSimulator.Data;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new SqliteConnection(connectionString);
});

builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<ISlaveRepository, SlaveRepository>();
builder.Services.AddScoped<IRegisterRepository, RegisterRepository>();

builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<ISlaveService, SlaveService>();
builder.Services.AddScoped<IRegisterService, RegisterService>();

builder.Services.AddSingleton<TcpServer>();
builder.Services.AddSingleton<IProtocolHandler, ModbusTcpService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbConnection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await DatabaseInitializer.InitializeAsync(dbConnection);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

await app.RunAsync();