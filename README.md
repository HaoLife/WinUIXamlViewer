winui3 xaml 预览器

可在 desktop(skia) 模式下加载外部dll 扩展自己的组件（或其他包的组件）

(winui3 模式下，XamlReader 的加载采用 GetReferencedAssemblies() 运行程序的依赖程序来识别xaml，无法动态扩展，插件模式在该模式下不能使用)

可直接选择xaml 文件监听文件变更来实时预览xaml的效果

可置顶窗口，在开发时候编辑xaml 在置顶的窗口上查看效果


问题：wasm 模式下打开文件和加载程插件模式不能使用（后续看看能否解决该问题）
