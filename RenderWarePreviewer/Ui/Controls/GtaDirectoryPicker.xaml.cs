using RenderWarePreviewer.Helpers;
using RenderWarePreviewer.Scenes;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for GtaDirectoryPicker.xaml
/// </summary>
public partial class GtaDirectoryPicker : UserControl
{
    public SceneManager? SceneManager { get; set; }

    public GtaDirectoryPicker()
    {
        InitializeComponent();
    }

    public void Init()
    {
        this.SetGtaFilepath(GtaDirectoryHelper.GetGtaDirectory());
    }

    public void SetGtaFilepath(string? path)
    {
        try
        {
            this.GtaDirectoryLabel.Text = path;

            if (path != null)
            {
                GtaDirectoryHelper.StoreGtaDirectory(path);
                this.SceneManager?.LoadGta(path);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void SelectGtaDirectory(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog()
        {
            Description = "Select GTA:SA Install directory"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        SetGtaFilepath(dialog.SelectedPath);
    }
}
