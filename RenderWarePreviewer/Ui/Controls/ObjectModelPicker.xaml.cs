using RenderWareIo.Structs.Ide;
using RenderWarePreviewer.Models;
using RenderWarePreviewer.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for ObjectModelPicker.xaml
/// </summary>
public partial class ObjectModelPicker : UserControl
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

    private IEnumerable<Obj> objects = Array.Empty<Obj>();

    public ObjectModelPicker()
    {
        InitializeComponent();
    }

    public void RefreshModels(SceneManager manager, string directory)
    {
        var objects = this.SceneManager?.GetDefinedObjects();
        if (objects == null)
            return;

        this.objects = objects;

        HandleSearch();
    }

    private void SelectModel(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = this.SearchResults.SelectedItem as ListBoxItem;
        var model = selectedItem?.Tag as GtaModel;

        if (model == null || this.sceneManager == null)
            return;

        try
        {
            this.sceneManager.RenderBackground = false;
            this.sceneManager.RotatesObjects = false;
            this.sceneManager.LoadModel(model);
            this.SceneManager.RequestRender();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        HandleSearch();
    }

    private void HandleSearch()
    {
        var query = this.Search.Text ?? "";

        var objects = this.objects.Where(x => 
            query == "" || 
            x.ModelName.Contains(query) || 
            x.Id.ToString().Contains(query)
        );


        this.SearchResults.Items.Clear();
        foreach (var obj in objects)
            this.SearchResults.Items.Add(new ListBoxItem()
            {
                Content = $"{obj.Id} {obj.ModelName}",
                Tag = GtaModel.Create(obj)
            });

        this.SearchResults.SelectedIndex = 0;
    }
}
