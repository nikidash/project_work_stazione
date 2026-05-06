using FerroviaApi.Models;

namespace StazioneFerroviaria.Services;

public class TrainNotificationService
{
    public event Action<Train>? OnTrainAdded;

    public void NotifyTrainAdded(Train train)
    {
        OnTrainAdded?.Invoke(train);
    }
}
