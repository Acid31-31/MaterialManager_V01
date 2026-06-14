using System.Windows;
using System.Windows.Controls;

namespace MaterialManager_V01.Views.Controls
{
    public partial class ModuleNavigationBar : UserControl
    {
        public static readonly RoutedEvent StartseiteClickEvent =
            EventManager.RegisterRoutedEvent(nameof(StartseiteClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModuleNavigationBar));

        public static readonly RoutedEvent HauptprogrammClickEvent =
            EventManager.RegisterRoutedEvent(nameof(HauptprogrammClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModuleNavigationBar));

        public static readonly DependencyProperty ModuleTitleProperty =
            DependencyProperty.Register(nameof(ModuleTitle), typeof(string), typeof(ModuleNavigationBar), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowHauptprogrammProperty =
            DependencyProperty.Register(nameof(ShowHauptprogramm), typeof(bool), typeof(ModuleNavigationBar), new PropertyMetadata(true));

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

        public string ModuleTitle
        {
            get => (string)GetValue(ModuleTitleProperty);
            set => SetValue(ModuleTitleProperty, value);
        }

        public bool ShowHauptprogramm
        {
            get => (bool)GetValue(ShowHauptprogrammProperty);
            set => SetValue(ShowHauptprogrammProperty, value);
        }

        public ModuleNavigationBar()
        {
            InitializeComponent();
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
