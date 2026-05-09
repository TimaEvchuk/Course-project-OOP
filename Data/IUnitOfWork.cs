using Plantify.Data.Repositories;
using Plantify.Models;

namespace Plantify.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IPlantRepository Plants { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserPlant> UserPlants { get; }
        Task<int> CompleteAsync();
    }
}
