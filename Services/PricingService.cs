using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MaterialManager_V01.Services
{
    public static class PricingService
    {
        private static readonly CultureInfo De = new("de-DE");

        private static string ConfigPathInAppDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pricing-model.json");
        private static string ConfigPathInDataDirectory => Path.Combine(PathService.DataDirectory, "pricing-model.json");

        public sealed class PricingDisplay
        {
            public string HeadlinePriceText { get; set; } = string.Empty;
            public string SinglePriceText { get; set; } = string.Empty;
            public string MultiPriceText { get; set; } = string.Empty;
            public string CompanyPriceText { get; set; } = string.Empty;
            public string EnterprisePriceText { get; set; } = string.Empty;
            public string CalculationHintText { get; set; } = string.Empty;
        }

        public static PricingDisplay BuildForCurrentSoftware()
        {
            var model = LoadOrCreateModel();

            var score = model.Modules.Sum(m => Math.Max(0m, m.Score));
            var baseYearPrice = RoundToStep(score * model.BasePriceFactor, model.RoundingStep);

            var single = RoundToStep(baseYearPrice * model.Multipliers.Single, model.RoundingStep);
            var multi3 = RoundToStep(baseYearPrice * model.Multipliers.Multi3, model.RoundingStep);
            var company10 = RoundToStep(baseYearPrice * model.Multipliers.Company10, model.RoundingStep);
            var enterpriseFrom = RoundToStep(baseYearPrice * model.Multipliers.Enterprise, model.RoundingStep);

            return new PricingDisplay
            {
                HeadlinePriceText = $"ab {single.ToString("N0", De)} EUR/Jahr",
                SinglePriceText = ToEuro(single),
                MultiPriceText = ToEuro(multi3),
                CompanyPriceText = ToEuro(company10),
                EnterprisePriceText = $"ab {ToEuro(enterpriseFrom)}",
                CalculationHintText =
                    $"Preise werden anhand des Software-Umfangs bewertet (Score {score:N0}, Faktor {model.BasePriceFactor.ToString("N2", De)}). Anpassbar in pricing-model.json."
            };
        }

        private static PricingModel LoadOrCreateModel()
        {
            var candidates = new[] { ConfigPathInAppDirectory, ConfigPathInDataDirectory };

            foreach (var path in candidates)
            {
                var loaded = TryLoad(path);
                if (loaded != null)
                    return loaded;
            }

            var model = CreateDefaultModel();
            TrySave(ConfigPathInDataDirectory, model);
            return model;
        }

        private static PricingModel? TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                var model = JsonSerializer.Deserialize<PricingModel>(json);
                if (model == null || model.Modules.Count == 0)
                    return null;

                EnsureDefaults(model);
                return model;
            }
            catch
            {
                return null;
            }
        }

        private static void TrySave(string path, PricingModel model)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // absichtlich ohne Abbruch
            }
        }

        private static PricingModel CreateDefaultModel()
        {
            return new PricingModel
            {
                BasePriceFactor = 5.0m,
                RoundingStep = 50m,
                Multipliers = new PricingMultipliers
                {
                    Single = 1.0m,
                    Multi3 = 2.2m,
                    Company10 = 4.25m,
                    Enterprise = 7.7m
                },
                Modules = new List<PricingModule>
                {
                    new() { Name = "Lager + Inventur + Materialdaten", Score = 220m },
                    new() { Name = "Netzwerk/Mehrbenutzer", Score = 180m },
                    new() { Name = "Aufträge + KW + Archiv", Score = 210m },
                    new() { Name = "Laser-/Tafelplanung-Workflows", Score = 260m },
                    new() { Name = "Audit + Lizenz + Sicherheit", Score = 190m },
                    new() { Name = "Update/Deployment/Supportfähigkeit", Score = 110m }
                }
            };
        }

        private static void EnsureDefaults(PricingModel model)
        {
            if (model.BasePriceFactor <= 0m)
                model.BasePriceFactor = 5.0m;
            if (model.RoundingStep <= 0m)
                model.RoundingStep = 50m;

            model.Multipliers ??= new PricingMultipliers();
            if (model.Multipliers.Single <= 0m) model.Multipliers.Single = 1.0m;
            if (model.Multipliers.Multi3 <= 0m) model.Multipliers.Multi3 = 2.2m;
            if (model.Multipliers.Company10 <= 0m) model.Multipliers.Company10 = 4.25m;
            if (model.Multipliers.Enterprise <= 0m) model.Multipliers.Enterprise = 7.7m;

            if (model.Modules == null || model.Modules.Count == 0)
                model.Modules = CreateDefaultModel().Modules;
        }

        private static decimal RoundToStep(decimal value, decimal step)
        {
            if (value <= 0m) return 0m;
            if (step <= 0m) step = 50m;
            return Math.Ceiling(value / step) * step;
        }

        private static string ToEuro(decimal amount)
        {
            return amount.ToString("N2", De) + " EUR";
        }

        private sealed class PricingModel
        {
            public decimal BasePriceFactor { get; set; } = 5.0m;
            public decimal RoundingStep { get; set; } = 50m;
            public PricingMultipliers Multipliers { get; set; } = new();
            public List<PricingModule> Modules { get; set; } = new();
        }

        private sealed class PricingMultipliers
        {
            public decimal Single { get; set; } = 1.0m;
            public decimal Multi3 { get; set; } = 2.2m;
            public decimal Company10 { get; set; } = 4.25m;
            public decimal Enterprise { get; set; } = 7.7m;
        }

        private sealed class PricingModule
        {
            public string Name { get; set; } = string.Empty;
            public decimal Score { get; set; }
        }
    }
}
