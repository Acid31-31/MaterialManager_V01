namespace MaterialManager_V01.Models
{
    public class GroupedMaterialItem
    {
        public string MaterialArt { get; set; } = "";
        public string Legierung { get; set; } = "";
        public string Oberflaeche { get; set; } = "";
        public string Guete { get; set; } = "";
        public double Staerke { get; set; }
        public string Masse { get; set; } = "";
        public int AnzahlReste { get; set; }
        public double GesamtGewichtKg { get; set; }
    }
}
