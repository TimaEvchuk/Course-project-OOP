using Plantify.Data.Repositories;
using Plantify.Models;

namespace Plantify.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IRepository<User> Users { get; private set; }
        public IPlantRepository Plants { get; private set; }
        public IRepository<Role> Roles { get; private set; }
        public IUserPlantRepository UserPlants { get; private set; }
        public INotificationRepository Notifications { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new Repository<User>(_context);
            Plants = new PlantRepository(_context);
            Roles = new Repository<Role>(_context);
            UserPlants = new UserPlantRepository(_context);
            Notifications = new NotificationRepository(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
