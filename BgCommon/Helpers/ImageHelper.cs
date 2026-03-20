using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BgCommon;

/// <summary>
/// 图片处理辅助类，提供异步加载与跨线程安全处理图片的功能.
/// </summary>
public class ImageHelper
{
    /// <summary>
    /// 异步加载本地图片并确保其跨线程安全.
    /// </summary>
    /// <param name="filePath">图片文件的完整路径.</param>
    /// <returns>返回异步加载后的 BitmapImage 实例，若失败则返回 null.</returns>
    public static async Task<BitmapImage?> GetImageAsync(string filePath)
    {
        // 验证参数是否为空.
        ArgumentNullException.ThrowIfNull(filePath, nameof(filePath));

        BitmapImage? bitmapImageResult = null;
        try
        {
            // 在后台线程执行文件读取和图像初始化.
            bitmapImageResult = await Task.Run(() =>
            {
                // 读取图片的原始字节数据.
                byte[] imageRawData = File.ReadAllBytes(filePath);

                using (var memoryStream = new MemoryStream(imageRawData))
                {
                    var bitmapImage = new BitmapImage();

                    // 开始图像初始化过程.
                    bitmapImage.BeginInit();

                    // 设置立即缓存，确保流关闭后图像依然可用.
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;

                    // 结束图像初始化.
                    bitmapImage.EndInit();

                    // 冻结对象，使其可以在非 UI 线程之间安全传递.
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            });
        }
        catch (Exception)
        {
            // 重新抛出捕获的异常以供上层处理.
            throw;
        }

        return bitmapImageResult;
    }

    /// <summary>
    /// 异步加载本地图片并支持取消操作与进度汇报.
    /// </summary>
    /// <param name="filePath">图片文件的完整路径.</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌.</param>
    /// <param name="progress">用于报告加载进度的接口.</param>
    /// <returns>返回异步加载后的 BitmapImage 实例.</returns>
    public static async Task<BitmapImage?> LoadImageAsync(string filePath, CancellationToken cancellationToken, IProgress<int>? progress = null)
    {
        // 验证路径参数.
        ArgumentNullException.ThrowIfNull(filePath, nameof(filePath));

        BitmapImage? bitmapImageResult = null;
        try
        {
            // 在后台线程异步读取并构建图像对象.
            bitmapImageResult = await Task.Run(
                async () =>
                {
                    // 使用异步方式读取文件的所有字节.
                    byte[] imageRawBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

                    using (var memoryStream = new MemoryStream(imageRawBytes))
                    {
                        memoryStream.Position = 0;
                        var bitmapImage = new BitmapImage();

                        bitmapImage.BeginInit();

                        // 确保图像在加载时完成解码并缓存.
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = memoryStream;

                        bitmapImage.EndInit();

                        // 冻结图像使其跨线程可用.
                        bitmapImage.Freeze();

                        return bitmapImage;
                    }
                }, cancellationToken);
        }
        catch (Exception)
        {
            // 维持原始逻辑，继续向上抛出异常.
            throw;
        }

        return bitmapImageResult;
    }
}