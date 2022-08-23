using Microsoft.UI.Xaml;
using ExperimentFramework;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace TestApp
{
    public sealed partial class MainWindow : Window
    {
        public MicaWindow MicaWindow;

        public MainWindow()
        {
            var Components = ExperimentContainer.Singleton;
            Components.AddComponentClass<LaserComponent>();
            Components.AddComponentClass<StageComponent>();
            Components.AddComponent<FakeLaser>();

            this.InitializeComponent();

            Components.UseWinUISettingsStore();
            Components.LoadFromSettings();

            Title = "TestApp";
            MicaWindow = this.EnableMica(useCustomTitlebar: true);
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            MicaWindow.Activate(args);
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            MicaWindow.Close();
        }
    }
}
