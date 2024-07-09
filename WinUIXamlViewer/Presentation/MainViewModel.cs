using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Windowing;
using Uno;
using Uno.Extensions;
using Windows.Storage.Pickers;

namespace WinUIXamlViewer.Presentation;

public partial class MainViewModel : ObservableObject
{
    private INavigator _navigator;
    private readonly IDispatcher dispatcher;
    private readonly Window window;
    private readonly IConfiguration configuration;

    private long count = 0;
    private readonly string _defXaml;


    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private bool isTop = false;

    [ObservableProperty]
    private ObservableCollection<WorkViewModel> workModels = new ObservableCollection<WorkViewModel>();

    [ObservableProperty]
    private WorkViewModel selectedWorkModel;

    public bool IsLoadAssembly => !OperatingSystem.IsBrowser();

    public MainViewModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator
        , IDispatcher dispatcher
        , Window window
        , IConfiguration configuration)
    {
        _navigator = navigator;
        this.dispatcher = dispatcher;
        this.window = window;
        this.configuration = configuration;

        _defXaml = configuration.GetValue<string>("files.DefaultXaml");

        ToggleTopCommand = new AsyncRelayCommand(ToggleTop);
        LoadAssemblyCommand = new AsyncRelayCommand(LoadAssembly);
        AddItemCommand = new AsyncRelayCommand(AddItem);
        RemoveItemCommand = new AsyncRelayCommand<TabViewTabCloseRequestedEventArgs>(RemoveItem);
        ReaderXamlCommand = new AsyncRelayCommand(ReaderXaml);
        OpenXamlFileCommand = new AsyncRelayCommand(OpenXamlFile);


        _ = this.AddItem();

    }

    public ICommand ToggleTopCommand { get; }
    public ICommand LoadAssemblyCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ReaderXamlCommand { get; }
    public ICommand OpenXamlFileCommand { get; }


    public async Task AddItem()
    {
        var i = ++count;
        var entity = new WorkEntity($"新建 {i}", false, string.Empty, _defXaml);
        var vm = new WorkViewModel(this.dispatcher, entity);
        this.WorkModels.Add(vm);
        this.SelectedWorkModel = vm;
    }

    public async Task RemoveItem(TabViewTabCloseRequestedEventArgs args)
    {
        this.WorkModels.Remove(args.Item as WorkViewModel);
    }


    public async Task ToggleTop()
    {
        var top = this.IsTop;

        await dispatcher.ExecuteAsync(() =>
        {
            var p = OverlappedPresenter.Create();
            p.IsAlwaysOnTop = !top;
            window.AppWindow.SetPresenter(p);

        });

        this.IsTop = !top;
    }


    public async Task LoadAssembly()
    {

        // Create a file picker
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

        // See the sample code below for how to make the window accessible from the App class.
        //var window = App.MainWindow;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your file picker
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = OperatingSystem.IsWindows() ? PickerLocationId.DocumentsLibrary : PickerLocationId.Unspecified;
        openPicker.FileTypeFilter.Add(".dll");



        // Open the picker for the user to pick a file
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var dll = Path.Combine(path, "plugins", file.Name);
            File.Copy(file.Path, dll);

            Assembly.LoadFile(dll);
        }
    }


    public async Task ReaderXaml()
    {
        var vm = this.SelectedWorkModel;
        if (vm == null) return;

        await vm.ReaderXaml();
    }

    public async Task OpenXamlFile()
    {

        // Create a file picker
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

        // See the sample code below for how to make the window accessible from the App class.
        //var window = App.MainWindow;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your file picker
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;

#if DESKTOP
        openPicker.SuggestedStartLocation = PickerLocationId.Unspecified;
#else 

        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
#endif
        //openPicker.SuggestedStartLocation = OperatingSystem.IsWindows() ? PickerLocationId.DocumentsLibrary : PickerLocationId.Unspecified;
        openPicker.FileTypeFilter.Add(".xaml");



        // Open the picker for the user to pick a file
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            var entity = new WorkEntity(file.Name, true, file.Path, string.Empty);
            var vm = new WorkViewModel(this.dispatcher, entity);
            this.WorkModels.Add(vm);
            this.SelectedWorkModel = vm;
            await this.ReaderXaml();
        }

    }


}
