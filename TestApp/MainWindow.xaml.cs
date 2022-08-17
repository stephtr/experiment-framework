using Microsoft.UI.Xaml;
using ExperimentFramework;

namespace TestApp
{
    public sealed partial class MainWindow : Window
    {
        public ExperimentContainer Components = new();

        public MainWindow()
        {
            this.InitializeComponent();

            Components.AddComponentClass<LaserComponent>();
            Components.AddComponentClass<StageComponent>();
            Components.AddComponent<FakeLaser>();
        }
    }
}
