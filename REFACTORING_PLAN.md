# 修改大纲：彻底前后端分离

## 一、设计原则

1. **单一依赖方向**：Views → ViewModels → Core 接口 → Core 实现。后端（Core）**零 WPF、零 CommunityToolkit 引用**，可独立编译、可单元测试。
2. **两条铁律**：
   - WPF 类型（`Window`、`Application`、`MessageBox`、`Dispatcher`、`ObservableCollection`）只允许出现在 UI 项目；
   - 所有 `App.*` 静态引用（`App.LogService`、`App.ProcessManager`、`App.ServiceProvider`）全部替换为 DI 注入。
3. **定义边界**：后端 = 领域模型 + 服务（MeCab、HTTP、文件、进程）；前端 = Views + ViewModels + 呈现辅助类 + 消息。

## 二、目标工程结构

```
FuriganaGlossing.sln
├── FuriganaGlossing.Core/          ← 后端（net8.0 class library）
│   ├── Models/
│   │   ├── AppConfig.cs  FuriganaToken.cs  OcrResult.cs (+Rect)
│   │   └── LogEntry.cs                       ← 新增
│   └── Services/
│       ├── ILogService.cs / LogService.cs
│       ├── IConfigService / ConfigService.cs
│       ├── IFuriganaService / FuriganaService.cs
│       ├── IOcrService / OcrService.cs
│       ├── ITranslationService / TranslationService.cs
│       └── IProcessManagerService / ProcessManagerService.cs
└── FuriganaGlossing/               ← 前端（net8.0-windows WPF 可执行）
    ├── App.xaml.cs                 （组合根，DI 注册）
    ├── Messages/
    ├── ViewModels/
    │   ├── MainViewModel.cs  ConfigViewModel.cs
    │   └── LogPresenter.cs                     ← 新增（日志→集合桥）
    └── Views/
```

依赖：`FuriganaGlossing` → `FuriganaGlossing.Core`。Core 只引用 `MeCab.DotNet`、`Microsoft.Extensions.Http`、`Microsoft.Extensions.DependencyInjection` 抽象，**不引用 WPF**。

## 三、具体改动

### 后端 Core

1. **`LogService` 去 WPF 化**（当前 `Services/LogService.cs` L39、L52 用了 `Application.Current.Dispatcher.Invoke`，L26 暴露 `ReadOnlyObservableCollection<string>`）：
   - 移除 WPF 类型；`Log()` 改为内部 lock + `ConcurrentQueue`，线程安全。
   - 新增事件 `event EventHandler<LogEntry>? EntryAdded`；`Logs` 集合改为只读的 `IReadOnlyCollection`（Core 侧不持有 UI 绑定集合）。
   - 新增 `Models/LogEntry.cs`：`Time`、`Level`、`Message`。
2. **`ProcessManagerService` 去 UI**（L72 `MessageBox.Show`）：
   - 异常不再弹框，改为 `ILogService` 记录后抛出 `ProcessStartException`（或返回 bool）；UI 侧决定如何提示。
   - 删除 L18-L25 注释掉的 P/Invoke（AttachConsole 等已不用）。
3. **三个服务统一注入日志**：`FuriganaService`（L37/41/51/55/64/100）、`OcrService`（L36/70/74/85）、`TranslationService`（L36/67/72）的 `App.LogService.Log` 全部改为构造函数注入 `ILogService` 并 `Log(...)`；同时删掉 `FuriganaService` 头部无意义的 static using（`System.Net.Mime.MediaTypeNames`、`JavaScript.JSType`）。
4. `ConfigService` 基本不动（已干净），仅确认不引用任何 WPF。

### 前端 UI

