using EdcsClient.Model;
using EdcsClient.Service;
using EdcsClient.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;

namespace EdcsClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

            var loginWindow = ServiceProvider.GetRequiredService<Login>();
            if (loginWindow.ShowDialog() == true)
            {
                mainWindow.ShowDialog();
            }

        }
        private void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Settings>(Configuration.GetSection(nameof(Settings)));
            services.AddTransient<IAuthenticationHelper, AuthenticationHelper>();
            services.AddScoped<IDbService, DbService>();
            services.AddSingleton<IRabbitService, RabbitService>();
            services.AddTransient(typeof(MainWindow));
            services.AddTransient(typeof(Login));
        }

    }
}
