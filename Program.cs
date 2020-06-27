using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using EdcsServer.Service.Background;
using Microsoft.Extensions.Configuration;
using EdcsServer.Service;
using EdcsServer.Helper;

namespace EdcsServer
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IRabbitService, RabbitService>();
                services.AddSingleton<IDbService, DbService>();
                services.AddTransient<IModelHelper, ModelHelper>();
                services.AddHostedService<Listener>();
            })
            .ConfigureHostConfiguration(configHost => {
                configHost.AddJsonFile("appsettings.json", optional: false);
            });

    }
}