5. **新增 `LogPresenter`**（ViewModels 层）：订阅 Core 的 `ILogService.EntryAdded`，在 UI 线程把 `LogEntry` 格式化为字符串加入 `ObservableCollection<string>`（限 1000 条）。MainWindow 的 `LogListBox` 绑定它而不是 VM。
6. **`MainViewModel` 瘦身**（当前 L44、L74-77、L82）：
   - L44 `Logs => App.LogService.Logs` 删除，交给 LogPresenter。
   - L74-77 `new ConfigViewModel(...)` / `new ConfigWindow(...)` / `Application.Current.MainWindow` 全部删除。改为：`OpenSettings` 命令只发一条 `OpenSettingsRequested` 消息；**窗口实例化回到 View 侧**（MainWindow 注册消息处理器，从 DI 取 `ConfigViewModel`，`new ConfigWindow`、设 Owner、ShowDialog）。
   - L82 `ProcessImageAsync` 增加 try/catch，失败时写 `ILogService`；可选加 `IsProcessing` 状态属性给 UI 显示。
7. **`ConfigViewModel` 纳入 DI**：在 `App.xaml.cs` 注册 `AddTransient<ConfigViewModel>()`（依赖 `IConfigService`、`IProcessManagerService`，均已存在），删除 `App.ProcessManager` 静态。
8. **`MainWindow.xaml.cs`**：L85 Ctrl+V 的 `_viewModel.ProcessImageAsync(...)` 加 `async/await + try/catch`；WebView2 错误处理（L63）可保留（这是 View 层，弹框合理）。
9. **`App.xaml.cs`**：`ProcessManager`、`LogService` 静态属性删除，只保留 `ServiceProvider`。新增 `ConfigViewModel`、`MainViewModel` 注册（`MainViewModel` 保持现有）。

### 工程结构

10. 解算拆两项目：`FuriganaGlossing.Core`（net8.0）+ `FuriganaGlossing`（WPF）；按上面的目录把 Models/Services 整体移动到 Core；`Messages` 留在 UI 项目（消息是 UI 通信契约）。
11. Core 的 csproj：`net8.0`，包引用 `MeCab.DotNet`、`Microsoft.Extensions.Http`、`Microsoft.Extensions.DependencyInjection`（仅接口层需要则用 `Microsoft.Extensions.DependencyInjection.Abstractions` + `Logging.Abstractions`）。`dic` 文件夹仍作为 UI 项目 Content 复制到输出目录（MeCab 初始化路径用 `AppDomain.CurrentDomain.BaseDirectory` 即可）。

### 测试（验证分离有效）

12. 新增 `FuriganaGlossing.Core.Tests`：
    - `FuriganaService`：固定 MeCab 节点序列的解析 + 平假名转换；
    - `OcrService`/`TranslationService`：`IHttpClientFactory` 用测试 HttpHandler 桩，验证请求体/响应解析；
    - `ConfigService`：临时目录 JSON 序列化/反序列化/损坏配置兜底；
    - `LogService`：跨线程事件投递 + Clear。

    核心验证点：**这些测试工程不引用任何 WPF 包**，能编译即证明后端干净。

## 四、分阶段实施

1. **Phase 1（Core 去耦合）**：改 LogService / ProcessManagerService / 三服务的日志来源 + LogEntry。风险最低。
2. **Phase 2（前端桥接）**：LogPresenter、MainViewModel 瘦身、OpenSettings 消息化、ConfigViewModel 进 DI、MainWindow 侧建配置窗。
3. **Phase 3（物理拆项目）**：Core/UI 两项目拆分、csproj 调整、组合根迁移。
4. **Phase 4（测试+收尾）**：Core 单测、清理未用 using、`ProcessImageAsync` 异常处理、`dic` 输出路径验证。

## 五、决策点

