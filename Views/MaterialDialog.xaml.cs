using MaterialManager_V01.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Collections.Specialized;
using System.Linq;

namespace MaterialManager_V01.Views
{
    public partial class MaterialDialog : Window, INotifyPropertyChanged
    {
        public List<string> MaterialArten { get; } =
            new() { "Stahl", "Edelstahl", "Aluminium" };

        private List<string> _legierungen = new();
        public List<string> Legierungen
        {
            get => _legierungen;
            set { _legierungen = value; OnPropertyChanged(nameof(Legierungen)); }
        }

        private List<string> _oberflaechen = new();
        public List<string> Oberflaechen
        {
            get => _oberflaechen;
            set { _oberflaechen = value; OnPropertyChanged(nameof(Oberflaechen)); }
        }

        private List<string> _gueten = new();
        public List<string> Gueten
        {
            get => _gueten;
            set { _gueten = value; OnPropertyChanged(nameof(Gueten)); }
        }

        public List<string> Formen { get; } =
            new() { "GF", "MF", "KF", "Rest" };

        public double[] Staerken => MaterialDefinitions.StandardStaerken;

        private string _selectedMaterialArt = "";
        public string SelectedMaterialArt
        {
            get => _selectedMaterialArt;
            set
            {
                if (_selectedMaterialArt == value) return;
                _selectedMaterialArt = value;
                OnPropertyChanged(nameof(SelectedMaterialArt));
                UpdateLegierungenUndOberflaechen();
                OnPropertyChanged(nameof(GueteVisible));
            }
        }

        private string _selectedLegierung = "";
        public string SelectedLegierung { get => _selectedLegierung; set { _selectedLegierung = value; OnPropertyChanged(nameof(SelectedLegierung)); } }

        private string _selectedOberflaeche = "";
        public string SelectedOberflaeche { get => _selectedOberflaeche; set { _selectedOberflaeche = value; OnPropertyChanged(nameof(SelectedOberflaeche)); } }

        private string _selectedGuete = "";
        public string SelectedGuete { get => _selectedGuete; set { _selectedGuete = value; OnPropertyChanged(nameof(SelectedGuete)); } }

        private string _selectedForm = "";
        public string SelectedForm
        {
            get => _selectedForm;
            set
            {
                if (_selectedForm == value) return;
                _selectedForm = value;
                OnPropertyChanged(nameof(SelectedForm));
                UpdateMassForForm();
                OnPropertyChanged(nameof(IsMassEditable));
                OnPropertyChanged(nameof(StueckzahlVisible));
                OnPropertyChanged(nameof(DatumLabel));
                OnPropertyChanged(nameof(LieferantVisible));
                OnPropertyChanged(nameof(EtiquetteVisible));
            }
        }

        private double _selectedStaerke;
        public double SelectedStaerke { get => _selectedStaerke; set { _selectedStaerke = value; OnPropertyChanged(nameof(SelectedStaerke)); } }

        private string _mass = "";
        public string Mass { get => _mass; set { _mass = value; OnPropertyChanged(nameof(Mass)); } }

        private int _stueckzahl = 1;
        public int Stueckzahl { get => _stueckzahl; set { _stueckzahl = value; OnPropertyChanged(nameof(Stueckzahl)); } }

        private string _restnummer = "";
        public string Restnummer { get => _restnummer; set { _restnummer = value; OnPropertyChanged(nameof(Restnummer)); } }

        private DateTime? _selectedDatum = DateTime.Today;
        public DateTime? SelectedDatum { get => _selectedDatum; set { _selectedDatum = value; OnPropertyChanged(nameof(SelectedDatum)); } }

        // Beschriftung ändert sich je nach Form
        public string DatumLabel => SelectedForm == "Rest" ? "Erstelldatum:" : "Lieferdatum:";

