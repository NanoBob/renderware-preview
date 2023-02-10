using RenderWarePreviewer.Scenes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RenderWarePreviewer.Ui.Controls;

/// <summary>
/// Interaction logic for SceneRenderer.xaml
/// </summary>
public partial class SceneRenderer : UserControl
{
    private readonly HashSet<Key> keysDown;

    private SceneManager? sceneManager;
    public SceneManager? SceneManager
    {
        get => this.sceneManager;
        set
        {
            if (this.sceneManager != null)
                this.sceneManager.RenderRequested -= Render;

            this.sceneManager = value;

            if (this.sceneManager != null)
                this.sceneManager.RenderRequested += Render;
        }
    }

    private bool isMovingCamera;
    private Point lastMousePos;

    public SceneRenderer()
    {
        InitializeComponent();

        this.keysDown = new();


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
        this.SceneManager?.RotateCamera(new Vector2((float)(current.X - this.lastMousePos.X), -(float)(current.Y - this.lastMousePos.Y)) * 0.01f);
        this.lastMousePos = current;
    }

    public void HandleMouseScroll(object sender, MouseWheelEventArgs e)
    {
        this.SceneManager?.ZoomCamera(e.Delta * -0.1f);
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
        this.SceneManager?.MoveCamera(speed * new Vector3(
            ((this.keysDown.Contains(Key.A) ? -1 : 0) + (this.keysDown.Contains(Key.D) ? 1 : 0)),
            ((this.keysDown.Contains(Key.W) ? 1 : 0) + (this.keysDown.Contains(Key.S) ? -1 : 0)),
            ((this.keysDown.Contains(Key.LeftShift) ? 1 : 0) + (this.keysDown.Contains(Key.LeftCtrl) ? -1 : 0))
        ));
    }

    private void Render(SceneManager sceneManager)
    {
        sceneManager.ApplyTo(this.ViewPort);
    }

    public void HandleKeyDown(object sender, KeyEventArgs e)
    {
        this.keysDown.Add(e.Key);
    }

    public void HandleKeyUp(object sender, KeyEventArgs e)
    {
        this.keysDown.Remove(e.Key);
    }
}
