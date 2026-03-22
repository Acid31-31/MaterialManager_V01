using MaterialManager_V01.Models;
using System.Collections.Generic;
using System.Windows;

namespace MaterialManager_V01.Views
{
    public partial class ResteAuswahlDialog : Window
    {
        public MaterialItem? SelectedMaterial { get; private set; }

        public ResteAuswahlDialog(IEnumerable<MaterialItem> items)
        {
            InitializeComponent();
            ResteGrid.ItemsSource = items;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            if (ResteGrid.SelectedItem is MaterialItem item)
            {
                SelectedMaterial = item;
                DialogResult = true;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnOk(sender, e);
        }
    }
}
