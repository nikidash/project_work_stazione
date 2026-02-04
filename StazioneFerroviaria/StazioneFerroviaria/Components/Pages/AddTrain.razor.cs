using FerroviaApi.Models;

namespace StazioneFerroviaria.Components.Pages;

public partial class AddTrain
{
    private Train train = new()
    {
        ScheduledArrivalTime = DateTime.UtcNow,
        ScheduledDepartureTime = DateTime.UtcNow.AddMinutes(10)
    };

    private async Task SaveTrain()
    {
        Db.Trains.Add(train);
        await Db.SaveChangesAsync();
        Nav.NavigateTo("/dashboard");
    }
}
