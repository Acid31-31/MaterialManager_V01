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
        private const double DefaultDialogWidth = 440;
        private const double DefaultDialogHeight = 680;
        private const double MinimumDialogWidth = 360;
        private const double MinimumDialogHeight = 460;

        // ── Kategorien ────────────────────────────────────────────────────────
        public List<string> Kategorien { get; } = new() { "Blech", "Rohr", "Profil" };

        private string _selectedKategorie = "Blech";
        public string SelectedKategorie
        {
            get => _selectedKategorie;
            set
            {
                if (_selectedKategorie == value) return;
                _selectedKategorie = value;
                OnPropertyChanged(nameof(SelectedKategorie));
                OnPropertyChanged(nameof(BlechFelderVisible));
                OnPropertyChanged(nameof(RohrFelderVisible));
                OnPropertyChanged(nameof(ProfilFelderVisible));
                OnPropertyChanged(nameof(LengthInfoVisible));
                OnPropertyChanged(nameof(GeschaetzterWert));
                UpdateShelfStats();
            }
        }

        public Visibility BlechFelderVisible  => _selectedKategorie == "Blech"  ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RohrFelderVisible   => _selectedKategorie == "Rohr"   ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ProfilFelderVisible => _selectedKategorie == "Profil" ? Visibility.Visible : Visibility.Collapsed;

        // ── Gemeinsam ─────────────────────────────────────────────────────────
        public List<string> MaterialArten { get; } = new() { "Stahl", "Edelstahl", "Aluminium" };

        private List<string> _legierungen = new();
        public List<string> Legierungen { get => _legierungen; set { _legierungen = value; OnPropertyChanged(nameof(Legierungen)); } }

        private List<string> _oberflaechen = new();
        public List<string> Oberflaechen { get => _oberflaechen; set { _oberflaechen = value; OnPropertyChanged(nameof(Oberflaechen)); } }

        private List<string> _gueten = new();
        public List<string> Gueten { get => _gueten; set { _gueten = value; OnPropertyChanged(nameof(Gueten)); } }

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

        private int _stueckzahl = 1;
        public int Stueckzahl { get => _stueckzahl; set { _stueckzahl = value; OnPropertyChanged(nameof(Stueckzahl)); } }

        private string _restnummer = "";
        public string Restnummer { get => _restnummer; set { _restnummer = value; OnPropertyChanged(nameof(Restnummer)); } }

        private DateTime? _selectedDatum = DateTime.Today;
        public DateTime? SelectedDatum { get => _selectedDatum; set { _selectedDatum = value; OnPropertyChanged(nameof(SelectedDatum)); } }

        private string _selectedLieferant = "";
        public string SelectedLieferant { get => _selectedLieferant; set { _selectedLieferant = value; OnPropertyChanged(nameof(SelectedLieferant)); } }

        private string _selectedLieferscheinNr = "";
        public string SelectedLieferscheinNr { get => _selectedLieferscheinNr; set { _selectedLieferscheinNr = value; OnPropertyChanged(nameof(SelectedLieferscheinNr)); } }

        private string _preisProKg = "0,00";
        public string PreisProKg { get => _preisProKg; set { _preisProKg = value; OnPropertyChanged(nameof(PreisProKg)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        public Visibility GueteVisible => SelectedMaterialArt == "Aluminium" ? Visibility.Visible : Visibility.Collapsed;

        // ── Blech-spezifisch ─────────────────────────────────────────────────
        public List<string> Formen { get; } = new() { "GF", "MF", "KF", "Rest" };
        public double[] Staerken => MaterialDefinitions.StandardStaerken;

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
        public double SelectedStaerke { get => _selectedStaerke; set { _selectedStaerke = value; OnPropertyChanged(nameof(SelectedStaerke)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        private string _mass = "";
        public string Mass { get => _mass; set { _mass = value; OnPropertyChanged(nameof(Mass)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        public string DatumLabel => SelectedForm == "Rest" ? "Erstelldatum:" : "Lieferdatum:";
        public bool IsMassEditable => SelectedForm == "Rest"
            || (_isEdit && _selectedKategorie == "Blech" && !string.IsNullOrWhiteSpace(_originalAuftragNr));
        public Visibility StueckzahlVisible => SelectedForm == "GF" || SelectedForm == "MF" || SelectedForm == "KF" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LieferantVisible  => SelectedForm == "GF" || SelectedForm == "MF" || SelectedForm == "KF" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EtiquetteVisible  => SelectedForm == "Rest" ? Visibility.Visible : Visibility.Collapsed;

        // ── Rohr-spezifisch ──────────────────────────────────────────────────
        public double[] RohrDurchmesser  => MaterialDefinitions.RohrStandardDurchmesser;
        public double[] RohrWandstaerken => MaterialDefinitions.RohrStandardWandstaerken;
        public int[]    StandardLaengen  => MaterialDefinitions.StandardLaengen;

        private string _selectedDurchmesser = "";
        public string SelectedDurchmesser { get => _selectedDurchmesser; set { _selectedDurchmesser = value; OnPropertyChanged(nameof(SelectedDurchmesser)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        private string _selectedRohrWand = "";
        public string SelectedRohrWand { get => _selectedRohrWand; set { _selectedRohrWand = value; OnPropertyChanged(nameof(SelectedRohrWand)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        private string _selectedLaenge = "";
        public string SelectedLaenge { get => _selectedLaenge; set { _selectedLaenge = value; OnPropertyChanged(nameof(SelectedLaenge)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        // ── Profil-spezifisch ────────────────────────────────────────────────
        public string[] ProfilTypen  => MaterialDefinitions.ProfilTypen;
        public double[] ProfilHoehen => MaterialDefinitions.ProfilStandardHoehen;
        public double[] ProfilBreiten => MaterialDefinitions.ProfilStandardBreiten;
        public double[] ProfilWandstaerken => MaterialDefinitions.ProfilStandardWandstaerken;

        private string _selectedProfilTyp = "";
        public string SelectedProfilTyp { get => _selectedProfilTyp; set { _selectedProfilTyp = value; OnPropertyChanged(nameof(SelectedProfilTyp)); } }

        private string _selectedProfilHoehe = "";
        public string SelectedProfilHoehe { get => _selectedProfilHoehe; set { _selectedProfilHoehe = value; OnPropertyChanged(nameof(SelectedProfilHoehe)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        private string _selectedProfilBreite = "";
        public string SelectedProfilBreite { get => _selectedProfilBreite; set { _selectedProfilBreite = value; OnPropertyChanged(nameof(SelectedProfilBreite)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        private string _selectedProfilWand = "";
        public string SelectedProfilWand { get => _selectedProfilWand; set { _selectedProfilWand = value; OnPropertyChanged(nameof(SelectedProfilWand)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        private string _selectedProfilLaenge = "";
        public string SelectedProfilLaenge { get => _selectedProfilLaenge; set { _selectedProfilLaenge = value; OnPropertyChanged(nameof(SelectedProfilLaenge)); OnPropertyChanged(nameof(GeschaetzterWert)); } }

        // ── Geschätzter Wert ─────────────────────────────────────────────────
        public string GeschaetzterWert
        {
            get
            {
                if (!decimal.TryParse(_preisProKg.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var preis) || preis <= 0)
                    return "0,00 €";

                double dichte = _selectedMaterialArt == "Stahl" ? 7850 : _selectedMaterialArt == "Edelstahl" ? 8000 : 2700;
                double gewicht = 0;

                if (_selectedKategorie == "Blech" && !string.IsNullOrWhiteSpace(_mass))
                {
                    var parts = _mass.Split('x');
                    if (parts.Length == 2 && double.TryParse(parts[0], out var l) && double.TryParse(parts[1], out var b))
                        gewicht = (l / 1000.0) * (b / 1000.0) * (_selectedStaerke / 1000.0) * dichte * _stueckzahl;
                }
                else if (_selectedKategorie == "Rohr")
                {
                    ParseD(_selectedDurchmesser, out var dm);
                    ParseD(_selectedRohrWand, out var wand);
                    ParseD(_selectedLaenge, out var len);
                    if (dm > 0 && wand > 0 && len > 0)
                    {
                        double ra = dm / 2.0 / 1000.0;
                        double ri = (dm / 2.0 - wand) / 1000.0;
                        gewicht = Math.PI * (ra * ra - ri * ri) * (len / 1000.0) * dichte * _stueckzahl;
                    }
                }
                else if (_selectedKategorie == "Profil")
                {
                    ParseD(_selectedProfilHoehe, out var h);
                    ParseD(_selectedProfilBreite, out var b);
                    ParseD(_selectedProfilWand, out var wand);
                    ParseD(_selectedProfilLaenge, out var len);
                    if (h > 0 && wand > 0 && len > 0)
                    {
                        double beff = b > 0 ? b : h;
                        double querschnitt = 2 * ((h + beff) * wand) / 1e6;
                        gewicht = querschnitt * (len / 1000.0) * dichte * _stueckzahl;
                    }
                }

                return gewicht > 0 ? $"≈ {(decimal)gewicht * preis:N2} €" : "0,00 €";
            }
        }

        private static bool ParseD(string s, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Replace(',', '.');
            return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        // ── Shelf-Stats ───────────────────────────────────────────────────────
        private double _currentLoadKg;
        public double CurrentLoadKg { get => _currentLoadKg; set { _currentLoadKg = value; OnPropertyChanged(nameof(CurrentLoadKg)); } }

        private double _capacityKg;
        public double CapacityKg { get => _capacityKg; set { _capacityKg = value; OnPropertyChanged(nameof(CapacityKg)); } }

        private double _utilizationPercent;
        public double UtilizationPercent { get => _utilizationPercent; set { _utilizationPercent = value; OnPropertyChanged(nameof(UtilizationPercent)); } }

        private double _totalInventoryWeight;
        public double TotalInventoryWeight { get => _totalInventoryWeight; set { _totalInventoryWeight = value; OnPropertyChanged(nameof(TotalInventoryWeight)); } }

        private double _currentLoadLengthM;
        public double CurrentLoadLengthM { get => _currentLoadLengthM; set { _currentLoadLengthM = value; OnPropertyChanged(nameof(CurrentLoadLengthM)); } }

        private double _totalInventoryLengthM;
        public double TotalInventoryLengthM { get => _totalInventoryLengthM; set { _totalInventoryLengthM = value; OnPropertyChanged(nameof(TotalInventoryLengthM)); } }

        public Visibility LengthInfoVisible => _selectedKategorie == "Rohr" || _selectedKategorie == "Profil"
            ? Visibility.Visible
            : Visibility.Collapsed;

        public MaterialItem Material { get; private set; }

        private IEnumerable<MaterialItem> _inventory = new List<MaterialItem>();
        private INotifyCollectionChanged? _inventoryNotifier;
        private bool _isEdit = false;
        private DateTime? _originalDatum;
        private DateTime? _originalAenderungsDatum;
        private string _originalAngelegtVon = "";
        private string _originalGeaendertVon = "";
        private string _originalAuftragNr = "";
        private string _originalLagerort = "";
        private string _originalPdfPfad = "";
        private string _originalPdfPfadAngefangeneTafel = "";

        private bool _canSave = true;
        public bool CanSave { get => _canSave; set { _canSave = value; OnPropertyChanged(nameof(CanSave)); } }

        public MaterialDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => ApplyResponsiveLayout();
            DataContext = this;
            Legierungen = new List<string>();
            Oberflaechen = new List<string>();
            Gueten = new List<string>();
            SelectedStaerke = Staerken.Length > 0 ? Staerken[0] : 0;
            CanSave = true;
            UpdateShelfStats();
        }

        private void ApplyResponsiveLayout()
        {
            var workArea = SystemParameters.WorkArea;
            MaxWidth  = Math.Max(MinimumDialogWidth,  workArea.Width  - 40);
            MaxHeight = Math.Max(MinimumDialogHeight, workArea.Height - 40);
            Width  = Math.Min(MaxWidth,  Math.Max(MinimumDialogWidth,  DefaultDialogWidth));
            Height = Math.Min(MaxHeight, Math.Max(MinimumDialogHeight, DefaultDialogHeight));
        }

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

        public void SetEditMode(MaterialItem original)
        {
            _isEdit = true;
            _originalDatum = original.Datum;
            _originalAenderungsDatum = original.AenderungsDatum;
            _originalAngelegtVon = original.AngelegtVon;
            _originalGeaendertVon = original.GeaendertVon;
            _originalAuftragNr = original.AuftragNr;
            _originalLagerort = original.Lagerort;
            _originalPdfPfad = original.PdfPfad;
            _originalPdfPfadAngefangeneTafel = original.PdfPfadAngefangeneTafel;

            SelectedKategorie = original.Kategorie.ToString();
            SelectedMaterialArt = original.MaterialArt;
            SelectedLegierung = original.Legierung;
            SelectedOberflaeche = original.Oberflaeche;
            SelectedGuete = original.Guete;
            Stueckzahl = original.Stueckzahl;
            Restnummer = original.Restnummer;
            SelectedDatum = original.Datum ?? DateTime.Today;
            SelectedLieferant = original.Lieferant;
            SelectedLieferscheinNr = original.LieferscheinNr;
            PreisProKg = original.PreisProKg.ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

            switch (original.Kategorie)
            {
                case MaterialKategorie.Blech:
                    SelectedForm = original.Form;
                    SelectedStaerke = original.Staerke;
                    Mass = original.Mass;
                    break;
                case MaterialKategorie.Rohr:
                    SelectedDurchmesser = original.Durchmesser > 0 ? original.Durchmesser.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
                    SelectedRohrWand    = original.Staerke     > 0 ? original.Staerke.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
                    SelectedLaenge      = original.Laenge       > 0 ? original.Laenge.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
                    break;
                case MaterialKategorie.Profil:
                    SelectedProfilTyp    = original.ProfilTyp;
                    SelectedProfilHoehe  = original.ProfilHoehe  > 0 ? original.ProfilHoehe.ToString(System.Globalization.CultureInfo.InvariantCulture)  : "";
                    SelectedProfilBreite = original.ProfilBreite > 0 ? original.ProfilBreite.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
                    SelectedProfilWand   = original.Staerke      > 0 ? original.Staerke.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
                    SelectedProfilLaenge = original.Laenge        > 0 ? original.Laenge.ToString(System.Globalization.CultureInfo.InvariantCulture)        : "";
                    break;
            }

            Title = "Material bearbeiten";
            OnPropertyChanged(nameof(IsMassEditable));
            OnPropertyChanged(nameof(EtiquetteVisible));
        }

        public MaterialDialog(MaterialItem existing) : this()
        {
            _isEdit = true;
            _originalAngelegtVon = existing.AngelegtVon;
            _originalGeaendertVon = existing.GeaendertVon;
            _originalAuftragNr = existing.AuftragNr;
            _originalLagerort = existing.Lagerort;
            _originalDatum = existing.Datum;
            _originalAenderungsDatum = existing.AenderungsDatum;
            SelectedDatum = existing.Datum ?? DateTime.Today;
            SetEditMode(existing);
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
                Mass = string.Empty;
            }
            UpdateShelfStats();
        }

        private void UpdateShelfStats()
        {
            string lagerort;
            if (_selectedKategorie == "Rohr")
            {
                lagerort = "Rohrlager";
            }
            else if (_selectedKategorie == "Profil")
            {
                lagerort = "Profillager";
            }
            else
            {
                lagerort = Services.RegalService.DetermineLagerort(
                    SelectedMaterialArt,
                    SelectedLegierung,
                    SelectedForm,
                    SelectedStaerke,
                    Mass,
                    _inventory);
            }

            CapacityKg = Services.RegalService.GetCapacity(lagerort);
            CurrentLoadKg = Services.RegalService.CalculateCurrentLoad(_inventory, lagerort);
            UtilizationPercent = Services.RegalService.ComputeUtilizationPercent(CurrentLoadKg, CapacityKg);
            TotalInventoryWeight = _inventory?.Sum(i => i.GewichtKg) ?? 0;

            CurrentLoadLengthM = _inventory?
                .Where(i => string.Equals(i.Lagerort, lagerort, StringComparison.OrdinalIgnoreCase) && (i.Kategorie == MaterialKategorie.Rohr || i.Kategorie == MaterialKategorie.Profil))
                .Sum(i => (i.Laenge * i.Stueckzahl) / 1000.0) ?? 0;

            TotalInventoryLengthM = _inventory?
                .Where(i => i.Kategorie == MaterialKategorie.Rohr || i.Kategorie == MaterialKategorie.Profil)
                .Sum(i => (i.Laenge * i.Stueckzahl) / 1000.0) ?? 0;
        }

        private void Inventory_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateShelfStats();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_inventoryNotifier != null)
                _inventoryNotifier.CollectionChanged -= Inventory_CollectionChanged;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();

            var currentUser = Services.OperatorIdentityService.CurrentOperatorName;
            var angelegtVon = _isEdit && !string.IsNullOrWhiteSpace(_originalAngelegtVon) ? _originalAngelegtVon : currentUser;
            var geaendertVon = _isEdit ? currentUser : string.Empty;
            var preis = decimal.TryParse(PreisProKg.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0m;

            var kat = _selectedKategorie switch
            {
                "Rohr"   => MaterialKategorie.Rohr,
                "Profil" => MaterialKategorie.Profil,
                _        => MaterialKategorie.Blech
            };

            if (kat == MaterialKategorie.Blech)
            {
                if (SelectedForm == "Rest")
                {
                    Restnummer = string.IsNullOrWhiteSpace(Restnummer) ? MaterialDefinitions.NeueRestnummer() : Restnummer;
                    Stueckzahl = 1;
                }
                Material = new MaterialItem
                {
                    Kategorie      = MaterialKategorie.Blech,
                    MaterialArt    = SelectedMaterialArt,
                    Legierung      = SelectedLegierung,
                    Oberflaeche    = SelectedOberflaeche,
                    Guete          = SelectedGuete,
                    Form           = SelectedForm,
                    Staerke        = SelectedStaerke,
                    Mass           = Mass,
                    Stueckzahl     = Stueckzahl,
                    Restnummer     = Restnummer,
                    Datum          = _isEdit ? _originalDatum : (SelectedDatum ?? DateTime.Today),
                    AenderungsDatum = _isEdit ? DateTime.Now : null,
                    Lagerort       = _isEdit && !string.IsNullOrWhiteSpace(_originalAuftragNr)
                                        ? _originalLagerort
                                        : Services.RegalService.DetermineLagerort(SelectedMaterialArt, SelectedLegierung, SelectedForm, SelectedStaerke, Mass, _inventory),
                    Lieferant      = SelectedLieferant,
                    LieferscheinNr = SelectedLieferscheinNr,
                    PreisProKg     = preis,
                    AngelegtVon    = angelegtVon,
                    GeaendertVon   = geaendertVon,
                    AuftragNr      = _isEdit ? _originalAuftragNr : string.Empty,
                    PdfPfad        = _isEdit ? _originalPdfPfad : string.Empty,
                    PdfPfadAngefangeneTafel = _isEdit ? _originalPdfPfadAngefangeneTafel : string.Empty
                };
            }
            else if (kat == MaterialKategorie.Rohr)
            {
                ParseD(SelectedDurchmesser, out var dm);
                ParseD(SelectedRohrWand,    out var wand);
                ParseD(SelectedLaenge,      out var len);
                Material = new MaterialItem
                {
                    Kategorie      = MaterialKategorie.Rohr,
                    MaterialArt    = SelectedMaterialArt,
                    Legierung      = SelectedLegierung,
                    Oberflaeche    = SelectedOberflaeche,
                    Guete          = SelectedGuete,
                    Durchmesser    = dm,
                    Staerke        = wand,
                    Laenge         = len,
                    Stueckzahl     = Stueckzahl,
                    Restnummer     = Restnummer,
                    Datum          = _isEdit ? _originalDatum : (SelectedDatum ?? DateTime.Today),
                    AenderungsDatum = _isEdit ? DateTime.Now : null,
                    Lagerort       = "Rohrlager",
                    Lieferant      = SelectedLieferant,
                    LieferscheinNr = SelectedLieferscheinNr,
                    PreisProKg     = preis,
                    AngelegtVon    = angelegtVon,
                    GeaendertVon   = geaendertVon,
                    AuftragNr      = _isEdit ? _originalAuftragNr : string.Empty,
                    PdfPfad        = _isEdit ? _originalPdfPfad : string.Empty,
                    PdfPfadAngefangeneTafel = _isEdit ? _originalPdfPfadAngefangeneTafel : string.Empty
                };
            }
            else
            {
                ParseD(SelectedProfilHoehe,  out var h);
                ParseD(SelectedProfilBreite, out var b);
                ParseD(SelectedProfilWand,   out var pw);
                ParseD(SelectedProfilLaenge, out var pl);
                Material = new MaterialItem
                {
                    Kategorie      = MaterialKategorie.Profil,
                    MaterialArt    = SelectedMaterialArt,
                    Legierung      = SelectedLegierung,
                    Oberflaeche    = SelectedOberflaeche,
                    Guete          = SelectedGuete,
                    ProfilTyp      = SelectedProfilTyp,
                    ProfilHoehe    = h,
                    ProfilBreite   = b,
                    Staerke        = pw,
                    Laenge         = pl,
                    Stueckzahl     = Stueckzahl,
                    Restnummer     = Restnummer,
                    Datum          = _isEdit ? _originalDatum : (SelectedDatum ?? DateTime.Today),
                    AenderungsDatum = _isEdit ? DateTime.Now : null,
                    Lagerort       = "Profillager",
                    Lieferant      = SelectedLieferant,
                    LieferscheinNr = SelectedLieferscheinNr,
                    PreisProKg     = preis,
                    AngelegtVon    = angelegtVon,
                    GeaendertVon   = geaendertVon,
                    AuftragNr      = _isEdit ? _originalAuftragNr : string.Empty,
                    PdfPfad        = _isEdit ? _originalPdfPfad : string.Empty,
                    PdfPfadAngefangeneTafel = _isEdit ? _originalPdfPfadAngefangeneTafel : string.Empty
                };
            }

            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnEtikett(object sender, RoutedEventArgs e)
        {
            var etiketLabel = $"{SelectedMaterialArt}-{SelectedStaerke:0.0}";
            var tempMaterial = new MaterialItem
            {
                MaterialArt    = SelectedMaterialArt,
                Legierung      = SelectedLegierung,
                Oberflaeche    = SelectedGuete,
                Guete          = SelectedGuete,
                Form           = SelectedForm,
                Staerke        = SelectedStaerke,
                Mass           = Mass,
                Stueckzahl     = Stueckzahl,
                Restnummer     = etiketLabel,
                Datum          = SelectedDatum ?? DateTime.Today,
                Lagerort       = "(wird berechnet)",
                Lieferant      = SelectedLieferant,
                LieferscheinNr = SelectedLieferscheinNr
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
