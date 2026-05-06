# Dreamine.Logging.Wpf

Dreamine.Logging.Wpf provides WPF-specific logging UI components for Dreamine applications.
It integrates `Dreamine.Logging` with WPF through a log panel view, log panel view model, and UI dispatcher support.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

This package is responsible only for WPF presentation and UI-thread integration.
The core logging pipeline is provided by `Dreamine.Logging`.

## Features

- `DreamineLogPanelView` for displaying logs in WPF
- `DreamineLogPanelViewModel` for binding log entries to the UI
- `WpfLogUiDispatcher` for safe UI thread updates
- Real-time log display through `IDreamineLogStore.LogAdded`
- Log detail display for selected entries

## Basic Architecture

```text
Dreamine.Logging
  -> InMemoryLogStore
     -> DreamineLogPanelViewModel
        -> DreamineLogPanelView
```

## Example XAML Usage

```xml
<UserControl x:Class="SampleSmart.Pages.PageSub.PageLog"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:logViews="clr-namespace:Dreamine.Logging.Wpf.Views;assembly=Dreamine.Logging.Wpf">
    <Grid>
        <logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
    </Grid>
</UserControl>
```

## Example ViewModel Wrapper

```csharp
public sealed class PageLogViewModel : ViewModelBase
{
    public DreamineLogPanelViewModel LogPanel { get; }

    public PageLogViewModel(DreamineLogPanelViewModel logPanel)
    {
        LogPanel = logPanel;
    }
}
```

## Example Registration

```csharp
DMContainer.RegisterSingleton<WpfLogUiDispatcher>(
    new WpfLogUiDispatcher());

DMContainer.Register<DreamineLogPanelViewModel>(() =>
    new DreamineLogPanelViewModel(
        DMContainer.Resolve<IDreamineLogStore>(),
        DMContainer.Resolve<WpfLogUiDispatcher>()));
```

## Important Notes

`DreamineLogPanelView` must receive a `DreamineLogPanelViewModel` as its `DataContext`.
When wrapping the log panel inside another page, bind the inner view's `DataContext` explicitly.

```xml
<logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
```

The detail text binding should use `Mode=OneWay` because the detail text is read-only.

```xml
<TextBox Text="{Binding SelectedDetailText, Mode=OneWay}" />
```

## Relationship with Dreamine.Logging

`Dreamine.Logging.Wpf` depends on `Dreamine.Logging`.
`Dreamine.Logging` must not depend on WPF.

```text
Dreamine.Logging.Wpf
  -> Dreamine.Logging
```

## Future Roadmap

- `DMContainer.UseDreamineLoggingWpf()`
- `DMContainer.UseDreamineLoggingForWpf(...)`
- Auto-scroll support
- Clear button
- Log level filter
- Search/filter UI
- Export selected logs

## License

MIT License
