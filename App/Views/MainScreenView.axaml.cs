using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Fcry.App.ViewModels;

namespace Fcry.App.Views;

public partial class MainScreenView : UserControl
{
    public MainScreenView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        DragDrop.SetAllowDrop(DropZone, true);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        RemoveHandler(DragDrop.DropEvent, OnDrop);
        RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
        RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        base.OnUnloaded(e);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
        SetDragOver(true);
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        SetDragOver(false);
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        SetDragOver(false);
        e.Handled = true;

        if (DataContext is not MainScreenViewModel vm) return;
        var files = e.Data.GetFiles();
        if (files == null) return;

        var paths = files
            .Select(f => f.Path.LocalPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (paths.Count > 0)
            await vm.EnqueueFilesAsync(paths);
    }

    private void SetDragOver(bool value)
    {
        if (DataContext is MainScreenViewModel vm)
            vm.IsDragOver = value;

        if (DropZone != null)
        {
            if (value)
                DropZone.Classes.Add("dragover");
            else
                DropZone.Classes.Remove("dragover");
        }
    }
}
