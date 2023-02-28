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
                this.sceneManager.GtaDirectoryChanged -= RefreshModels;

            this.sceneManager = value;

            if (this.sceneManager != null)
                this.sceneManager.GtaDirectoryChanged += RefreshModels;
        }
    }

    public PedModelPicker()
    {
        InitializeComponent();
    }

    public void RefreshModels(SceneManager manager, string directory)
    {
        var peds = this.SceneManager?.GetDefinedPeds();
        if (peds == null)
            return;

        this.SkinComboBox.Items.Clear();
        foreach (var ped in peds)
            this.SkinComboBox.Items.Add(new ComboBoxItem() { Content = $"{ped.Id} {ped.ModelName}", Tag = GtaModel.Create(ped) });

        this.SkinComboBox.SelectedIndex = 0;
    }

    private void SelectModel(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = this.SkinComboBox.SelectedItem as ComboBoxItem;
        var model = selectedItem?.Tag as GtaModel;

        if (model == null || this.sceneManager == null)
            return;

        try
        {
            this.sceneManager.RenderBackground = true;
            this.sceneManager.RotatesObjects = true;
            this.sceneManager.LoadModel(model);
            this.sceneManager.RequestRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