1. **HTML 渲染放哪**：`GenerateResultHtml()` 产出的只是字符串（非 WPF 类型），留在 `MainViewModel` 可接受；若要更彻底，抽成 UI 项目里独立 `FuriganaHtmlRenderer` 类注入 VM。建议：先留在 VM。
2. **连接状态轮询**（MainViewModel 每 4s 查一次）属于前端呈现逻辑，保留不动。
3. **`OcrService` 里的 `"image_url = [image omitted]{base64Image}"`** 是疑似占位符遗留（与分离无关），可在 Phase 1 顺手确认是否要清理。
4. **是否直接动手**：大纲按 4 个 Phase 设计，建议一次一个 Phase + 构建验证，不一次性改完。

## 六、进度记录

> 说明："三、具体改动"中的行号是重构前代码的参考位置，重构后已失效。

### 2026-10-10 · Phase 1（Core 去耦合）完成 + 警告清零

**Phase 1 四项（三节 1-4）已完成**
- LogService 去 WPF：Log() 改为内部 lock + ConcurrentQueue，线程安全；新增 vent EventHandler<LogEntry>? EntryAdded；Logs 改为只读 IReadOnlyCollection<LogEntry>（1000 条上限）；新增 Models/LogEntry.cs（Time/Level/Message）。
- ProcessManagerService 去 UI：MessageBox.Show 改为 ILogService 记录 + 抛 ProcessStartException（新增类）；删除注释掉的 P/Invoke（AttachConsole/FreeConsole/GenerateConsoleCtrlEvent）。
- FuriganaService/OcrService/TranslationService：所有 App.LogService.Log 改为构造函数注入 ILogService 后 Log(...)；删除 FuriganaService 头部两个无意义 static using。
- ConfigService：确认干净（零 WPF、零 App.*），未动。
- 交叉验证：Services/ 下无任何 System.Windows / Microsoft.Web / App. 引用。

**Phase 2 项先行完成（必需：Phase 1 的 Logs 契约变更会直接打破 UI 旧绑定，不补齐这些项目无法编译）**
- 新增 ViewModels/LogPresenter（三节 5）：订阅 EntryAdded，UI 线程格式化，ObservableCollection<string>（1000 条）；MainWindow 的 LogListBox 绑定它而非 VM。
- MainViewModel：删除 Logs => App.LogService.Logs（三节 6 第 1 子项）。
- ConfigViewModel.StartServers：捕获 ProcessStartException 并弹框（保持原有错误提示体验）。
- Ctrl+V 路径（三节 8）：OnKeyDown → sync void，新增 CaptureClipboardImageAsync（wait _viewModel.ProcessImageAsync(...) + try/catch，失败弹框）。

**全部 34 条构建警告修复完毕（17 类，temp 项目重复报告各计一次）；结果：0 错误 / 34 警告 → 0 错误 / 0 警告**
- Models/OcrResult.cs：OcrTextLine.Text → string?（CS8618）。
- FuriganaService：字段 → MeCabTagger? _tagger；InitializeTaggerAsync() 返回 MeCabTagger?；GetFuriganaAsync 增加判空，失败时记录日志并抛 InvalidOperationException（原为潜在 NRE）（CS8618/CS8625）。
- OcrService/TranslationService：LlmResponse/LlmChoice/LlmMessage 的 Choices/Message/Content 改可空（CS8618 × 6）。
- MainViewModel：5 个 [ObservableProperty] 字段初始化（_ocrText/_translationText/_resultHtml → string.Empty，_ocrResults/_furiganaTokens → 
ew()），删除 FuriganaTokens != null && 多余判空（CS8618 × 5）。
- MainWindow.xaml.cs：两个事件处理函数 sender 改 object?（CS8622 × 2）；Ctrl+V 路径消除未 await 警告（CS4014）。

**验证**：dotnet build FuriganaGlossing.csproj --no-incremental → 0 个警告 0 个错误。

