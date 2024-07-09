using System.Collections.ObjectModel;
using System.Dynamic;
using System.Text.Json;
using Uno.Extensions.Equality;

namespace WinUIXamlViewer.Presentation;

public partial class WorkViewModel : ObservableObject
{
    private readonly IDispatcher dispatcher;
    private readonly WorkEntity entity;
    private FileSystemWatcher watcher;

    private double hideEditerWidth = 400;
    private double hideJsonWidth = 300;
    private long lastWriteTimeTicks = 0;
    private object dataContext;

    private ShowEntity cache;

    public WorkViewModel(IDispatcher dispatcher, WorkEntity entity)
    {
        this.dispatcher = dispatcher;
        this.entity = entity;

        if (entity.IsFile)
        {
            _ = this.ReadFileAsync();
            _ = this.Watcher();
        }

        _ = Task.Delay(500).ContinueWith(async (a) => await this.ReaderXaml());


        this.SelectedShowEntity = this.ShowEntities.FirstOrDefault();
    }

    public IconSource Icon => new SymbolIconSource() { Symbol = this.entity.IsFile ? Symbol.OpenFile : Symbol.Document };

    public string Title
    {
        get => this.entity.Title;
        set => SetProperty(this.entity.Title, value, this.entity, (e, n) => e.Title = n);
    }

    public bool IsFile
    {
        get => this.entity.IsFile;
        set => SetProperty(this.entity.IsFile, value, this.entity, (e, n) => e.IsFile = n);
    }
    public string Path
    {
        get => this.entity.Path;
        set => SetProperty(this.entity.Path, value, this.entity, (e, n) => e.Path = n);
    }
    public string EditText
    {
        get => this.entity.Text;
        set => SetProperty(this.entity.Text, value, this.entity, (e, n) =>
        {
            e.Text = n;
            EditTextChanged(n);
        });
    }

    public string JsonText
    {
        get => this.entity.Json;
        set => SetProperty(this.entity.Json, value, this.entity, (e, n) =>
        {
            e.Json = n;
            JsonTextChanged(n);
        });
    }

    [ObservableProperty]
    private GridLength editerTextWidth = new GridLength(400, GridUnitType.Pixel);

    [ObservableProperty]
    private bool isShow = true;

    [ObservableProperty]
    private FrameworkElement? designContent;

    [ObservableProperty]
    private int contentHeight = 550;

    [ObservableProperty]
    private int contentWidth = 375;

    [ObservableProperty]
    private bool sizeIsEnabled = false;

    [ObservableProperty]
    private GridLength jsonTextWidth = new GridLength(300, GridUnitType.Pixel);

    [ObservableProperty]
    private ObservableCollection<ShowEntity> showEntities = new ObservableCollection<ShowEntity>()
    {
        new ShowEntity("响应",-1,-1,false),
        new ShowEntity("iPhone SE",375,667),
        new ShowEntity("iPhone XR",414,896),
        new ShowEntity("iPhone 12 Pro",390,844),
        new ShowEntity("iPhone 14 Pro Max",430,932),
        new ShowEntity("Pixel 7",412,915),
    };

    [ObservableProperty]
    private ShowEntity selectedShowEntity;

    [ObservableProperty]
    private bool isShowJson = true;


    partial void OnSelectedShowEntityChanged(ShowEntity? oldValue, ShowEntity newValue)
    {
        if (newValue == null) return;
        if ((oldValue != null && !oldValue.IsOnlyRead) || (oldValue == null && cache == null))
        {
            cache = new ShowEntity("缓存", this.contentWidth, this.contentHeight);
        }

        this.SizeIsEnabled = !newValue.IsOnlyRead;
        if (!newValue.IsOnlyRead)
        {
            this.ContentWidth = cache.Width;
            this.ContentHeight = cache.Height;
            return;
        }

        this.ContentWidth = newValue.Width;
        this.ContentHeight = newValue.Height;
    }

    partial void OnIsShowChanged(bool value)
    {
        if (!value)
        {
            var d = this.EditerTextWidth;
            this.hideEditerWidth = d.Value;

            this.EditerTextWidth = new GridLength(0, GridUnitType.Pixel);
        }
        else
        {
            this.EditerTextWidth = new GridLength(this.hideEditerWidth, GridUnitType.Pixel);
        }
    }

    partial void OnIsShowJsonChanged(bool value)
    {
        if (!value)
        {
            var d = this.JsonTextWidth;
            this.hideJsonWidth = d.Value;

            this.JsonTextWidth = new GridLength(0, GridUnitType.Pixel);
        }
        else
        {
            this.JsonTextWidth = new GridLength(this.hideJsonWidth, GridUnitType.Pixel);
        }
    }
    public void EditTextChanged(string value)
    {
        _ = this.ReaderXaml();
    }

    public void JsonTextChanged(string value)
    {
        var db = System.Text.Json.JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(value);
        dataContext = db;
        if (this.DesignContent != null) this.DesignContent.DataContext = this.dataContext;
    }

    private async Task Watcher()
    {

        this.watcher = new FileSystemWatcher();
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Path = System.IO.Path.GetDirectoryName(this.entity.Path);
        watcher.Filter = System.IO.Path.GetFileName(this.entity.Path);
        watcher.EnableRaisingEvents = true;
        watcher.Changed += Watcher_Changed;
    }



    private async void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        await this.ReadFileAsync();

        await this.ReaderXaml();
    }

    private async Task ReadFileAsync()
    {
        var path = this.entity.Path;

        FileInfo fileInfo = new FileInfo(path);
        if (lastWriteTimeTicks >= fileInfo.LastWriteTime.Ticks) return;

        lastWriteTimeTicks = fileInfo.LastWriteTime.Ticks;

        try
        {
            var value = File.ReadAllText(path);
            this.EditText = value;
        }
        catch (IOException ioex)
        {
            return;
        }

    }

    public async Task ReaderXaml()
    {
        var text = this.EditText;

        FrameworkElement? ui = null;

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                var content = await this.dispatcher.ExecuteAsync<object>((ct) => ValueTask.FromResult(Microsoft.UI.Xaml.Markup.XamlReader.Load(text)));
                if (content is FrameworkElement u) ui = u;
            }
            catch (Exception ex)
            {
                var content = await this.dispatcher.ExecuteAsync<FrameworkElement>((ct) => ValueTask.FromResult<FrameworkElement>(new TextBlock()
                {
                    Text = ex.Message,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }));

                ui = content;
            }
        }


        this.dispatcher.TryEnqueue(() =>
        {
            if (ui != null)
                ui.DataContext = this.dataContext;
            this.DesignContent = ui;
        });
    }


}
