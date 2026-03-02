using System.Globalization;

namespace MaterialManager_V01.Models
{
    public static class MaterialDefinitions
    {
        public static readonly Dictionary<string, (int Laenge, int Breite)> StandardMasse =
            new()
            {
                { "GF", (3000,1500) },
                { "MF", (2500,1250) },
                { "KF", (2000,1000) }
            };

        public static readonly double[] StandardStaerken =
        {
            0.3, 0.5, 0.8, 1, 1.5, 2, 3, 4, 5, 6, 8, 10, 12, 15, 20, 25
        };

        public static readonly Dictionary<string, List<string>> Legierungen =
            new()
            {
                { "Stahl", new List<string>{ 
                    "DC01", "DC03", "DC04", "DC05", "DC06",
                    "DD11", "DD12", "DD13", "DD14",
                    "DX51D", "DX52D", "DX53D", "DX54D", "DX56D",
                    "S235", "S235JR", "S275", "S355", "S355J2", "S460", "S550",
                    "HB400", "HB500", "HX340LAD", "HX420LAD"
                } },
                { "Edelstahl", new List<string>{ 
                    "1.4301", "1.4307", "1.4404", "1.4571", "1.4541", "1.4401",
                    "1.4016", "1.4003", "1.4509", "1.4512", "1.4520", "1.4539",
                    "1.4828", "1.4845", "1.4876"
                } },
                { "Aluminium", new List<string>{ 
                    "EN AW-1050", "EN AW-1050A", "EN AW-1100",
                    "EN AW-3003", "EN AW-3103", "EN AW-3105",
                    "EN AW-5005", "EN AW-5052", "EN AW-5083", "EN AW-5754",
                    "EN AW-6060", "EN AW-6061", "EN AW-6082",
                    "EN AW-7020", "EN AW-7075"
                } }
            };

        public static readonly Dictionary<string, List<string>> Oberflaechen =
            new()
            {
                { "Stahl", new List<string>{ 
                    "blank", "Roh", "geölt", "phosphatiert",
                    "g+g", "ELO", "Sen-Verz", "feuerverzinkt", "galvanisch verzinkt",
                    "Tränenblech", "Riffelblech", "Warzenblech",
                    "lackiert", "pulverbeschichtet", "grundiert"
                } },
                { "Edelstahl", new List<string>{ 
                    "2B", "2D", "2E", "2R",
                    "1D", "1E", "BA",
                    "Korn240", "Korn320", "Korn400", "Korn600",
                    "Gebürstet", "geschliffen", "poliert",
                    "Riffelblech", "Tränenblech", "Warzenblech",
                    "gebeizt", "elektrolytisch poliert"
                } },
                { "Aluminium", new List<string>{ 
                    "mit Folie", "ohne Folie", "einseitig foliert", "beidseitig foliert",
                    "Riffelblech", "Tränenblech", "Warzenblech",
                    "eloxiert", "blank", "natur", "matt",
                    "lackiert", "pulverbeschichtet", "anodisiert"
                } }
            };

        public static readonly List<string> AluminiumGueten =
            new() { "H111", "H112", "H114", "H116", "H321" };

        public static string NeueRestnummer()
        {
            return Services.RestnummerService.GenerateNext();
        }
    }
}