        // Lieferant
        private string _selectedLieferant = "";
        public string SelectedLieferant { get => _selectedLieferant; set { _selectedLieferant = value; OnPropertyChanged(nameof(SelectedLieferant)); } }

        // Lieferschein-Nr
        private string _selectedLieferscheinNr = "";
        public string SelectedLieferscheinNr { get => _selectedLieferscheinNr; set { _selectedLieferscheinNr = value; OnPropertyChanged(nameof(SelectedLieferscheinNr)); } }

        // Preis pro kg (Feature 8)
        private string _preisProKg = "0,00";
        public string PreisProKg { get => _preisProKg; set { _preisProKg = value; OnPropertyChanged(nameof(PreisProKg)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        // Geschätzter Wert (berechnet)
        public string GeschaetzterWert
        {
            get
            {
                if (decimal.TryParse(_preisProKg.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var preis) && preis > 0)
                {
                    // Geschätzte Masse berechnen
                    if (!string.IsNullOrWhiteSpace(_mass))
                    {
                        var parts = _mass.Split('x');
                        if (parts.Length == 2 && int.TryParse(parts[0], out var l) && int.TryParse(parts[1], out var b))
                        {
                            double dichte = _selectedMaterialArt == "Stahl" ? 7850 : _selectedMaterialArt == "Edelstahl" ? 8000 : 2700;
                            double gewicht = (l / 1000.0) * (b / 1000.0) * (_selectedStaerke / 1000.0) * dichte * _stueckzahl;
                            var wert = (decimal)gewicht * preis;
                            return $"≈ {wert:N2} €";
                        }
                    }
                }
                return "0,00 €";
            }
        }

        // Lieferant nur bei GF/MF/KF sichtbar
        public Visibility LieferantVisible =>
            SelectedForm == "GF" || SelectedForm == "MF" || SelectedForm == "KF"
                ? Visibility.Visible
                : Visibility.Collapsed;

        // Etikett-Button bei Rest sichtbar (für QR-Code)
        public Visibility EtiquetteVisible =>
            SelectedForm == "Rest"
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility GueteVisible =>
            SelectedMaterialArt == "Aluminium"
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility StueckzahlVisible =>
            SelectedForm == "GF" || SelectedForm == "MF" || SelectedForm == "KF"
                ? Visibility.Visible
                : Visibility.Collapsed;

        public bool IsMassEditable => SelectedForm == "Rest";

        public MaterialItem Material { get; private set; }

        private IEnumerable<MaterialItem> _inventory = new List<MaterialItem>();
        private INotifyCollectionChanged? _inventoryNotifier;
        private bool _isEdit = false;
        private DateTime? _originalDatum;
        private DateTime? _originalAenderungsDatum;
        private string _originalAngelegtVon = "";
        private string _originalGeaendertVon = "";

        public MaterialDialog()
        {
            InitializeComponent();
            DataContext = this;
            // initialize defaults
            Legierungen = new List<string>();
            Oberflaechen = new List<string>();
            Gueten = new List<string>();
            SelectedStaerke = Staerken.Length > 0 ? Staerken[0] : 0;
            CanSave = true;
            UpdateShelfStats();
        }

        // overload to accept current inventory to compute shelf utilization
        public MaterialDialog(IEnumerable<MaterialItem> inventory) : this()
        {
            _inventory = inventory ?? new List<MaterialItem>();
            if (inventory is INotifyCollectionChanged nc)
            {
                _inventoryNotifier = nc;
                _inventoryNotifier.CollectionChanged += Inventory_CollectionChanged;
            }
            UpdateShelfStats();
        }

        /// <summary>
        /// Aktiviert Bearbeitungsmodus — Originaldatum bleibt, Änderungsdatum wird gesetzt
        /// </summary>
        public void SetEditMode(MaterialItem original)
        {
            _isEdit = true;
            _originalDatum = original.Datum;
            _originalAenderungsDatum = original.AenderungsDatum;
            _originalAngelegtVon = original.AngelegtVon;
            _originalGeaendertVon = original.GeaendertVon;

            SelectedMaterialArt = original.MaterialArt;
            SelectedLegierung = original.Legierung;
            SelectedOberflaeche = original.Oberflaeche;
            SelectedGuete = original.Guete;
            SelectedForm = original.Form;
            SelectedStaerke = original.Staerke;
            Mass = original.Mass;
            Stueckzahl = original.Stueckzahl;
            Restnummer = original.Restnummer;

            SelectedDatum = original.Datum ?? DateTime.Today;
            SelectedLieferant = original.Lieferant;
            SelectedLieferscheinNr = original.LieferscheinNr;
            PreisProKg = original.PreisProKg.ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
            Title = "Material bearbeiten";
            OnPropertyChanged(nameof(EtiquetteVisible));
        }

        public MaterialDialog(MaterialItem existing) : this()
        {
            _isEdit = true;
            _originalAngelegtVon = existing.AngelegtVon;
            _originalGeaendertVon = existing.GeaendertVon;
            // populate lists first
            SelectedMaterialArt = existing.MaterialArt;
            SelectedLegierung = existing.Legierung;
            SelectedOberflaeche = existing.Oberflaeche;
            SelectedGuete = existing.Guete;
            SelectedForm = existing.Form;
            SelectedStaerke = existing.Staerke;
            Mass = existing.Mass;
            Stueckzahl = existing.Stueckzahl;
            Restnummer = existing.Restnummer;
            _originalDatum = existing.Datum;
            _originalAenderungsDatum = existing.AenderungsDatum;
            SelectedDatum = existing.Datum ?? DateTime.Today;
        }

        private void UpdateLegierungenUndOberflaechen()
        {
            if (string.IsNullOrWhiteSpace(SelectedMaterialArt))
            {
                Legierungen = new List<string>();
                Oberflaechen = new List<string>();
                Gueten = new List<string>();
                return;
            }

            if (MaterialDefinitions.Legierungen.TryGetValue(SelectedMaterialArt, out var lg))
                Legierungen = new List<string>(lg);
            else
                Legierungen = new List<string>();

            if (MaterialDefinitions.Oberflaechen.TryGetValue(SelectedMaterialArt, out var of))
                Oberflaechen = new List<string>(of);
            else
                Oberflaechen = new List<string>();

            Gueten = SelectedMaterialArt == "Aluminium" ? new List<string>(MaterialDefinitions.AluminiumGueten) : new List<string>();
        }

        private void UpdateMassForForm()
        {
            if (SelectedForm != "Rest" && !string.IsNullOrWhiteSpace(SelectedForm) && MaterialDefinitions.StandardMasse.TryGetValue(SelectedForm, out var m))
            {
                Mass = $"{m.Laenge}x{m.Breite}";
                Restnummer = string.Empty;
            }
            else if (SelectedForm == "Rest")
            {
                Mass = string.Empty; // user must enter
            }

            UpdateShelfStats();
        }

        // Shelf stats bindings
        private double _currentLoadKg;
        public double CurrentLoadKg { get => _currentLoadKg; set { _currentLoadKg = value; OnPropertyChanged(nameof(CurrentLoadKg)); } }

        private double _capacityKg;
        public double CapacityKg { get => _capacityKg; set { _capacityKg = value; OnPropertyChanged(nameof(CapacityKg)); } }

        private double _utilizationPercent;
        public double UtilizationPercent { get => _utilizationPercent; set { _utilizationPercent = value; OnPropertyChanged(nameof(UtilizationPercent)); } }

        private void UpdateShelfStats()
        {
            // determine target lagerort for the selected form
            var lagerort = SelectedForm == "Rest" ? "Restregal" : SelectedForm == "GF" || SelectedForm == "MF" || SelectedForm == "KF" ? "Tafelregal" : "Palette";

            CapacityKg = Services.LagerService.GetCapacity(lagerort);
            CurrentLoadKg = Services.LagerService.CalculateCurrentLoad(_inventory, lagerort);
            UtilizationPercent = Services.LagerService.ComputeUtilizationPercent(CurrentLoadKg, CapacityKg);
            TotalInventoryWeight = _inventory?.Sum(i => i.GewichtKg) ?? 0;
        }

        private void Inventory_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Recompute stats on collection changes
            UpdateShelfStats();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_inventoryNotifier != null)
                _inventoryNotifier.CollectionChanged -= Inventory_CollectionChanged;
        }

        private double _totalInventoryWeight;
        public double TotalInventoryWeight { get => _totalInventoryWeight; set { _totalInventoryWeight = value; OnPropertyChanged(nameof(TotalInventoryWeight)); } }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            if (SelectedForm == "Rest")
            {
                Restnummer = string.IsNullOrWhiteSpace(Restnummer) ? MaterialDefinitions.NeueRestnummer() : Restnummer;
                Stueckzahl = 1; // Reste haben immer Stückzahl 1
            }

            var currentUser = Environment.UserName;
            var angelegtVon = _isEdit && !string.IsNullOrWhiteSpace(_originalAngelegtVon) ? _originalAngelegtVon : currentUser;
            var geaendertVon = _isEdit ? currentUser : string.Empty;

            Material = new MaterialItem
            {
                MaterialArt = SelectedMaterialArt,
                Legierung = SelectedLegierung,
                Oberflaeche = SelectedOberflaeche,
                Guete = SelectedGuete,
                Form = SelectedForm,
                Staerke = SelectedStaerke,
                Mass = Mass,
                Stueckzahl = Stueckzahl,
                Restnummer = Restnummer,
                Datum = _isEdit ? _originalDatum : (SelectedDatum ?? DateTime.Today),
                AenderungsDatum = _isEdit ? DateTime.Now : null,
                Lagerort = MaterialManager_V01.Services.RegalService.DetermineLagerort(
                    SelectedMaterialArt,
                    SelectedLegierung,
                    SelectedForm,
                    SelectedStaerke,
                    Mass,
                    _inventory),
                Lieferant = SelectedLieferant,
                LieferscheinNr = SelectedLieferscheinNr,
                PreisProKg = decimal.TryParse(PreisProKg.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var preis) ? preis : 0m,
                AngelegtVon = angelegtVon,
                GeaendertVon = geaendertVon
            };

            DialogResult = true;
        }

