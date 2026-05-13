using Plantify.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plantify.Data.Repositories
{
    public interface IUserPlantRepository : IRepository<UserPlant>
    {
        Task<IEnumerable<UserPlant>> GetUserPlantsWithPlantDetailsAsync(int userId);
    }
}
