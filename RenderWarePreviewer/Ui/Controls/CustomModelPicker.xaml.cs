using RenderWarePreviewer.Models;
using RenderWarePreviewer.Scenes;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for CustomModelPicker.xaml
/// </summary>
public partial class CustomModelPicker : UserControl
{
    public SceneManager? SceneManager { get; set; }

    private byte[]? selectedDff;
    private byte[]? selectedTxd;

    public CustomModelPicker()
    {
        InitializeComponent();
    }

    private void SelectDff(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.OpenFileDialog()
        {
            Title = "Select .dff file",
            Filter = "Dff files|*.dff"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        try
        {
            this.selectedDff = File.ReadAllBytes(dialog.FileName);
            this.DffLabel.Content = dialog.FileName;
            UpdateModelIfPossible();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "An error occured");
#if DEBUG
            throw;
#endif
        }
    }

    private void SelectTxd(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.OpenFileDialog()
        {
            Title = "Select .txd file",
            Filter = "Txd files|*.txd"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        try
        {
            this.selectedTxd = File.ReadAllBytes(dialog.FileName);
            this.TxdLabel.Content = dialog.FileName;
            UpdateModelIfPossible();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "An error occured");
#if DEBUG
            throw;
#endif
        }
    }


    private void UpdateModelIfPossible()
    {
        if (this.SceneManager == null)
            return;

        if (this.selectedDff == null || this.selectedTxd == null)
            return;

        var model = CustomGtaModel.Create(this.selectedDff, this.selectedTxd);

        this.SceneManager.RenderBackground = false;
        this.SceneManager.RotatesObjects = false;
        this.SceneManager.LoadModel(model, true);
        this.SceneManager.RequestRender();
    }
}
