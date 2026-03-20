using System.Windows.Input;
using System.Windows.Threading;

namespace BgCommon;

/// <summary>
/// 空闲监控器.
/// </summary>
public class IdleMonitor
{
    /// <summary>
    /// 是否为调试模式.
    /// </summary>
    private readonly bool isDebugEnabled;

    /// <summary>
    /// 用于监控用户空闲状态的计时器.
    /// </summary>
    private readonly DispatcherTimer idleTimer;

    /// <summary>
    /// 当用户在指定的超时时间内没有任何键盘或鼠标输入时触发的事件.
    /// </summary>
    public event EventHandler? IdleTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdleMonitor"/> class.
    /// </summary>
    /// <param name="timeout">超时时间.</param>
    public IdleMonitor(TimeSpan timeout)
    {
        isDebugEnabled = System.IO.File.Exists("debugtest.txt");

        idleTimer = new DispatcherTimer
        {
            Interval = timeout,
        };
        idleTimer.Tick += OnIdleTimeout;

        // 监听全局的输入事件来重置计时器
        InputManager.Current.PreProcessInput += OnUserInput;
    }

    /// <summary>
    /// 启动空闲监控计时器.
    /// </summary>
    public void Start()
    {
        idleTimer.Start();
    }

    /// <summary>
    /// 停止空闲监控计时器.
    /// </summary>
    public void Stop()
    {
        idleTimer.Stop();
    }

    private void OnUserInput(object sender, PreProcessInputEventArgs e)
    {
        // 检查是否为键盘事件
        if (e.StagingItem.Input is KeyEventArgs _)
        {
            idleTimer.Stop();
            idleTimer.Start();
        }
        else if (e.StagingItem.Input is MouseEventArgs _)
        {
            // 检查是否为鼠标事件
            idleTimer.Stop();
            idleTimer.Start();
        }
    }

    private void OnIdleTimeout(object? sender, EventArgs e)
    {
        idleTimer.Stop();
        if (!isDebugEnabled)
        {
            IdleTimeout?.Invoke(this, EventArgs.Empty);
        }
    }
}