using MaterialManager_V01.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaterialManager_V01.Services
{
    public static class UndoService
    {
        private static readonly Stack<HistoryAction> _undoStack = new();
        private static readonly Stack<HistoryAction> _redoStack = new();
        private static readonly Stack<LegacyUndoAction> _legacyUndoStack = new();
        private const int MaxHistory = 20;

        public static void Push(string beschreibung, List<MaterialItem> geloeschteItems)
        {
            _legacyUndoStack.Push(new LegacyUndoAction
            {
                Beschreibung = beschreibung,
                Items = CloneItems(geloeschteItems),
                Zeitpunkt = DateTime.Now
            });

            TrimLegacyStack();
        }

        public static List<MaterialItem>? Undo()
        {
            if (_legacyUndoStack.Count == 0)
                return null;

            return CloneItems(_legacyUndoStack.Pop().Items);
        }

        public static void PushSnapshot(string beschreibung, IEnumerable<MaterialItem> items)
        {
            _undoStack.Push(new HistoryAction
            {
                Beschreibung = beschreibung,
                Items = CloneItems(items),
                Zeitpunkt = DateTime.Now
            });

            TrimStack(_undoStack);
            _redoStack.Clear();
        }

        public static bool CanUndo => _undoStack.Count > 0;
        public static bool CanRedo => _redoStack.Count > 0;

        public static string? PeekUndoDescription()
        {
            if (_undoStack.Count == 0)
                return null;

            var action = _undoStack.Peek();
            return $"{action.Beschreibung} ({action.Zeitpunkt:HH:mm})";
        }

        public static string? PeekRedoDescription()
        {
            if (_redoStack.Count == 0)
                return null;

            var action = _redoStack.Peek();
            return $"{action.Beschreibung} ({action.Zeitpunkt:HH:mm})";
        }

        public static List<MaterialItem>? Undo(IEnumerable<MaterialItem> currentItems)
        {
            if (_undoStack.Count == 0)
                return null;

            var action = _undoStack.Pop();
            _redoStack.Push(new HistoryAction
            {
                Beschreibung = action.Beschreibung,
                Items = CloneItems(currentItems),
                Zeitpunkt = DateTime.Now
            });

            TrimStack(_redoStack);
            return CloneItems(action.Items);
        }

        public static List<MaterialItem>? Redo(IEnumerable<MaterialItem> currentItems)
        {
            if (_redoStack.Count == 0)
                return null;

            var action = _redoStack.Pop();
            _undoStack.Push(new HistoryAction
            {
                Beschreibung = action.Beschreibung,
                Items = CloneItems(currentItems),
                Zeitpunkt = DateTime.Now
            });

            TrimStack(_undoStack);
            return CloneItems(action.Items);
        }

        private static void TrimStack(Stack<HistoryAction> stack)
        {
            if (stack.Count <= MaxHistory)
                return;

            var items = stack.Take(MaxHistory).Reverse().ToList();
            stack.Clear();
            foreach (var item in items)
                stack.Push(item);
        }

        private static void TrimLegacyStack()
        {
            if (_legacyUndoStack.Count <= MaxHistory)
                return;

            var items = _legacyUndoStack.Take(MaxHistory).Reverse().ToList();
            _legacyUndoStack.Clear();
            foreach (var item in items)
                _legacyUndoStack.Push(item);
        }

        private static List<MaterialItem> CloneItems(IEnumerable<MaterialItem> items)
        {
            return items.Select(CloneItem).ToList();
        }

        private static MaterialItem CloneItem(MaterialItem source)
        {
            return new MaterialItem
            {
                Kategorie = source.Kategorie,
                MaterialArt = source.MaterialArt,
                Legierung = source.Legierung,
                Oberflaeche = source.Oberflaeche,
                Guete = source.Guete,
                SuchTrefferArt = source.SuchTrefferArt,
                Form = source.Form,
                Staerke = source.Staerke,
                Mass = source.Mass,
                Durchmesser = source.Durchmesser,
                Laenge = source.Laenge,
                ProfilTyp = source.ProfilTyp,
                ProfilHoehe = source.ProfilHoehe,
                ProfilBreite = source.ProfilBreite,
                Stueckzahl = source.Stueckzahl,
                Restnummer = source.Restnummer,
                Datum = source.Datum,
                AenderungsDatum = source.AenderungsDatum,
                Lagerort = source.Lagerort,
                AngelegtVon = source.AngelegtVon,
                GeaendertVon = source.GeaendertVon,
                Lieferant = source.Lieferant,
                LieferscheinNr = source.LieferscheinNr,
                AuftragNr = source.AuftragNr,
                PdfPfad = source.PdfPfad,
                PdfPfadAngefangeneTafel = source.PdfPfadAngefangeneTafel,
                PreisProKg = source.PreisProKg,
                IsHighlighted = source.IsHighlighted,
                IsSelected = source.IsSelected
            };
        }

        private class HistoryAction
        {
            public string Beschreibung { get; set; } = string.Empty;
            public List<MaterialItem> Items { get; set; } = new();
            public DateTime Zeitpunkt { get; set; }
        }

        private class LegacyUndoAction
        {
            public string Beschreibung { get; set; } = string.Empty;
            public List<MaterialItem> Items { get; set; } = new();
            public DateTime Zeitpunkt { get; set; }
        }
    }
}
