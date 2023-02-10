using RenderWareIo.Structs.Ide;
using RenderWarePreviewer.Models;
using RenderWarePreviewer.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for ModelDetail.xaml
/// </summary>
public partial class ModelDetail : UserControl
{
    private string SelectedTexture => ((this.TextureComboBox.SelectedItem as ComboBoxItem)?.Tag as string) ?? "";
    private GtaModel? loadedModel;

    private FileSystemWatcher? watcher;

    private SceneManager? sceneManager;
    public SceneManager? SceneManager
    {
        get => this.sceneManager;
        set
        {
            if (this.sceneManager != null)
                this.sceneManager.ModelLoaded -= HandleModelLoad;

            this.sceneManager = value;

            if (this.sceneManager != null)
                this.sceneManager.ModelLoaded += HandleModelLoad;
        }
    }

    public ModelDetail()
    {
        InitializeComponent();
    }

    private void HandleModelLoad(SceneManager sceneManager, GtaModel model)
    {
        if (model == this.loadedModel)
            return;

        this.loadedModel = model;
        var textures = this.SceneManager?.GetTextures(model);

        this.TextureComboBox.Items.Clear();
        foreach (var texture in textures ?? Array.Empty<string>())
            this.TextureComboBox.Items.Add(new ComboBoxItem() { Content = texture, Tag = texture.Trim('\0') });

        this.watcher?.Dispose();
        this.watcher = null;

        this.TextureComboBox.SelectedIndex = 0;
        UpdateTexturePreview(model, this.SelectedTexture);
    }

    private void SelectTexture(object sender, SelectionChangedEventArgs e)
    {
        if (this.loadedModel == null)
            return;

        UpdateTexturePreview(this.loadedModel, this.SelectedTexture);
    }

    private void UpdateTexturePreview(GtaModel model, string texture)
    {
        var image = this.SceneManager?.GetImage(model, texture);
        if (image != null)
            UpdateTexturePreview(model, image);
    }

    private void UpdateTexturePreview(GtaModel model, Image<Rgba32> image)
    {
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;

        BitmapImage bitmapImage = new();
        MemoryStream ms = new(stream.ToArray());
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = ms;
        bitmapImage.EndInit();

        this.Image.Source = bitmapImage;
    }

    public void ExportImage(object sender, RoutedEventArgs e)
    {
        var model = this.loadedModel;
        var texture = this.SelectedTexture;

        var dialog = new System.Windows.Forms.SaveFileDialog()
        {
            FileName = texture + ".png"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var image = this.SceneManager?.GetImage(model, texture);
        image.SaveAsPng(dialog.FileName);
    }

    public void SelectTargetFile(object sender, RoutedEventArgs e)
    {
        if (this.watcher != null)
            this.watcher.Dispose();

        var dialog = new System.Windows.Forms.OpenFileDialog()
        {
            CheckFileExists = false,

        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        this.ImagePathLabel.Text = dialog.FileName;
        UpdatePedTexture(dialog.FileName);

        this.watcher = new FileSystemWatcher()
        {
            NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size,
            Path = Path.GetDirectoryName(dialog.FileName)!,
            Filter = Path.GetFileName(dialog.FileName),
            EnableRaisingEvents = true,
        };
        this.watcher.Changed += HandleFileChange;
    }

    private void HandleFileChange(object sender, FileSystemEventArgs e)
    {
        try
        {
            UpdatePedTexture(e.FullPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void UpdatePedTexture(string file)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (this.loadedModel == null)
                return;

            var image = SixLabors.ImageSharp.Image.Load<Rgba32>(file);
            this.SceneManager?.LoadModel(this.loadedModel, image, this.SelectedTexture);

            UpdateTexturePreview(this.loadedModel, image);
        });
    }
}
