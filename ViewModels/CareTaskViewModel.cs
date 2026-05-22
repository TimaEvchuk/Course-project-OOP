using Plantify.Models;

namespace Plantify.ViewModels
{
    public class CareTaskViewModel
    {
        public UserPlant UserPlant { get; }
        public string TaskType { get; } // "Полив" или "Удобрение"

        public string TaskDescription => $"{TaskType}: {UserPlant.CustomName ?? UserPlant.Plant.Name}";

        public CareTaskViewModel(UserPlant userPlant, string taskType)
        {
            UserPlant = userPlant;
            TaskType = taskType;
        }
    }
}