**Phase 2（前端桥接）已完成（三节 5-9 全部落实）**
- LogPresenter（三节 5）：LogListBox 绑定 LogPresenter 而非 VM（已先行完成）。
- MainViewModel 瘦身（三节 6）：OpenSettings 命令只发 OpenSettingsRequestedMessage；ProcessImageAsync 加 try/catch（失败写 ILogService 后重抛，便于 UI 弹框）并新增 IsProcessing 状态属性。
- ConfigViewModel 纳入 DI（三节 7/9）：App.xaml.cs 新增 AddTransient<ConfigViewModel>()；删除 App.ProcessManager / App.LogService 静态属性，仅保留 ServiceProvider；OnExit 改用 DI 取得 ProcessManagerService。
- 窗口实例化回 View 侧（三节 6/8）：MainWindow 注册 OpenSettingsRequestedMessage 处理器，从 DI 取 ConfigViewModel，new ConfigWindow + Owner + ShowDialog；ILogService 亦改 DI 取得。
- 顺带修复：MainWindow.xaml 第 68 行 IsProcessing 的 DataTrigger 缺失 </DataTrigger> 闭合标签（此前导致构建失败）。
- 验证：dotnet build FuriganaGlossing.csproj --no-incremental → 0 个警告 0 个错误。

**Phase 3（物理拆分项目）完成（三节 10-11 + 组合根迁移）**
- 物理拆为两项目：`FuriganaGlossing.Core/`（net8.0 类库）+ `FuriganaGlossing/`（net8.0-windows WPF）。`Models/`、`Services/` 整目录移入 Core；`Messages/` 留在 UI 项目（消息是 UI 通信契约）。
- 新建 `FuriganaGlossing.Core/FuriganaGlossing.Core.csproj`：`net8.0`，包引用 MeCab.DotNet 1.2.0、Microsoft.Extensions.DependencyInjection 8.0.0、Microsoft.Extensions.Http 8.0.0；Core 零 WPF、零 CommunityToolkit 引用。
- WPF 项目 `FuriganaGlossing/FuriganaGlossing.csproj` 新增对 Core 的 ProjectReference；移除 MeCab.DotNet、Microsoft.Extensions.Http（已随 Core 引入），保留 CommunityToolkit.Mvvm、DI、Hosting、WebView2；`Microsoft.Extensions.Http` 因 `App.xaml.cs` 调用 `AddHttpClient()` 而在 WPF 侧显式保留。
- `dic` 词典仍作为 WPF 项目 Content 复制到输出目录（MeCab 初始化路径用 `AppDomain.CurrentDomain.BaseDirectory`）。
- 组合根迁移：`App.xaml.cs` 留在 WPF 项目，注册 Core 各服务（ILogService/IFuriganaService/IOcrService/ITranslationService/IProcessManagerService/IConfigService）及 MainViewModel/ConfigViewModel；`App.ServiceProvider` 保留（ProcessManager/LogService 静态已在 Phase 2 删除）。
- 解决方案 `FuriganaGlossing.slnx` 更新为引用两个项目。

**验证**
- `dotnet build FuriganaGlossing.slnx --no-incremental` → 0 个错误 0 个警告。
- Core 依赖图（`FuriganaGlossing.Core.deps.json`）无任何 WPF 相关包（无 Windows/Wpf/PresentationCore/WindowsBase/Microsoft.Web）。
- Core 源码无 `System.Windows`、`Dispatcher`、`MessageBox`、`ObservableCollection`、`Application.Current` 等 WPF 类型引用。
- `dic` 文件夹正确复制到 WPF 输出目录（`FuriganaGlossing/bin/Debug/net8.0-windows/dic`），含 char.bin、dicrc、matrix.bin、sys.dic、unk.dic、README.txt。

**剩余事项**
- Phase 4：新增 FuriganaGlossing.Core.Tests（MeCab 解析、HTTP 桩、配置 JSON、跨线程日志四类测试）；清理未用 using；dic 输出路径验证（Phase 3 已验证复制行为）。
- 决策点 3 待决：OcrService 请求体中 [image omitted]{base64Image} 占位符尚未清理（涉及网络请求行为，待确认正确写法）。
