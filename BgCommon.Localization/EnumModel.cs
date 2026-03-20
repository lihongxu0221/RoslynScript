namespace BgCommon.Localization;

/// <summary>
/// 枚举实体类
/// </summary>
public partial class EnumModel : ObservableObject
{
    [ObservableProperty]
    private Enum? value = null;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string langKey = string.Empty;

    [ObservableProperty]
    private string display = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isEnable = true;

    [ObservableProperty]
    private bool isSelected = false;
}