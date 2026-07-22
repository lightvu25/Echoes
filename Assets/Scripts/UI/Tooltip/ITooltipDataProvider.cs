public interface ITooltipDataProvider
{
    /// <summary>
    /// Returns the data required to populate the tooltip.
    /// If null is returned, the tooltip will not be shown.
    /// </summary>
    ItemBaseData GetTooltipData();
}
