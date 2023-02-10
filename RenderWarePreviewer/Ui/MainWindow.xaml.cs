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
            this.PedModelPicker.SceneManager = this.sceneManager;
            this.ModelDetail.SceneManager = this.sceneManager;
            this.GtaDirectoryPicker.SceneManager = this.sceneManager;
            this.GtaDirectoryPicker.Init();
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
