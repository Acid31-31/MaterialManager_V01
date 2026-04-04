using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public class KantbankDemoWindow : LaserDemoWindow
    {
        public KantbankDemoWindow()
        {
            Title = "Kantbank - Restmaterial";
            WorkspaceTitle = "Kantbank – Auftragsübersicht";
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} – Kantbanksicht";
        }
    }
}
