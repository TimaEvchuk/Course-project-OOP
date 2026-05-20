using Microsoft.EntityFrameworkCore;
using Plantify.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plantify.Data.Repositories
{
    public class PlantRepository : Repository<Plant>, IPlantRepository
    {
        public PlantRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Plant>> GetAllWithSectionsAsync()
        {
            return await _appDbContext.Plants
                .Include(p => p.Sections)
                .Include(p => p.Variety)
                .Include(p => p.LightRequirement)
                .ToListAsync();
        }

        public async Task<Plant?> GetByIdWithSectionsAsync(int id)
        {
            return await _appDbContext.Plants
                .Include(p => p.Sections)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private AppDbContext _appDbContext => (AppDbContext)_context;
    }
}
