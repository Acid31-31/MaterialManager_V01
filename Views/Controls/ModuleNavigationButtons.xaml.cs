using System.Windows;
using System.Windows.Controls;

namespace MaterialManager_V01.Views.Controls
{
    public partial class ModuleNavigationButtons : UserControl
    {
        public static readonly RoutedEvent StartseiteClickEvent =
            EventManager.RegisterRoutedEvent(nameof(StartseiteClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModuleNavigationButtons));

        public static readonly RoutedEvent HauptprogrammClickEvent =
            EventManager.RegisterRoutedEvent(nameof(HauptprogrammClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModuleNavigationButtons));

        public static readonly DependencyProperty ShowHauptprogrammProperty =
            DependencyProperty.Register(nameof(ShowHauptprogramm), typeof(bool), typeof(ModuleNavigationButtons),
                new PropertyMetadata(true, OnShowHauptprogrammChanged));

        public event RoutedEventHandler StartseiteClick
        {
            add => AddHandler(StartseiteClickEvent, value);
            remove => RemoveHandler(StartseiteClickEvent, value);
        }

        public event RoutedEventHandler HauptprogrammClick
        {
            add => AddHandler(HauptprogrammClickEvent, value);
            remove => RemoveHandler(HauptprogrammClickEvent, value);
        }

        public bool ShowHauptprogramm
        {
            get => (bool)GetValue(ShowHauptprogrammProperty);
            set => SetValue(ShowHauptprogrammProperty, value);
        }

        public ModuleNavigationButtons()
        {
            InitializeComponent();
            UpdateHauptprogrammVisibility();
        }

        private static void OnShowHauptprogrammChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ModuleNavigationButtons control)
                control.UpdateHauptprogrammVisibility();
        }

        private void UpdateHauptprogrammVisibility()
        {
            HauptprogrammButton.Visibility = ShowHauptprogramm ? Visibility.Visible : Visibility.Collapsed;
            StartseiteButton.Margin = ShowHauptprogramm ? new Thickness(0, 0, 10, 0) : new Thickness(0);
        }

        private void OnStartseiteClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StartseiteClickEvent, this));
        }

        private void OnHauptprogrammClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(HauptprogrammClickEvent, this));
        }
    }
}
