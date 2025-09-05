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

// Add memory cache
builder.Services.AddMemoryCache();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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
builder.Services.AddScoped<IProtocolHandlerFactory, ProtocolHandlerFactory>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbConnection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await DatabaseInitializer.InitializeAsync(dbConnection);
}

// 自动启动配置的TCP服务器
try
{
    using var scope = app.Services.CreateScope();
    var connectionService = scope.ServiceProvider.GetRequiredService<IConnectionService>();
    var tcpServer = app.Services.GetRequiredService<TcpServer>();
    
    var connections = await connectionService.GetConnectionsTreeAsync();
    var portToConnectionId = connections.ToDictionary(c => c.Port, c => c.Id);
    
    if (portToConnectionId.Count > 0)
    {
        await tcpServer.StartAsync(portToConnectionId);
        Console.WriteLine($"TCP服务器已启动，监听端口：{string.Join(", ", portToConnectionId.Keys)}");
    }
    else
    {
        Console.WriteLine("未找到配置的连接，跳过TCP服务器启动");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"启动TCP服务器时发生错误：{ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseRouting();
app.MapControllers();

await app.RunAsync();