        private bool _canSave = true;
        public bool CanSave { get => _canSave; set { _canSave = value; OnPropertyChanged(nameof(CanSave)); } }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnEtikett(object sender, RoutedEventArgs e)
        {
            // Temporäres Material für Etikett erstellen (Dialog bleibt offen)
            // ✅ Restnummer = "Materialart-Stärke" (z.B. "Stahl-25.0")
            var etiketLabel = $"{SelectedMaterialArt}-{SelectedStaerke:0.0}";
            
            var tempMaterial = new MaterialItem
            {
                MaterialArt = SelectedMaterialArt,
                Legierung = SelectedLegierung,
                Oberflaeche = SelectedOberflaeche,
                Guete = SelectedGuete,
                Form = SelectedForm,
                Staerke = SelectedStaerke,
                Mass = Mass,
                Stueckzahl = Stueckzahl,
                Restnummer = etiketLabel,  // ✅ GEÄNDERT: z.B. "Stahl-25.0"
                Datum = SelectedDatum ?? DateTime.Today,
                Lagerort = "(wird berechnet)",
                Lieferant = SelectedLieferant,  // ✅ Lieferant hinzugefügt
                LieferscheinNr = SelectedLieferscheinNr  // ✅ Lieferschein-Nr hinzugefügt
            };
            Services.QrCodeService.ZeigeEtikett(tempMaterial);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && CanSave)
            {
                OnOk(sender, e);
                e.Handled = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
