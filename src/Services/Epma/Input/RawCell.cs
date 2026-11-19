namespace GeoChemistryNexus.Services.Epma.Input
{
    /// <summary>
    /// 工作表及包内示例的原始单元格值。
    /// </summary>
    public abstract record RawCell
    {
        private RawCell()
        {
        }

        /// <summary>空单元格。</summary>
        public sealed record Empty : RawCell;

        /// <summary>原样文本（不修剪、不转换）。</summary>
        public sealed record Text(string Value) : RawCell;

        /// <summary>工作表或 JSON 示例中已类型化的数值。</summary>
        public sealed record NativeNumber(double Value) : RawCell;
    }

}
