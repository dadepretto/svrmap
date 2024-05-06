using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SvrMap.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<ServerSettings>().BindConfiguration("Settings");
builder.Services.AddHostedService<Server>();

var app = builder.Build();

await app.RunAsync();