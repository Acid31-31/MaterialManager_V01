using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
    public class KantbankDemoWindow : LaserDemoWindow
    {
        protected override string Arbeitsbereich => AuftragArbeitsplatzService.Kantbank;
        protected override bool ShowReservedMaterialArea => false;
        protected override bool ShowExcelOrderButton => true;

        public KantbankDemoWindow()
        {
            Title = "Kantbank - Restmaterial";
            WorkspaceTitle = "Kantbank – Auftragsübersicht";
            HeaderText = $"Angemeldet als {OperatorIdentityService.CurrentOperatorName} – Kantbanksicht";
        }
    }
}
