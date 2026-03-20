using BgCommon.Script.Runtime.Configuration;

namespace BgCommon.Script.Runtime.Services;

/// <summary>
/// 遥测提供程序的具体实现类.
/// </summary>
internal class TelemetryProvider : TelemetryProviderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryProvider"/> class.
    /// </summary>
    public TelemetryProvider()
    {
    }

    /// <summary>
    /// 初始化遥测提供程序并订阅分发器异常.
    /// </summary>
    /// <param name="version">应用程序版本.</param>
    /// <param name="settings">应用程序设置.</param>
    public override void Initialize(string version, IApplicationSettings settings)
    {
        // 调用基类初始化逻辑以订阅 AppDomain 异常
        base.Initialize(version, settings);

        // 订阅 WPF 界面线程的未处理异常
        Application.Current.DispatcherUnhandledException += this.OnUnhandledDispatcherException;
    }

    /// <summary>
    /// 处理 UI 线程分发器捕获的未处理异常.
    /// </summary>
    /// <param name="sender">事件发送者.</param>
    /// <param name="eventArgs">异常事件参数.</param>
    private void OnUnhandledDispatcherException(object? sender, DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        // 将异常交给基类处理逻辑
        this.HandleException(eventArgs.Exception);

        // 标记异常已处理，防止应用程序直接崩溃退出.
        eventArgs.Handled = true;
    }
}