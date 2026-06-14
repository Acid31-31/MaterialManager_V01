using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialManager_V01.Models;
using MaterialManager_V01.Services;

namespace MaterialManager_V01.Views
{
	public partial class RohrZuschnittDialog : Window
	{
		private readonly ObservableCollection<RohrZuschnittEingabePosition> _positionen = new();
		private readonly List<string> _drawingOptions = new();
		private string _selectedPdfPath = string.Empty;
		private bool _isNavigating;

		public RohrZuschnittDialog(IEnumerable<MaterialItem> materialien)
		{
			InitializeComponent();
			PositionenGrid.ItemsSource = _positionen;
			LoadDrawingOptions(materialien?.ToList() ?? new List<MaterialItem>());
			Loaded += OnLoaded;
			Closing += OnWindowClosing;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Styling wird vollständig über ControlTemplates im XAML gesteuert
		}

		private static void ApplyDarkEditableTextBox(TextBox textBox) { }

		private static void ApplyDarkEditableComboBoxText(ComboBox comboBox) { }

		private void LoadDrawingOptions(List<MaterialItem> materialien)
		{
			_drawingOptions.Clear();
			_drawingOptions.AddRange(materialien
				.Where(m => m.Kategorie == MaterialKategorie.Rohr)
				.Where(m => !string.IsNullOrWhiteSpace(m.PdfPfad))
				.Select(m => Path.GetFileNameWithoutExtension(m.PdfPfad))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x));

			ZeichnungComboBox.ItemsSource = _drawingOptions;
			if (_drawingOptions.Count > 0)
				ZeichnungComboBox.SelectedIndex = 0;
		}

