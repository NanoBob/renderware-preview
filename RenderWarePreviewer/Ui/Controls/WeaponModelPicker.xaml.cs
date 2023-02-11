using RenderWarePreviewer.Models;
using RenderWarePreviewer.Scenes;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for WeaponModelPicker.xaml
/// </summary>
public partial class WeaponModelPicker : UserControl
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

    public WeaponModelPicker()
    {
        InitializeComponent();
    }

    public void RefreshModels(SceneManager manager, string directory)
    {
        var weapons = this.SceneManager?.GetDefinedWeapons();
        if (weapons == null)
            return;

        this.WeaponComboBox.Items.Clear();
        foreach (var weapon in weapons)
            this.WeaponComboBox.Items.Add(new ComboBoxItem() { Content = $"{weapon.Id} {weapon.ModelName}", Tag = GtaModel.Create(weapon) });

        this.WeaponComboBox.SelectedIndex = 0;
    }

    private void SelectModel(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = this.WeaponComboBox.SelectedItem as ComboBoxItem;
        var model = selectedItem?.Tag as GtaModel;

        if (model == null || this.sceneManager == null)
            return;

        try
        {
            this.sceneManager.RenderBackground = true;
            this.sceneManager.RotatesObjects = false;
            this.sceneManager.LoadModel(model);
            this.SceneManager.RequestRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
