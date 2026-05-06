# Dreamine.Logging.Wpf

Dreamine.Logging.Wpf는 Dreamine 애플리케이션을 위한 WPF 전용 로그 UI 패키지입니다.
`Dreamine.Logging`의 로그 파이프라인을 WPF 화면에 연결하기 위해 로그 패널 View, ViewModel, UI Dispatcher를 제공합니다.

[➡️ English Version](./README.md)

## 목적

이 패키지는 WPF 표시 계층과 UI 스레드 연동만 담당합니다.
핵심 로그 파이프라인은 `Dreamine.Logging` 패키지가 담당합니다.

## 주요 기능

- WPF 로그 표시용 `DreamineLogPanelView`
- 로그 엔트리 바인딩용 `DreamineLogPanelViewModel`
- UI 스레드 안전 갱신을 위한 `WpfLogUiDispatcher`
- `IDreamineLogStore.LogAdded` 이벤트 기반 실시간 로그 표시
- 선택된 로그의 상세 내용 표시

## 기본 구조

```text
Dreamine.Logging
  -> InMemoryLogStore
     -> DreamineLogPanelViewModel
        -> DreamineLogPanelView
```

## XAML 사용 예시

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

## ViewModel 래퍼 예시

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

## 등록 예시

```csharp
DMContainer.RegisterSingleton<WpfLogUiDispatcher>(
    new WpfLogUiDispatcher());

DMContainer.Register<DreamineLogPanelViewModel>(() =>
    new DreamineLogPanelViewModel(
        DMContainer.Resolve<IDreamineLogStore>(),
        DMContainer.Resolve<WpfLogUiDispatcher>()));
```

## 주의 사항

`DreamineLogPanelView`는 `DreamineLogPanelViewModel`을 `DataContext`로 받아야 합니다.
다른 페이지 내부에 로그 패널을 감싸서 사용할 경우 내부 View의 `DataContext`를 명시적으로 연결해야 합니다.

```xml
<logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
```

상세 로그 텍스트는 읽기 전용 속성이므로 `TextBox.Text` 바인딩에는 `Mode=OneWay`를 사용해야 합니다.

```xml
<TextBox Text="{Binding SelectedDetailText, Mode=OneWay}" />
```

## Dreamine.Logging과의 관계

`Dreamine.Logging.Wpf`는 `Dreamine.Logging`에 의존합니다.
반대로 `Dreamine.Logging`은 WPF를 알면 안 됩니다.

```text
Dreamine.Logging.Wpf
  -> Dreamine.Logging
```

## 향후 계획

- `DMContainer.UseDreamineLoggingWpf()`
- `DMContainer.UseDreamineLoggingForWpf(...)`
- 자동 스크롤 지원
- Clear 버튼
- 로그 레벨 필터
- 검색/필터 UI
- 선택 로그 내보내기

## 라이선스

MIT License