		private void OnZeichnungSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ZeichnungComboBox.SelectedItem is string s)
				BezeichnungTextBox.Text = s;
		}

		private void OnPdfDragEnter(object sender, DragEventArgs e)
		{
			if (IsPdfDrag(e))
			{
				e.Effects = DragDropEffects.Copy;
				PdfDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
					System.Windows.Media.Color.FromRgb(0, 191, 165));
				PdfDropBorder.Background = new System.Windows.Media.SolidColorBrush(
					System.Windows.Media.Color.FromArgb(40, 0, 191, 165));
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
		}

		private void OnPdfDragLeave(object sender, DragEventArgs e)
		{
			PdfDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
				System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));
			PdfDropBorder.Background = new System.Windows.Media.SolidColorBrush(
				System.Windows.Media.Color.FromRgb(0x11, 0x11, 0x11));
			e.Handled = true;
		}

		private void OnPdfDragOver(object sender, DragEventArgs e)
		{
			e.Effects = IsPdfDrag(e) ? DragDropEffects.Copy : DragDropEffects.None;
			e.Handled = true;
		}

		private void OnPdfDrop(object sender, DragEventArgs e)
		{
			OnPdfDragLeave(sender, e);

			if (!IsPdfDrag(e))
				return;

			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			var pdf = files?.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
			if (string.IsNullOrEmpty(pdf))
				return;

			_selectedPdfPath = pdf;
			AusgewaehltePdfTextBlock.Text = $"📄  {Path.GetFileName(pdf)}  –  wird ausgelesen …";
			AusgewaehltePdfTextBlock.Foreground = System.Windows.Media.Brushes.White;
			AusgewaehltePdfTextBlock.FontStyle = System.Windows.FontStyles.Normal;

			var parse = PdfRohrParser.LesePdf(pdf);

			if (parse.LaengeMm.HasValue && parse.LaengeMm.Value > 0)
				PositionLaengeTextBox.Text = ((int)Math.Round(parse.LaengeMm.Value)).ToString();

			if (parse.Menge.HasValue && parse.Menge.Value > 0)
				PositionMengeTextBox.Text = parse.Menge.Value.ToString();

			if (!string.IsNullOrWhiteSpace(parse.Bezeichnung) && string.IsNullOrWhiteSpace(BezeichnungTextBox.Text))
				BezeichnungTextBox.Text = parse.Bezeichnung;

			if (parse.Erfolgreich)
			{
				var gefunden = new List<string>();
				if (parse.LaengeMm.HasValue) gefunden.Add($"Länge: {(int)Math.Round(parse.LaengeMm.Value)} mm");
				if (parse.Menge.HasValue)    gefunden.Add($"Menge: {parse.Menge.Value}");

				AusgewaehltePdfTextBlock.Text = $"✔  {Path.GetFileName(pdf)}";
				ZusammenfassungTextBlock.Text  = $"✔  Aus PDF ermittelt: {string.Join("  |  ", gefunden)}  –  Bitte prüfen und ggf. korrigieren.";
			}
			else
			{
				AusgewaehltePdfTextBlock.Text = $"📄  {Path.GetFileName(pdf)}";
				ZusammenfassungTextBlock.Text  = $"⚠  PDF geladen, aber {parse.Fehlermeldung}  –  Werte bitte manuell eingeben.";
			}
		}

		private static bool IsPdfDrag(DragEventArgs e) =>
			e.Data.GetDataPresent(DataFormats.FileDrop) &&
			e.Data.GetData(DataFormats.FileDrop) is string[] files &&
			files.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

		private void OnPdfZuPositionDrop(object sender, DragEventArgs e)
		{
			if (sender is not Border border || border.Tag is not RohrZuschnittEingabePosition position)
				return;

			if (!IsPdfDrag(e))
				return;

			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			var pdf = files?.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
			if (string.IsNullOrEmpty(pdf))
				return;

			position.PdfPfad = pdf;
			PositionenGrid.Items.Refresh();
			ZusammenfassungTextBlock.Text = $"✔  PDF für '{position.Bezeichnung}' gesetzt: {Path.GetFileName(pdf)}";
			e.Handled = true;
		}

		private void OnPositionLoeschen(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not RohrZuschnittEingabePosition position)
				return;

			if (_positionen.Remove(position))
			{
				PositionenGrid.Items.Refresh();
				ZusammenfassungTextBlock.Text = $"Position {position.Bezeichnung} entfernt.";
			}
		}

		private void OnRohrHinzufuegen(object sender, RoutedEventArgs e)
		{
			var bezeichnung = (BezeichnungTextBox.Text ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(bezeichnung) && ZeichnungComboBox.SelectedItem is string drawing)
				bezeichnung = drawing;

			if (string.IsNullOrWhiteSpace(bezeichnung))
			{
				MessageBox.Show("Bitte eine Rohrbezeichnung eingeben oder eine Zeichnung auswählen.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			if (!TryParseDouble(PositionLaengeTextBox.Text, out var laenge) || laenge <= 0)
			{
				MessageBox.Show("Bitte eine gültige Länge eingeben.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(PositionMengeTextBox.Text?.Trim(), out var menge) || menge <= 0)
			{
				MessageBox.Show("Bitte eine gültige Menge eingeben.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!TryParseWinkel(WinkelLinksTextBox.Text, out var winkelLinks))
			{
				MessageBox.Show("Winkel links muss zwischen 1 und 90 Grad liegen.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!TryParseWinkel(WinkelRechtsTextBox.Text, out var winkelRechts))
			{
				MessageBox.Show("Winkel rechts muss zwischen 1 und 90 Grad liegen.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			_positionen.Add(new RohrZuschnittEingabePosition
			{
				Bezeichnung     = bezeichnung,
				LaengeMm        = laenge,
				Menge           = menge,
				WinkelLinksGrad  = winkelLinks,
				WinkelRechtsGrad = winkelRechts,
				PdfPfad         = string.IsNullOrWhiteSpace(_selectedPdfPath) ? string.Empty : _selectedPdfPath
			});

			PositionenGrid.Items.Refresh();
			ZusammenfassungTextBlock.Text = $"{_positionen.Count} Position(en) erfasst. Weitere Rohre können hinzugefügt werden.";
		}

		private void OnBerechnen(object sender, RoutedEventArgs e)
		{
			if (_positionen.Count == 0)
			{
				MessageBox.Show("Bitte zuerst mindestens ein Rohr hinzufügen.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			if (!TryParseDouble(StangenLaengeTextBox.Text, out var stangenLaenge) || stangenLaenge <= 0)
			{
				MessageBox.Show("Ungültige Stangenlänge.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!TryParseDouble(SchnittverlustTextBox.Text, out var schnittverlust) || schnittverlust < 0)
			{
				MessageBox.Show("Ungültiger Schnittverlust.", "Rohrezuschnitt", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var cfg = new RohrZuschnittEinstellung
			{
				StandardStangenLaengeMm        = stangenLaenge,
				SaegeSchnittverlustMm          = schnittverlust,
				GehrungsZuschlagPro45GradEndeMm = 2
			};

			var result = RohrZuschnittService.BerechneFuerPositionen(_positionen, cfg);
			if (result.TeilAnzahl == 0)
			{
				ZusammenfassungTextBlock.Text = "Für die erfassten Positionen wurden keine gültigen Rohrzuschnitte gefunden.";
				return;
			}

			var ergebnisDialog = new RohrZuschnittErgebnisDialog(result, schnittverlust)
			{
				Owner = this
			};
			ergebnisDialog.ShowDialog();
		}

		/// <summary>Parst einen Winkelwert (1–90 Grad). Gibt 90 zurück wenn leer.</summary>
		private static bool TryParseWinkel(string? text, out double grad)
		{
			grad = 90;
			if (string.IsNullOrWhiteSpace(text))
				return true; // leer = 90° gerade

			if (!TryParseDouble(text, out var v))
				return false;

			if (v < 1 || v > 90)
				return false;

			grad = v;
			return true;
		}

		private static bool TryParseDouble(string? text, out double value)
		{
			value = 0;
			if (string.IsNullOrWhiteSpace(text))
				return false;

			var trimmed = text.Trim();
			if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out value))
				return true;

			return double.TryParse(trimmed.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
		}

		private void OnWindowClosing(object? sender, CancelEventArgs e)
		{
			if (_isNavigating)
				return;

			if (ReferenceEquals(Application.Current.MainWindow, this))
			{
				e.Cancel = true;
				NavigateToStartMode();
			}
		}

		private void OnZurStartseiteClick(object sender, RoutedEventArgs e)
		{
			NavigateToStartMode();
		}

		private void OnZurHauptprogrammClick(object sender, RoutedEventArgs e)
		{
			NavigateToMainWindow();
		}

		private void NavigateToStartMode()
		{
			_isNavigating = true;
			WindowNavigationService.NavigateToStart(this);
		}

		private void NavigateToMainWindow()
		{
			_isNavigating = true;
			WindowNavigationService.NavigateToMain(this);
		}
	}
}

