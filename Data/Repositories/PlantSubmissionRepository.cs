using Plantify.Models;

namespace Plantify.Data.Repositories
{
    public class PlantSubmissionRepository : Repository<PlantSubmission>, IPlantSubmissionRepository
    {
        public PlantSubmissionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
