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
            return await _context.Plants.Include(p => p.Sections).ToListAsync();
        }

        private AppDbContext _context => Context as AppDbContext;
    }
}
