using Microsoft.EntityFrameworkCore;
using Plantify.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.Data.Repositories
{
    public class UserPlantRepository : Repository<UserPlant>, IUserPlantRepository
    {
        // Store a strongly-typed reference to the AppDbContext
        private readonly AppDbContext _appDbContext;

        public UserPlantRepository(AppDbContext context) : base(context)
        {
            _appDbContext = context;
        }

        public async Task<IEnumerable<UserPlant>> GetUserPlantsWithPlantDetailsAsync(int userId)
        {
            return await _appDbContext.UserPlants
                                 .Include(up => up.Plant)
                                 .Where(up => up.UserId == userId)
                                 .ToListAsync();
        }
    }
}
