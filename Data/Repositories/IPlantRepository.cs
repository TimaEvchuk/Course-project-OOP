using Plantify.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plantify.Data.Repositories
{
    public interface IPlantRepository : IRepository<Plant>
    {
        Task<IEnumerable<Plant>> GetAllWithSectionsAsync();
    }
}
