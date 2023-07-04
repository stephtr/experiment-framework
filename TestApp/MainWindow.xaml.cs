using Microsoft.UI.Xaml;
using ExperimentFramework;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace TestApp
{
    public sealed partial class MainWindow : Window
    {
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
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
    }
}
