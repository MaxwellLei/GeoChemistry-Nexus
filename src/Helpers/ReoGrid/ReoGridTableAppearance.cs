using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using unvell.ReoGrid;
using unvell.ReoGrid.CellTypes;
using unvell.ReoGrid.Rendering;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>表格列的语义类型，决定表头与数据区的配色（颜色全部来自当前主题）。</summary>
    public enum ReoGridColumnKind
    {
        /// <summary>用户输入列（白色表头，白色数据区）。</summary>
        Input,
        /// <summary>计算结果列（主题浅色表头，主题浅色数据区）。</summary>
        Output,
        /// <summary>输入区与结果区之间的分隔列。</summary>
        Separator
    }

    /// <summary>
    /// ReoGrid 表格统一外观。
    /// 所有基于 ReoGrid 的表格（EPMA、CIPW、Niggli、地质温压计、图解数据表）都通过此类取得一致的配色、字体、行高与表头。
    /// 表头、行头、选区、输入 / 结果区底色全部取自 <see cref="AppThemeHelper.CurrentSheetColors"/>，随主题色变化；
    /// 配合 <see cref="GeoChemistryNexus.Controls.ReoGridTableHost"/> 可同时获得细滚动条，并在切换主题时自动刷新。
    /// </summary>
    public static class ReoGridTableAppearance
    {
        public const string CellFontName = "Segoe UI";
        // WPF uses DIP; ReoGrid cell styles use points (13 DIP = 9.75 pt).
        public const double HeaderFontSize = 13;
        public const float CellFontSize = 9.75f;
        public const ushort BodyRowHeight = 26;
        public const ushort HeaderRowHeight = 36;
        /// <summary>列头带“单位”第二行时的主区域高度。</summary>
        public const ushort HeaderRowHeightWithUnit = 46;
        /// <summary>列头上方“分组行”的高度（仅在模块提供分组信息时出现）。</summary>
        public const ushort GroupBandHeight = 24;
        /// <summary>注册了行状态点后，行头需要比默认更宽，给状态点留出位置。</summary>
        public const ushort RowHeaderWidthWithStatus = 54;
        /// <summary>按文本宽度设置列宽时附加的左右留白（像素）。</summary>
        public const double ColumnHorizontalPadding = 20;
        /// <summary>行数超过此值的超大表格不自绘行头，避免逐行创建对象。</summary>
        private const int MaxStyledRowHeaders = 20000;

        private static readonly Color BodyText = Color.FromRgb(51, 65, 85);

        private static readonly Typeface RegularTypeface = new(CellFontName);
        private static readonly Typeface BoldTypeface = new(new FontFamily(CellFontName), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly FontFamily NumberFontFamily = new("Cascadia Mono, Consolas");
        private static readonly Typeface NumberTypeface = new(NumberFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private static readonly Typeface NumberBoldTypeface = new(NumberFontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private static readonly FieldInfo? HeaderHeightField = typeof(Worksheet).GetField("colHeaderHeight", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? DefaultRowHeightField = typeof(Worksheet).GetField("defaultRowHeight", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo? UpdateViewportBounds = typeof(Worksheet).GetMethod("UpdateViewportControllBounds", BindingFlags.Instance | BindingFlags.NonPublic);

        #region 主题画刷

        /// <summary>当前主题对应的一组冻结画刷，主题切换后（<see cref="SheetColors"/> 换了新实例）自动重建。</summary>
        private sealed class SheetBrushes
        {
            public readonly SheetColors Source;
            public readonly Brush Accent, Highlight, OutputBack, OutputBand, InputBand, Separator;
            public readonly Brush HeaderText, RowHeaderBack, Mute, Sub, White;
            public readonly Brush AccentSoft, WhiteSoft;
            public readonly Pen GridPen, HeaderLinePen;

            public SheetBrushes(SheetColors c)
            {
                Source = c;
                Accent = FrozenBrush(c.Accent);
                Highlight = FrozenBrush(c.Highlight);
                OutputBack = FrozenBrush(c.OutputBack);
                OutputBand = FrozenBrush(c.OutputBand);
                InputBand = FrozenBrush(c.InputBand);
                Separator = FrozenBrush(c.Separator);
                HeaderText = FrozenBrush(c.HeaderText);
                RowHeaderBack = FrozenBrush(c.RowHeaderBack);
                Mute = FrozenBrush(c.Mute);
                Sub = FrozenBrush(c.Sub);
                White = FrozenBrush(System.Windows.Media.Colors.White);
                AccentSoft = FrozenBrush(Color.FromArgb(0xB0, c.Accent.R, c.Accent.G, c.Accent.B));
                WhiteSoft = FrozenBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF));
                GridPen = FrozenPen(c.GridLine);
                HeaderLinePen = FrozenPen(c.HeaderLine);
            }
        }

        private static SheetBrushes? _theme;

        private static SheetBrushes Theme
        {
            get
            {
                var colors = AppThemeHelper.CurrentSheetColors;
                if (_theme is null || !ReferenceEquals(_theme.Source, colors)) _theme = new SheetBrushes(colors);
                return _theme;
            }
        }

        #endregion

        #region 控件级外观

        /// <summary>设置控件级配色：表头、网格线、选区（均取自当前主题）。</summary>
        public static void ApplyControlStyle(ReoGridControl grid)
        {
            var c = AppThemeHelper.CurrentSheetColors;
            var style = ControlAppearanceStyle.CreateDefaultControlStyle();
            style[ControlAppearanceColors.ColHeadNormalStart] = Colors.White;
            style[ControlAppearanceColors.ColHeadNormalEnd] = Colors.White;
            foreach (var key in new[] { ControlAppearanceColors.RowHeadNormal, ControlAppearanceColors.LeadHeadNormal })
                style[key] = c.RowHeaderBack;
            foreach (var key in new[] { ControlAppearanceColors.ColHeadHoverStart, ControlAppearanceColors.ColHeadHoverEnd,
                ControlAppearanceColors.RowHeadHover, ControlAppearanceColors.LeadHeadHover })
                style[key] = c.OutputBack;
            foreach (var key in new[] { ControlAppearanceColors.ColHeadSelectedStart, ControlAppearanceColors.ColHeadSelectedEnd,
                ControlAppearanceColors.ColHeadFullSelectedStart, ControlAppearanceColors.ColHeadFullSelectedEnd,
                ControlAppearanceColors.RowHeadSelected, ControlAppearanceColors.RowHeadFullSelected, ControlAppearanceColors.LeadHeadSelected })
                style[key] = c.Highlight;
            style[ControlAppearanceColors.ColHeadText] = c.HeaderText;
            style[ControlAppearanceColors.RowHeadText] = c.Mute;
            style[ControlAppearanceColors.GridLine] = c.GridLine;
            // 表头 / 行头由 Body 自己画边线；ReoGrid 在 Body 之后还会补画一条分隔线，会把分组带切成一格一格，所以让它几乎透明。
            // 注意不能用 alpha = 0：ReoGrid 对完全透明的颜色取不到画笔（返回 null），绘制时会直接抛 NullReferenceException。
            var invisible = new unvell.ReoGrid.Graphics.SolidColor((byte)1, c.GridLine.R, c.GridLine.G, c.GridLine.B);
            style[ControlAppearanceColors.ColHeadSplitter] = invisible;
            style[ControlAppearanceColors.RowHeadSplitter] = invisible;
            style[ControlAppearanceColors.SelectionBorder] = c.Accent;
            style[ControlAppearanceColors.SelectionFill] = new unvell.ReoGrid.Graphics.SolidColor((byte)26, c.Accent.R, c.Accent.G, c.Accent.B);
            style.SelectionBorderWidth = 2;
            grid.ControlStyle = style;
        }

        /// <summary>
        /// 主题切换后重新套用所有依赖主题色的部分：控件级配色与已登记的结果区 / 分隔列底色。
        /// <see cref="GeoChemistryNexus.Controls.ReoGridTableHost"/> 会在收到主题变化消息时自动调用。
        /// </summary>
        public static void ReapplyTheme(ReoGridControl grid)
        {
            ApplyControlStyle(grid);
            var sheet = grid.CurrentWorksheet;
            if (sheet is null) return;
            if (GetDecorations(sheet) is { } decorations)
            {
                foreach (var (range, kind) in decorations.BodyRanges) ApplyBodyRange(sheet, range, kind);
            }
            sheet.RequestInvalidate();
        }

        #endregion

        #region 数据区外观

        /// <summary>
        /// 设置数据区默认字体、文字颜色、垂直居中与行高。
        /// 注意：会覆盖整个工作表的字体与文字颜色，应在写入表头样式之前调用。
        /// </summary>
        public static void ApplyCellDefaults(Worksheet sheet)
        {
            sheet.SetRangeStyles(RangePosition.EntireRange, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontName | PlainStyleFlag.FontSize | PlainStyleFlag.TextColor | PlainStyleFlag.VerticalAlign,
                FontName = CellFontName,
                FontSize = CellFontSize,
                TextColor = BodyText,
                VAlign = ReoGridVerAlign.Middle,
            });
            // 之后新增的行（粘贴扩行、末行自动增行）沿用同一行高。
            DefaultRowHeightField?.SetValue(sheet, BodyRowHeight);
            if (sheet.RowCount > 0) sheet.SetRowsHeight(0, sheet.RowCount, BodyRowHeight);
        }

        /// <summary>为新增的行设置标准行高（行数增加后调用）。</summary>
        public static void ApplyBodyRowHeight(Worksheet sheet, int firstRow, int rowCount)
        {
            if (rowCount > 0 && firstRow >= 0 && firstRow + rowCount <= sheet.RowCount)
                sheet.SetRowsHeight(firstRow, rowCount, BodyRowHeight);
        }

        /// <summary>
        /// 给一块区域套用结果列/分隔列底色与细边框；输入列保持白底，不做处理。
        /// 区域会被登记下来，主题切换时由 <see cref="ReapplyTheme"/> 重新着色。
        /// </summary>
        public static void StyleBodyRange(Worksheet sheet, RangePosition range, ReoGridColumnKind kind)
        {
            if (kind == ReoGridColumnKind.Input) return;
            var decorations = DecorationTable.GetOrCreateValue(sheet);
            decorations.BodyRanges.RemoveAll(x => x.Range.Row == range.Row && x.Range.Col == range.Col
                && x.Range.Rows == range.Rows && x.Range.Cols == range.Cols);
            decorations.BodyRanges.Add((range, kind));
            ApplyBodyRange(sheet, range, kind);
        }

        private static void ApplyBodyRange(Worksheet sheet, RangePosition range, ReoGridColumnKind kind)
        {
            var c = AppThemeHelper.CurrentSheetColors;
            bool output = kind == ReoGridColumnKind.Output;
            sheet.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.BackColor,
                BackColor = output ? c.OutputBack : c.Separator,
            });
            // ReoGrid 对有底色的单元格不绘制默认网格线，这里补回细边框保持表格结构。
            sheet.SetRangeBorders(range, BorderPositions.All, new RangeBorderStyle
            {
                Color = c.GridLine,
                Style = BorderLineStyle.Solid,
            });
        }

        #endregion

        #region 原生列头（使用 ReoGrid 自带列头的表格）

        /// <summary>读取当前列头高度；列头隐藏时返回 0。</summary>
        public static double GetColumnHeaderHeight(Worksheet sheet)
        {
            if (!sheet.HasSettings(WorksheetSettings.View_ShowColumnHeader)) return 0;
            return HeaderHeightField?.GetValue(sheet) is ushort height ? height : 18;
        }

        /// <summary>把原生列头加高到与 EPMA 表格一致。</summary>
        public static void ConfigureColumnHeaderHeight(Worksheet sheet)
        {
            // ReoGrid WPF 3.3.0 没有公开的列头高度设置接口，版本相关的反射访问统一收口在这里。
            // 若后续版本改了成员，则保留其原生高度，仅以紧凑文字渲染。
            if (HeaderHeightField?.FieldType != typeof(ushort) || UpdateViewportBounds is null)
            {
                Trace.TraceWarning("ReoGrid: this version does not expose the expected column-header height members.");
                return;
            }
            // 列头总高 = 主区域（有单位行时更高）+ 分组行（仅模块提供分组时）。
            var decorations = GetDecorations(sheet);
            ushort target = (ushort)((decorations?.HasUnit == true ? HeaderRowHeightWithUnit : HeaderRowHeight)
                + (decorations?.HasGroup == true ? GroupBandHeight : 0));
            if (HeaderHeightField.GetValue(sheet) is ushort current && current == target) return;
            HeaderHeightField.SetValue(sheet, target);
            UpdateViewportBounds.Invoke(sheet, null);
        }

        /// <summary>
        /// 设置一个原生列头；<paramref name="text"/> 为 null 时保留原有文字。
        /// 列头文字在绘制时实时读取，之后修改 <c>ColumnHeaders[i].Text</c> 无需再次调用。
        /// </summary>
        public static void SetColumnHeader(ReoGridControl grid, int column, string? text = null,
            ReoGridColumnKind kind = ReoGridColumnKind.Input, double? fontSize = null)
        {
            var header = grid.CurrentWorksheet.ColumnHeaders[column];
            if (text is not null) header.Text = text;
            header.Body = new ColumnHeaderBody(grid, header, kind, fontSize);
        }

        public static ReoGridColumnKind GetColumnKind(Worksheet sheet, int column)
            => sheet.ColumnHeaders[column].Body is ColumnHeaderBody body ? body.Kind : ReoGridColumnKind.Input;

        /// <summary>
        /// 为当前工作表中尚未设置的原生列头套用统一表头外观（已设置过的列保持其类型）。
        /// 列数增加后（如 <c>ColumnCount = n</c>）需要再次调用；<see cref="GeoChemistryNexus.Controls.ReoGridTableHost"/> 会在重置/增删列时自动调用。
        /// </summary>
        public static void StyleColumnHeaders(ReoGridControl grid)
        {
            var sheet = grid.CurrentWorksheet;
            if (sheet is null || !sheet.HasSettings(WorksheetSettings.View_ShowColumnHeader)) return;
            for (int c = 0; c < sheet.ColumnCount; c++)
            {
                var header = sheet.ColumnHeaders[c];
                if (header.Body is ColumnHeaderBody) continue;
                header.Body = new ColumnHeaderBody(grid, header, ReoGridColumnKind.Input);
            }
            // 列数可能变化，分组/单位信息需要按新列数重新取一遍。
            GetDecorations(sheet)?.Rebuild(sheet);
            ConfigureColumnHeaderHeight(sheet);
        }

        #endregion

        #region 可选：列头分组行 / 单位、行头状态点

        /// <summary>每张工作表上登记的附加信息（列头分组与单位、行头状态点、需随主题重新着色的区域）。</summary>
        private sealed class SheetDecorations
        {
            public IWorksheetColumnHeaderProvider? Columns;
            public IWorksheetRowStatusProvider? Rows;
            public EventHandler? ColumnsChanged, RowsChanged;
            public readonly List<(RangePosition Range, ReoGridColumnKind Kind)> BodyRanges = new();
            public string?[] Groups = Array.Empty<string?>();
            public string?[] Units = Array.Empty<string?>();
            public int[] GroupStart = Array.Empty<int>(), GroupEnd = Array.Empty<int>();
            public bool HasGroup, HasUnit;

            public void Rebuild(Worksheet sheet)
            {
                int n = sheet.ColumnCount;
                Groups = new string?[n]; Units = new string?[n];
                GroupStart = new int[n]; GroupEnd = new int[n];
                HasGroup = HasUnit = false;
                for (int c = 0; c < n; c++)
                {
                    var group = Columns?.GetGroup(c);
                    var unit = Columns?.GetUnit(c);
                    Groups[c] = string.IsNullOrWhiteSpace(group) ? null : group;
                    Units[c] = string.IsNullOrWhiteSpace(unit) ? null : unit;
                    HasGroup |= Groups[c] != null;
                    HasUnit |= Units[c] != null;
                    GroupStart[c] = c > 0 && Groups[c] != null && string.Equals(Groups[c - 1], Groups[c], StringComparison.Ordinal)
                        ? GroupStart[c - 1] : c;
                }
                for (int c = n - 1; c >= 0; c--)
                {
                    GroupEnd[c] = c < n - 1 && Groups[c] != null && string.Equals(Groups[c + 1], Groups[c], StringComparison.Ordinal)
                        ? GroupEnd[c + 1] : c;
                }
            }
        }

        private static readonly ConditionalWeakTable<Worksheet, SheetDecorations> DecorationTable = new();

        private static SheetDecorations? GetDecorations(Worksheet sheet)
            => DecorationTable.TryGetValue(sheet, out var decorations) ? decorations : null;

        /// <summary>
        /// 注册（或用 null 取消）列头分组行 / 单位提供者。
        /// 未注册时列头保持单行 <see cref="HeaderRowHeight"/>，与原先完全一致。
        /// </summary>
        public static void SetColumnHeaderProvider(ReoGridControl grid, IWorksheetColumnHeaderProvider? provider)
        {
            var sheet = grid.CurrentWorksheet;
            if (sheet is null) return;
            var decorations = DecorationTable.GetOrCreateValue(sheet);
            if (decorations.Columns != null && decorations.ColumnsChanged != null)
                decorations.Columns.ColumnHeaderInfoChanged -= decorations.ColumnsChanged;
            decorations.Columns = provider;
            decorations.ColumnsChanged = null;
            if (provider != null)
            {
                decorations.ColumnsChanged = (_, _) => RefreshColumnHeaderLayout(grid);
                provider.ColumnHeaderInfoChanged += decorations.ColumnsChanged;
            }
            RefreshColumnHeaderLayout(grid);
        }

        /// <summary>分组名 / 单位 / 列数变化后调用：重新计算列头高度并重绘。</summary>
        public static void RefreshColumnHeaderLayout(ReoGridControl grid)
        {
            var sheet = grid.CurrentWorksheet;
            if (sheet is null) return;
            GetDecorations(sheet)?.Rebuild(sheet);
            ConfigureColumnHeaderHeight(sheet);
            sheet.RequestInvalidate();
        }

        /// <summary>注册（或用 null 取消）行头状态点提供者。未注册时行头只显示行号。</summary>
        public static void SetRowStatusProvider(ReoGridControl grid, IWorksheetRowStatusProvider? provider)
        {
            var sheet = grid.CurrentWorksheet;
            if (sheet is null) return;
            var decorations = DecorationTable.GetOrCreateValue(sheet);
            if (decorations.Rows != null && decorations.RowsChanged != null)
                decorations.Rows.RowStatusChanged -= decorations.RowsChanged;
            decorations.Rows = provider;
            decorations.RowsChanged = null;
            if (provider != null)
            {
                decorations.RowsChanged = (_, _) => sheet.RequestInvalidate();
                provider.RowStatusChanged += decorations.RowsChanged;
            }
            StyleRowHeaders(grid);
            sheet.RequestInvalidate();
        }

        /// <summary>
        /// 给所有行头套用统一外观（行号 + 可选状态点，选中高亮与列头一致）。行数增加后需要再次调用；
        /// <see cref="GeoChemistryNexus.Controls.ReoGridTableHost"/> 会在插入行、重置时自动调用。
        /// </summary>
        public static void StyleRowHeaders(ReoGridControl grid)
        {
            var sheet = grid.CurrentWorksheet;
            if (sheet is null || sheet.RowCount > MaxStyledRowHeaders) return;
            for (int r = 0; r < sheet.RowCount; r++)
            {
                var header = sheet.RowHeaders[r];
                if (header.Body is not RowHeaderBody) header.Body = new RowHeaderBody(grid, header);
            }
            if (GetDecorations(sheet)?.Rows != null && sheet.RowHeaderWidth < RowHeaderWidthWithStatus)
                sheet.RowHeaderWidth = RowHeaderWidthWithStatus;
        }

        #endregion

        #region 文本测量

        /// <summary>按表格字体测量文字宽度（WPF 设备无关像素）。</summary>
        public static double MeasureTextWidth(string? text, bool bold = false, double? fontSizeDip = null)
        {
            // ReoGrid 字号单位为 pt，而 WPF 文本测量使用 DIP。
            var formatted = new FormattedText(text ?? string.Empty, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                bold ? BoldTypeface : RegularTypeface, fontSizeDip ?? CellFontSize * 96.0 / 72.0, Brushes.Black, GetPixelsPerDip());
            return formatted.WidthIncludingTrailingWhitespace;
        }

        /// <summary>按表头文字（粗体）计算列宽并限制在 [minWidth, maxWidth] 之间。</summary>
        public static ushort GetHeaderColumnWidth(string? headerText, int minWidth, int maxWidth, double? fontSizeDip = null)
        {
            int width = (int)Math.Ceiling(MeasureTextWidth(headerText, bold: true, fontSizeDip) + ColumnHorizontalPadding);
            return (ushort)Math.Clamp(width, minWidth, maxWidth);
        }

        private static double GetPixelsPerDip()
        {
            try
            {
                var window = Application.Current?.MainWindow;
                if (window != null) return VisualTreeHelper.GetDpi(window).PixelsPerDip;
            }
            catch
            {
                // 设计时或窗口尚未创建时取不到 DPI，回退为 1.0。
            }
            return 1.0;
        }

        #endregion

        private static Brush FrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Pen FrozenPen(Color color, double thickness = 1)
        {
            var pen = new Pen(FrozenBrush(color), thickness);
            pen.Freeze();
            return pen;
        }

        private static string ToAlphaName(int column)
        {
            var name = string.Empty;
            for (int n = column; n >= 0; n = n / 26 - 1) name = (char)('A' + n % 26) + name;
            return name;
        }

        /// <summary>
        /// 自绘原生列头（颜色全部来自主题）。
        /// 常态：输入列白底、结果列主题浅色底；选区覆盖到的列为“高亮”底色并带底部主题色线；
        /// 活动单元格所在列为实心主题色、白字。模块提供分组 / 单位时，列头上方多一条分组带、列名下方多一行单位。
        /// </summary>
        private sealed class ColumnHeaderBody : HeaderBody
        {
            private const double UnitFontSize = 10.5, GroupFontSize = 11;
            private readonly ReoGridControl _grid;
            private readonly ColumnHeader _header;
            private FormattedText? _text, _unitText, _groupText;
            private string? _label, _culture, _unit, _group;
            private double _dpi, _fontSize, _unitDpi, _groupDpi;
            private Brush? _textBrush, _unitBrush, _groupBrush;
            private readonly double? _fontSizeOverride;
            public ReoGridColumnKind Kind { get; }

            public ColumnHeaderBody(ReoGridControl grid, ColumnHeader header, ReoGridColumnKind kind, double? fontSize = null)
            {
                _grid = grid; _header = header; _fontSizeOverride = fontSize;
                Kind = kind;
            }

            public override void OnPaint(CellDrawingContext dc, unvell.ReoGrid.Graphics.Size size)
            {
                if (size.Width <= 0 || size.Height <= 0) return;
                var graphics = dc.Graphics.PlatformGraphics;
                var sheet = _grid.CurrentWorksheet;
                var theme = Theme;
                int column = _header.Index;
                var decorations = GetDecorations(sheet);
                // 模块提供了分组信息时，列头最上方是分组带，列名区域整体下移。
                double band = decorations is { HasGroup: true } ? GroupBandHeight : 0;
                var selection = sheet.SelectionRange;
                // 选区覆盖到该列即高亮；活动单元格所在列用实心主题色。
                bool selected = column >= selection.Col && column <= selection.EndCol;
                bool current = selected && column == sheet.FocusPos.Col;
                double dpi = VisualTreeHelper.GetDpi(_grid).PixelsPerDip;
                if (band > 0 && size.Height > band) PaintGroupBand(graphics, sheet, theme, decorations!, column, size, band, dpi);

                var main = new Rect(0, band, size.Width, Math.Max(0, size.Height - band));
                if (main.Height <= 0) return;
                Brush back = current ? theme.Accent
                    : selected ? theme.Highlight
                    : Kind == ReoGridColumnKind.Output ? theme.OutputBack
                    : Kind == ReoGridColumnKind.Separator ? theme.Separator : theme.White;
                graphics.DrawRectangle(back, null, main);
                // 与数据区网格线同样纤细：底部较明显，竖线由相邻列共用，只画右侧一条。
                graphics.DrawLine(theme.HeaderLinePen, new Point(0, size.Height - 0.5), new Point(size.Width, size.Height - 0.5));
                graphics.DrawLine(theme.GridPen, new Point(size.Width - 0.5, band), new Point(size.Width - 0.5, size.Height));
                if (column == 0) graphics.DrawLine(theme.GridPen, new Point(0.5, band), new Point(0.5, size.Height));
                if (selected) graphics.DrawRectangle(theme.Accent, null, new Rect(0, size.Height - 2, size.Width, 2));

                string label = _header.Text ?? ToAlphaName(column);
                // 内置列（样品 / 分组）：Text 是程序使用的列标识，这里只替换显示出来的文字。
                if (_header.Text != null && decorations?.Columns is IWorksheetColumnHeaderLabelProvider labelProvider)
                {
                    string? display = labelProvider.GetLabel(column, _header.Text);
                    if (!string.IsNullOrEmpty(display)) label = display;
                }
                if (label.Length == 0 || size.Width <= 2 || main.Height <= 2) return;
                double fontSize = _fontSizeOverride ?? HeaderFontSize;
                string culture = CultureInfo.CurrentUICulture.Name;
                Brush foreground = current ? theme.White
                    : selected || Kind == ReoGridColumnKind.Output ? theme.Accent : theme.HeaderText;
                if (_text is null || _label != label || _dpi != dpi || _fontSize != fontSize || _culture != culture
                    || !ReferenceEquals(_textBrush, foreground))
                {
                    _text = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        BoldTypeface, fontSize, foreground, dpi);
                    _label = label; _dpi = dpi; _fontSize = fontSize; _culture = culture; _textBrush = foreground;
                }

                // 单位是可选的：该列没有单位（如样品列）时，列名在主区域内垂直居中。
                string? unit = decorations != null && column < decorations.Units.Length ? decorations.Units[column] : null;
                double unitHeight = 0;
                if (unit != null)
                {
                    Brush unitBrush = current ? theme.WhiteSoft : selected ? theme.AccentSoft : theme.Mute;
                    if (_unitText is null || _unit != unit || _unitDpi != dpi || !ReferenceEquals(_unitBrush, unitBrush))
                    {
                        _unitText = new FormattedText(unit, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                            RegularTypeface, UnitFontSize, unitBrush, dpi);
                        _unit = unit; _unitDpi = dpi; _unitBrush = unitBrush;
                    }
                    unitHeight = _unitText.Height - 1;
                }
                double top = band + (main.Height - _text.Height - unitHeight) / 2 - 0.8;
                graphics.PushClip(new RectangleGeometry(new Rect(1, band + 1, size.Width - 2, main.Height - 2)));
                graphics.DrawText(_text, new Point((size.Width - _text.WidthIncludingTrailingWhitespace) / 2 - 0.4, top));
                if (unit != null && _unitText != null)
                    graphics.DrawText(_unitText, new Point((size.Width - _unitText.WidthIncludingTrailingWhitespace) / 2 - 0.4, top + _text.Height - 1));
                graphics.Pop();
            }

            /// <summary>
            /// 分组带：同一分组内部不画竖线，只在分组末尾收口，看上去就是一个合并的大单元格。
            /// 分组名只在分组的起始位置对齐，每一列各自绘制属于自己的那一段，
            /// 这样跨多列的标签无需额外的覆盖层，随表格横向滚动天然同步。
            /// </summary>
            private void PaintGroupBand(System.Windows.Media.DrawingContext graphics, Worksheet sheet, SheetBrushes theme,
                SheetDecorations decorations, int column, unvell.ReoGrid.Graphics.Size size, double band, double dpi)
            {
                string? group = column < decorations.Groups.Length ? decorations.Groups[column] : null;
                Brush back = Kind == ReoGridColumnKind.Separator ? theme.Separator
                    : group != null && Kind == ReoGridColumnKind.Output ? theme.OutputBand : theme.InputBand;
                graphics.DrawRectangle(back, null, new Rect(0, 0, size.Width, band));
                graphics.DrawLine(theme.GridPen, new Point(0, band - 0.5), new Point(size.Width, band - 0.5));
                bool lastOfGroup = column >= decorations.GroupEnd.Length || decorations.GroupEnd[column] == column;
                if (lastOfGroup) graphics.DrawLine(theme.GridPen, new Point(size.Width - 0.5, 0), new Point(size.Width - 0.5, band));
                if (column == 0) graphics.DrawLine(theme.GridPen, new Point(0.5, 0), new Point(0.5, band));
                if (group == null || column >= decorations.GroupStart.Length) return;

                Brush foreground = Kind == ReoGridColumnKind.Output ? theme.Accent : theme.Sub;
                if (_groupText is null || _group != group || _groupDpi != dpi || !ReferenceEquals(_groupBrush, foreground))
                {
                    _groupText = new FormattedText(group, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        BoldTypeface, GroupFontSize, foreground, dpi);
                    _group = group; _groupDpi = dpi; _groupBrush = foreground;
                }
                double offset = _header.Left - sheet.ColumnHeaders[decorations.GroupStart[column]].Left;
                graphics.PushClip(new RectangleGeometry(new Rect(0, 0, Math.Max(0, size.Width - (lastOfGroup ? 1 : 0)), band - 1)));
                graphics.DrawText(_groupText, new Point(10 - offset, (band - _groupText.Height) / 2 - 0.5));
                graphics.Pop();
            }
        }

        /// <summary>
        /// 自绘行头（颜色全部来自主题）：行号 + 可选状态点。
        /// 选中样式与 <see cref="ColumnHeaderBody"/> 完全一致：选区内为“高亮”底色并带右侧主题色线，活动单元格所在行为实心主题色、白字。
        /// </summary>
        private sealed class RowHeaderBody : HeaderBody
        {
            private static readonly Brush OkDot = FrozenBrush(Color.FromRgb(46, 160, 67));
            private static readonly Brush WarningDot = FrozenBrush(Color.FromRgb(224, 138, 0));
            private static readonly Brush ErrorDot = FrozenBrush(Color.FromRgb(194, 54, 75));
            private static readonly Pen StaleRing = FrozenPen(Color.FromRgb(224, 138, 0), 1.5);
            private static readonly Pen IdleRing = FrozenPen(Color.FromRgb(195, 200, 220), 1.5);
            private const double NumberFontSize = 11.5, DotRadius = 3.5, DotCenterX = 11;
            private readonly ReoGridControl _grid;
            private readonly RowHeader _header;
            private FormattedText? _text;
            private string? _label;
            private double _dpi;
            private bool _bold;
            private Brush? _textBrush;

            public RowHeaderBody(ReoGridControl grid, RowHeader header)
            {
                _grid = grid; _header = header;
            }

            public override void OnPaint(CellDrawingContext dc, unvell.ReoGrid.Graphics.Size size)
            {
                if (size.Width <= 2 || size.Height <= 2) return;
                var graphics = dc.Graphics.PlatformGraphics;
                var sheet = _grid.CurrentWorksheet;
                var theme = Theme;
                int row = _header.Index;
                var selection = sheet.SelectionRange;
                bool selected = row >= selection.Row && row <= selection.EndRow;
                bool current = selected && row == sheet.FocusPos.Row;

                Brush back = current ? theme.Accent : selected ? theme.Highlight : theme.RowHeaderBack;
                graphics.DrawRectangle(back, null, new Rect(0, 0, size.Width, size.Height));
                graphics.DrawLine(theme.GridPen, new Point(0, size.Height - 0.5), new Point(size.Width, size.Height - 0.5));
                graphics.DrawLine(theme.GridPen, new Point(size.Width - 0.5, 0), new Point(size.Width - 0.5, size.Height));
                if (selected) graphics.DrawRectangle(theme.Accent, null, new Rect(size.Width - 2, 0, 2, size.Height));

                var provider = GetDecorations(sheet)?.Rows;
                double dpi = VisualTreeHelper.GetDpi(_grid).PixelsPerDip;
                string label = (row + 1).ToString(CultureInfo.InvariantCulture);
                Brush foreground = current ? theme.White : selected ? theme.Accent : theme.Mute;
                if (_text is null || _label != label || _dpi != dpi || _bold != selected || !ReferenceEquals(_textBrush, foreground))
                {
                    _text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        selected ? NumberBoldTypeface : NumberTypeface, NumberFontSize, foreground, dpi);
                    _label = label; _dpi = dpi; _bold = selected; _textBrush = foreground;
                }
                // 有状态点时数字略向右偏，给左侧的状态点让位。
                graphics.DrawText(_text, new Point((size.Width - _text.WidthIncludingTrailingWhitespace) / 2 + (provider != null ? 5 : 0),
                    (size.Height - _text.Height) / 2));

                if (provider == null) return;
                var status = provider.GetRowStatus(row);
                var center = new Point(DotCenterX, size.Height / 2);
                switch (status)
                {
                    case WorksheetRowStatus.Ok: graphics.DrawEllipse(OkDot, null, center, DotRadius, DotRadius); break;
                    case WorksheetRowStatus.Warning: graphics.DrawEllipse(WarningDot, null, center, DotRadius, DotRadius); break;
                    case WorksheetRowStatus.Error: graphics.DrawEllipse(ErrorDot, null, center, DotRadius, DotRadius); break;
                    case WorksheetRowStatus.Stale: graphics.DrawEllipse(null, StaleRing, center, DotRadius - 0.5, DotRadius - 0.5); break;
                    case WorksheetRowStatus.NotCalculated: graphics.DrawEllipse(null, IdleRing, center, DotRadius - 0.5, DotRadius - 0.5); break;
                }
            }
        }
    }
}
