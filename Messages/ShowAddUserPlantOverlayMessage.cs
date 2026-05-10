using Plantify.Models;

namespace Plantify.Messages
{
    public class ShowAddUserPlantOverlayMessage
    {
        public UserPlant? UserPlantToEdit { get; }
        public Plant? PlantToPreFill { get; }

        public ShowAddUserPlantOverlayMessage(UserPlant? userPlantToEdit)
        {
            UserPlantToEdit = userPlantToEdit;
            PlantToPreFill = null;
        }

        public ShowAddUserPlantOverlayMessage(Plant? plantToPreFill)
        {
            UserPlantToEdit = null;
            PlantToPreFill = plantToPreFill;
        }
    }
}
