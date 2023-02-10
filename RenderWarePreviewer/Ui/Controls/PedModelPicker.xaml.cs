using RenderWarePreviewer.Models;
using RenderWarePreviewer.Scenes;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for PedModelPicker.xaml
/// </summary>
public partial class PedModelPicker : UserControl
{
    private SceneManager? sceneManager;
    public SceneManager? SceneManager
    {
        get => this.sceneManager;
        set
        {
            if (this.sceneManager != null)
                this.sceneManager.GtaDirectoryChanged -= RefreshSkins;

            this.sceneManager = value;

            if (this.sceneManager != null)
                this.sceneManager.GtaDirectoryChanged += RefreshSkins;
        }
    }

    public PedModelPicker()
    {
        InitializeComponent();
    }

    public void RefreshSkins(SceneManager manager, string directory)
    {
        var peds = this.SceneManager?.GetDefinedPeds();
        if (peds == null)
            return;

        this.SkinComboBox.Items.Clear();
        foreach (var ped in peds)
            this.SkinComboBox.Items.Add(new ComboBoxItem() { Content = $"{ped.Id} {ped.ModelName}", Tag = GtaModel.Create(ped) });

        this.SkinComboBox.SelectedIndex = 0;
    }

    private void SelectSkin(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = this.SkinComboBox.SelectedItem as ComboBoxItem;
        var model = selectedItem?.Tag as GtaModel;

        if (model == null)
            return;

        try
        {
            this.sceneManager?.LoadModel(model);
            this.SceneManager?.RequestRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
