using Dalamud.IoC;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace QuestShare.Common
{
    internal static class SheetManager
    {
        // All ExcelSheet<T> instances should be stored here
        [PluginService] public static ExcelSheet<Quest> QuestSheet { get; } = DataManager.GetExcelSheet<Quest>();
        [PluginService] public static ExcelSheet<Map> MapSheet { get; } = DataManager.GetExcelSheet<Map>();
        [PluginService] public static ExcelSheet<PlaceName> PlaceNameSheet { get; } = DataManager.GetExcelSheet<PlaceName>();
    }
}
