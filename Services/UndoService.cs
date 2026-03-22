using MaterialManager_V01.Models;
using System.Collections.Generic;

namespace MaterialManager_V01.Services
{
    public static class UndoService
    {
        private static readonly Stack<UndoAction> _undoStack = new();
        private const int MaxUndo = 20;

        public static void Push(string beschreibung, List<MaterialItem> geloeschteItems)
        {
            _undoStack.Push(new UndoAction
            {
                Beschreibung = beschreibung,
                Items = new List<MaterialItem>(geloeschteItems),
                Zeitpunkt = System.DateTime.Now
            });

            // Begrenze Stack-Größe
            if (_undoStack.Count > MaxUndo)
            {
                var temp = new Stack<UndoAction>();
                int count = 0;
                foreach (var item in _undoStack)
                {
                    if (count++ < MaxUndo)
                        temp.Push(item);
                }
                _undoStack.Clear();
                foreach (var item in temp)
                    _undoStack.Push(item);
            }
        }

        public static bool CanUndo => _undoStack.Count > 0;

        public static string? PeekDescription()
        {
            if (_undoStack.Count == 0) return null;
            var action = _undoStack.Peek();
            return $"{action.Beschreibung} ({action.Zeitpunkt:HH:mm})";
        }

        public static List<MaterialItem>? Undo()
        {
            if (_undoStack.Count == 0) return null;
            return _undoStack.Pop().Items;
        }

        public static int Count => _undoStack.Count;

        private class UndoAction
        {
            public string Beschreibung { get; set; } = "";
            public List<MaterialItem> Items { get; set; } = new();
            public System.DateTime Zeitpunkt { get; set; }
        }
    }
}
