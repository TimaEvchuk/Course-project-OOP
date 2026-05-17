using Plantify.Data.Repositories;
using Plantify.Models;

namespace Plantify.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IPlantRepository Plants { get; }
        IRepository<Role> Roles { get; }
        IUserPlantRepository UserPlants { get; }
        INotificationRepository Notifications { get; }
        IPlantSubmissionRepository PlantSubmissions { get; }
        IRepository<Variety> Varieties { get; }
        IRepository<LightRequirement> LightRequirements { get; }
        Task<int> CompleteAsync();
    }
}
