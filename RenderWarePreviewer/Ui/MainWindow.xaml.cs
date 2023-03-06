using RenderWarePreviewer.Scenes;
using System.Windows;
using System.Windows.Input;

namespace RenderWarePreviewer.Ui
{
    public partial class MainWindow : Window
    {
        private readonly SceneManager sceneManager;

        public MainWindow()
        {
            InitializeComponent();

            this.sceneManager = new();
            this.SceneRenderer.SceneManager = this.sceneManager;

            this.CustomModelPicker.SceneManager = this.sceneManager;
            this.VehicleModelPicker.SceneManager = this.sceneManager;
            this.ObjectModelPicker.SceneManager = this.sceneManager;
            this.WeaponModelPicker.SceneManager = this.sceneManager;
            this.PedModelPicker.SceneManager = this.sceneManager;

            this.ModelDetail.SceneManager = this.sceneManager;

            this.GtaDirectoryPicker.SceneManager = this.sceneManager;
            this.GtaDirectoryPicker.Init();
        }

        public void HandleKeyDownPreview(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                this.SceneRenderer.HandleKeyDown(sender, e);
            }
        }

        public void HandleKeyDown(object sender, KeyEventArgs e)
        {
            this.SceneRenderer.HandleKeyDown(sender, e);
        }

        public void HandleKeyUp(object sender, KeyEventArgs e)
        {
            this.SceneRenderer.HandleKeyUp(sender, e);
        }
    }
}
