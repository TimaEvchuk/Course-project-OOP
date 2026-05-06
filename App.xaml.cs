using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Plantify.Data;
using Plantify.Data.Repositories;
using Plantify.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Windows;

namespace Plantify
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IHost? _host;

        public static IHost Host => _host ??= CreateHostBuilder(new string[] { }).Build();

        public static IServiceProvider Services => Host.Services;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
                    });

                    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                    services.AddScoped<IUnitOfWork, UnitOfWork>();

                    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                    
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<RegisterViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<EncyclopediaViewModel>();
                    services.AddTransient<PlantManagementViewModel>();

                    services.AddSingleton<MainWindow>();
                });

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host.StartAsync();

            // Seed the database
            using (var scope = Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    
                    // Ensure the database is created.
                    // Note: This is an alternative to `dotnet ef database update`
                    context.Database.EnsureCreated();

                    // Seed roles if they don't exist
                    if (!context.Roles.Any())
                    {
                        context.Roles.AddRange(
                            new Models.Role { Name = "Администратор" },
                            new Models.Role { Name = "Контент-менеджер" },
                            new Models.Role { Name = "Клиент" }
                        );
                        await context.SaveChangesAsync();
                    }

                    // Seed plants if they don't exist
                    if (!context.Plants.Any())
                    {
                        context.Plants.AddRange(
                            new Models.Plant { Name = "Snake Plant", Description = "Thrives on neglect, one of the easiest plants to keep alive.", WateringInterval = 21, LightRequirement = "Low to Bright Indirect", Difficulty = "Easy" },
                            new Models.Plant { Name = "ZZ Plant", Description = "An architectural plant with wide, attractive, dark green leaves.", WateringInterval = 21, LightRequirement = "Low to Bright Indirect", Difficulty = "Easy" },
                            new Models.Plant { Name = "Monstera Deliciosa", Description = "Famous for its quirky, natural leaf holes.", WateringInterval = 10, LightRequirement = "Bright Indirect", Difficulty = "Medium" },
                            new Models.Plant { Name = "Fiddle Leaf Fig", Description = "A demanding but beautiful plant with huge, glossy leaves.", WateringInterval = 7, LightRequirement = "Bright Indirect", Difficulty = "Hard" }
                        );
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log the error or handle it as needed
                    MessageBox.Show($"An error occurred while seeding the database: {ex.Message}");
                }
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (Host)
            {
                await Host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
