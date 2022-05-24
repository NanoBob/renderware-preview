using RenderWareIo.Structs.Ide;
using RenderWarePreviewer.Helpers;
using RenderWarePreviewer.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RenderWarePreviewer
{
    public partial class MainWindow : Window
    {
        private readonly HashSet<Key> keysDown;
        private readonly SceneManager sceneManager;

        private bool isMovingCamera;
        private System.Windows.Point lastMousePos;

        private FileSystemWatcher? watcher;

        public MainWindow()
        {
            InitializeComponent();

            this.keysDown = new();
            this.sceneManager = new();

            this.SetGtaFilepath(GtaDirectoryHelper.GetGtaDirectory());


            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (s, e) => HandleCameraMovement();
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(16);
            dispatcherTimer.Start();
        }

        public void HandleMouseMovement(object sender, MouseEventArgs e)
        {
            if (!this.isMovingCamera)
                return;

            var current = e.GetPosition(this);
            this.sceneManager.RotateCamera(new Vector2((float)(current.X - this.lastMousePos.X), -(float)(current.Y - this.lastMousePos.Y)) * 0.01f);
            this.lastMousePos = current;
        }

        public void HandleMouseScroll(object sender, MouseWheelEventArgs e)
        {
            this.sceneManager.ZoomCamera(e.Delta * -0.1f);
        }

        public void StartCameraMovement(object sender, MouseEventArgs e)
        {
            this.lastMousePos = e.GetPosition(this);
            this.isMovingCamera = true;
        }

        public void StopCameraMovement(object sender, MouseEventArgs e)
        {
            if (sender == this.EventSurface)
                this.isMovingCamera = false;
        }

        private void HandleCameraMovement()
        {
            if (!this.isMovingCamera)
                return;

            var speed = this.keysDown.Contains(Key.Space) ? .15f : .05f;
            this.sceneManager.MoveCamera(speed * new Vector3(
                ((this.keysDown.Contains(Key.A) ? -1 : 0) + (this.keysDown.Contains(Key.D) ? 1 : 0)),
                ((this.keysDown.Contains(Key.W) ? 1 : 0) + (this.keysDown.Contains(Key.S) ? -1 : 0)),
                ((this.keysDown.Contains(Key.LeftShift) ? 1 : 0) + (this.keysDown.Contains(Key.LeftCtrl) ? -1 : 0))
            ));
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            this.keysDown.Add(e.Key);
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            this.keysDown.Remove(e.Key);
        }


        public void SetGtaFilepath(string? path)
        {
            try
            {
                this.GtaDirectoryLabel.Content = path;

                if (path != null)
                {
                    GtaDirectoryHelper.StoreGtaDirectory(path);
                    this.sceneManager.LoadGta(path);

                    var skins = this.sceneManager.GetSkinNames();

                    this.SkinComboBox.Items.Clear();
                    foreach (var skin in skins)
                        this.SkinComboBox.Items.Add(new ComboBoxItem() { Content = $"{skin.Id} {skin.ModelName}" , Tag = skin });

                    this.SkinComboBox.SelectedIndex = 0;
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

        private void SelectSkin(object sender, SelectionChangedEventArgs e)
        {
            var ped = (Ped)(this.SkinComboBox.SelectedItem as ComboBoxItem)!.Tag;
            var textures = this.sceneManager.GetTextures(ped);

            this.TextureComboBox.Items.Clear();
            foreach (var texture in textures)
                this.TextureComboBox.Items.Add(new ComboBoxItem() { Content = texture, Tag = texture.Trim('\0') });

            this.TextureComboBox.SelectedIndex = 0;
            LoadSkin((Ped)(this.SkinComboBox.SelectedItem as ComboBoxItem)!.Tag, ((this.TextureComboBox.SelectedItem as ComboBoxItem)?.Tag as string) ?? "");
        }

        private void SelectTexture(object sender, SelectionChangedEventArgs e)
        {
            LoadSkin((Ped)(this.SkinComboBox.SelectedItem as ComboBoxItem)!.Tag, ((this.TextureComboBox.SelectedItem as ComboBoxItem)?.Tag as string) ?? "");
        }

        private void LoadSkin(Ped ped, string texture)
        {
            try
            {
                this.sceneManager.LoadModel(ped, texture);
                this.sceneManager.ApplyTo(this.ViewPort);
                UpdateTexturePreview(ped, texture);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateTexturePreview(Ped ped, string texture)
        {
            var image = this.sceneManager.GetImage(ped, texture);
            UpdateTexturePreview(ped, image);
        }

        private void UpdateTexturePreview(Ped ped, Image<Rgba32> image)
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
            var ped = (Ped)(this.SkinComboBox.SelectedItem as ComboBoxItem)!.Tag;
            var texture = ((this.TextureComboBox.SelectedItem as ComboBoxItem)?.Tag as string) ?? "";

            var dialog = new System.Windows.Forms.SaveFileDialog()
            {
                FileName = texture + ".png"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var image = this.sceneManager.GetImage(ped, texture);
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

            this.ImagePathLabel.Content = dialog.FileName;
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
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdatePedTexture(string file)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var image = SixLabors.ImageSharp.Image.Load<Rgba32>(file);
                var ped = (Ped)(this.SkinComboBox.SelectedItem as ComboBoxItem)!.Tag;
                this.sceneManager.LoadModel(ped, image);
                this.sceneManager.ApplyTo(this.ViewPort);

                UpdateTexturePreview(ped, image);
            });
        }
    }
}
