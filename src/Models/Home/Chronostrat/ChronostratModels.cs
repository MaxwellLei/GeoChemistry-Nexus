using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Models.Chronostrat
{
    /// <summary>
    /// chronostratigraphy.json 根容器。
    /// </summary>
    public sealed class ChronostratCatalog
    {
        [JsonPropertyName("source")]
        public ChronostratSourceInfo Source { get; set; } = new();

        [JsonPropertyName("units")]
        public List<ChronostratUnitRecord> Units { get; set; } = new();
    }

    /// <summary>
    /// 数据来源与版权信息。
    /// </summary>
    public sealed class ChronostratSourceInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("license")]
        public string License { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("citation")]
        public string Citation { get; set; } = string.Empty;
    }

    /// <summary>
    /// 地质年代单元级别枚举（从大到小）。
    /// </summary>
    public enum ChronostratRank
    {
        Unknown = 0,
        SuperEon = 1,   // 超宙 / 超宇
        Eon = 2,        // 宙 / 宇
        Era = 3,        // 代 / 界
        Period = 4,     // 纪 / 系
        SubPeriod = 5,  // 亚纪 / 亚系
        Epoch = 6,      // 世 / 统
        Age = 7         // 期 / 阶
    }

    /// <summary>
    /// 单个地质年代单元记录（对应 JSON 中的 units[] 条目）。
    /// </summary>
    public sealed class ChronostratUnitRecord
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("nameEn")]
        public string NameEn { get; set; } = string.Empty;

        /// <summary>年代地层名（如"寒武系"、"长兴阶"）。</summary>
        [JsonPropertyName("nameZh")]
        public string NameZh { get; set; } = string.Empty;

        /// <summary>地质年代名（如"寒武纪"、"长兴期"）。</summary>
        [JsonPropertyName("nameZhChrono")]
        public string NameZhChrono { get; set; } = string.Empty;

        [JsonPropertyName("rank")]
        public string RankRaw { get; set; } = string.Empty;

        [JsonPropertyName("colorHex")]
        public string ColorHex { get; set; } = "#FFFFFF";

        [JsonPropertyName("order")]
        public int Order { get; set; }

        /// <summary>底界年龄（百万年前，Ma）。</summary>
        [JsonPropertyName("startMa")]
        public double? StartMa { get; set; }

        /// <summary>底界年龄不确定度（±Ma）。</summary>
        [JsonPropertyName("startError")]
        public double? StartError { get; set; }

        /// <summary>顶界年龄（百万年前，Ma）。</summary>
        [JsonPropertyName("endMa")]
        public double? EndMa { get; set; }

        /// <summary>顶界年龄不确定度（±Ma）。</summary>
        [JsonPropertyName("endError")]
        public double? EndError { get; set; }

        /// <summary>是否具有已批准的 GSSP（全球界线层型剖面，金钉子）。</summary>
        [JsonPropertyName("hasGssp")]
        public bool HasGssp { get; set; }

        /// <summary>是否具有 GSSA（全球标准地层年龄）。</summary>
        [JsonPropertyName("hasGssa")]
        public bool HasGssa { get; set; }

        private Dictionary<string, string>? _names;

        [JsonPropertyName("names")]
        public Dictionary<string, string> Names
        {
            get => _names ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            set
            {
                _names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _names[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        private Dictionary<string, string>? _stratNames;

        [JsonPropertyName("stratNames")]
        public Dictionary<string, string> StratNames
        {
            get => _stratNames ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            set
            {
                _stratNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        _stratNames[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        [JsonPropertyName("parentId")]
        public string? ParentId { get; set; }

        [JsonPropertyName("childrenIds")]
        public List<string> ChildrenIds { get; set; } = new();

        // ── 运行时衍生属性（不序列化）──────────────────────────────────

        [JsonIgnore]
        public ChronostratRank Rank => RankRaw switch
        {
            "Super-Eon"  => ChronostratRank.SuperEon,
            "Eon"        => ChronostratRank.Eon,
            "Era"        => ChronostratRank.Era,
            "Period"     => ChronostratRank.Period,
            "Sub-Period" => ChronostratRank.SubPeriod,
            "Epoch"      => ChronostratRank.Epoch,
            "Age"        => ChronostratRank.Age,
            _            => ChronostratRank.Unknown
        };

        /// <summary>时间跨度（Ma），即 StartMa - EndMa。</summary>
        [JsonIgnore]
        public double? DurationMa => (StartMa.HasValue && EndMa.HasValue)
            ? StartMa.Value - EndMa.Value
            : null;

        /// <summary>是否具有底界年龄不确定度。</summary>
        [JsonIgnore]
        public bool HasStartError => StartError.HasValue;

        /// <summary>是否具有顶界年龄不确定度。</summary>
        [JsonIgnore]
        public bool HasEndError => EndError.HasValue;

        /// <summary>运行时填充的父节点引用（由 ChronostratDataService 设置）。</summary>
        [JsonIgnore]
        public ChronostratUnitRecord? Parent { get; set; }

        /// <summary>运行时填充的直接子节点列表（由 ChronostratDataService 设置）。</summary>
        [JsonIgnore]
        public List<ChronostratUnitRecord> Children { get; set; } = new();

        // ── 多语言动态呈现辅助 ──────────────────────────────────────────

        public static string ToTraditional(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (var ch in text)
            {
                sb.Append(ch switch
                {
                    '乌' => '烏', '乐' => '樂', '亚' => '亞', '伦' => '倫', '兴' => '興',
                    '凯' => '凱', '卢' => '盧', '叠' => '疊', '叶' => '葉', '吕' => '呂',
                    '圣' => '聖', '垩' => '堊', '奥' => '奧', '宪' => '憲', '宾' => '賓',
                    '层' => '層', '尔' => '爾', '岭' => '嶺', '带' => '帶', '显' => '顯',
                    '欧' => '歐', '温' => '溫', '渐' => '漸', '盖' => '蓋', '纪' => '紀',
                    '纳' => '納', '纽' => '紐', '结' => '結', '统' => '統', '维' => '維',
                    '缪' => '繆', '罗' => '羅', '莱' => '萊', '萨' => '薩', '诺' => '諾',
                    '谢' => '謝', '贝' => '貝', '赞' => '贊', '达' => '達', '运' => '運',
                    '钦' => '欽', '铁' => '鐵', '长' => '長', '门' => '門', '阶' => '階',
                    '顿' => '頓', '马' => '馬', '鲁' => '魯', '麦' => '麥', '兰' => '蘭',
                    '断' => '斷', '线' => '線', '类' => '類', '图' => '圖', '东' => '東',
                    '华' => '華', '国' => '國', '际' => '際', '广' => '廣', '变' => '變',
                    '质' => '質', '极' => '極', '标' => '標', '准' => '準', '历' => '歷',
                    '测' => '測', '数' => '數', '据' => '據', '点' => '點', '钉' => '釘',
                    '体' => '體', '库' => '庫', '托' => '托', '卡' => '卡',
                    _ => ch
                });
            }
            return sb.ToString();
        }

        public static string NormalizeCultureKey(string? cultureCode = null)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
                cultureCode = LanguageService.CurrentLanguage;

            if (string.IsNullOrWhiteSpace(cultureCode))
                cultureCode = LanguageService.GetLanguage();

            if (string.IsNullOrWhiteSpace(cultureCode))
                cultureCode = System.Globalization.CultureInfo.CurrentUICulture.Name;

            if (string.IsNullOrWhiteSpace(cultureCode))
                return "en";

            var code = cultureCode.Trim().ToLowerInvariant().Replace('_', '-');
            if (code.StartsWith("zh-tw") || code.StartsWith("zh-hk") || code.StartsWith("zh-mo") || code.StartsWith("zh-hant"))
                return "zh-tw";
            if (code.StartsWith("zh"))
                return "zh-cn";
            if (code.StartsWith("de"))
                return "de";
            if (code.StartsWith("es"))
                return "es";
            if (code.StartsWith("ja"))
                return "ja";
            if (code.StartsWith("ko"))
                return "ko";
            if (code.StartsWith("ru"))
                return "ru";
            if (code.StartsWith("fr"))
                return "fr";
            if (code.StartsWith("pt"))
                return "pt-br";
            return "en";
        }

        public string GetDisplayName(string? cultureCode = null)
        {
            var key = NormalizeCultureKey(cultureCode);

            // 1. 从 Names 多语言表中提取
            if (Names != null && Names.Count > 0)
            {
                if (Names.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                    return val;

                if (key == "zh-tw")
                {
                    if (Names.TryGetValue("zh-tw", out var valTw) && !string.IsNullOrWhiteSpace(valTw))
                        return valTw;
                    if (Names.TryGetValue("zh-cn", out var zhCn) && !string.IsNullOrWhiteSpace(zhCn))
                        return ToTraditional(zhCn);
                    if (Names.TryGetValue("zh", out var zh) && !string.IsNullOrWhiteSpace(zh))
                        return ToTraditional(zh);
                }
                else if (key == "zh-cn")
                {
                    if (Names.TryGetValue("zh-cn", out var zhCn) && !string.IsNullOrWhiteSpace(zhCn))
                        return zhCn;
                    if (Names.TryGetValue("zh", out var zh) && !string.IsNullOrWhiteSpace(zh))
                        return zh;
                }
            }

            // 2. 中文环境强制回退原生中文属性（确保中文界面下绝不会显示英文）
            if (key is "zh-cn" or "zh-tw" or "zh")
            {
                if (!string.IsNullOrWhiteSpace(NameZhChrono))
                    return key == "zh-tw" ? ToTraditional(NameZhChrono) : NameZhChrono;
                if (!string.IsNullOrWhiteSpace(NameZh))
                    return key == "zh-tw" ? ToTraditional(NameZh) : NameZh;
            }

            // 3. 其它语种最后回退到英文
            if (Names != null && Names.TryGetValue("en", out var en) && !string.IsNullOrWhiteSpace(en))
                return en;

            return !string.IsNullOrWhiteSpace(NameEn) ? NameEn : Id;
        }

        public string GetSubDisplayName(string? cultureCode = null)
        {
            var key = NormalizeCultureKey(cultureCode);
            if (key == "en")
                return string.Empty;

            var mainName = GetDisplayName(cultureCode);
            if (string.Equals(mainName, NameEn, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return NameEn;
        }

        public string GetStratigraphicName(string? cultureCode = null)
        {
            var key = NormalizeCultureKey(cultureCode);
            if (StratNames != null && StratNames.Count > 0)
            {
                if (StratNames.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                    return val;
                if (key == "zh-tw")
                {
                    if (StratNames.TryGetValue("zh-tw", out var valTw) && !string.IsNullOrWhiteSpace(valTw))
                        return valTw;
                    if (StratNames.TryGetValue("zh-cn", out var valCn) && !string.IsNullOrWhiteSpace(valCn))
                        return ToTraditional(valCn);
                    if (StratNames.TryGetValue("zh", out var valZh) && !string.IsNullOrWhiteSpace(valZh))
                        return ToTraditional(valZh);
                }
                else if (key == "zh-cn")
                {
                    if (StratNames.TryGetValue("zh-cn", out var valCn) && !string.IsNullOrWhiteSpace(valCn))
                        return valCn;
                    if (StratNames.TryGetValue("zh", out var valZh) && !string.IsNullOrWhiteSpace(valZh))
                        return valZh;
                }
            }

            if (key is "zh-cn" or "zh-tw" or "zh")
                return key == "zh-tw" ? ToTraditional(NameZh) : NameZh;

            return string.Empty;
        }

        public string GetLocalizedRank(string? cultureCode = null)
        {
            var key = NormalizeCultureKey(cultureCode);
            return Rank switch
            {
                ChronostratRank.SuperEon => key switch
                {
                    "zh-cn" => "超宙",
                    "zh-tw" => "超宙",
                    "de" => "Superäon",
                    "es" => "Supereón",
                    "ja" => "超累代",
                    "ko" => "슈퍼누대",
                    "ru" => "Суперэон",
                    "fr" => "Super-éon",
                    _ => "Super-Eon"
                },
                ChronostratRank.Eon => key switch
                {
                    "zh-cn" => "宙",
                    "zh-tw" => "宙",
                    "de" => "Äon",
                    "es" => "Eón",
                    "ja" => "累代",
                    "ko" => "누대",
                    "ru" => "Эон",
                    "fr" => "Éon",
                    _ => "Eon"
                },
                ChronostratRank.Era => key switch
                {
                    "zh-cn" => "代",
                    "zh-tw" => "代",
                    "de" => "Ära",
                    "es" => "Era",
                    "ja" => "代",
                    "ko" => "대",
                    "ru" => "Эра",
                    "fr" => "Ère",
                    _ => "Era"
                },
                ChronostratRank.Period => key switch
                {
                    "zh-cn" => "纪",
                    "zh-tw" => "紀",
                    "de" => "Periode",
                    "es" => "Periodo",
                    "ja" => "紀",
                    "ko" => "기",
                    "ru" => "Период",
                    "fr" => "Période",
                    _ => "Period"
                },
                ChronostratRank.SubPeriod => key switch
                {
                    "zh-cn" => "亚纪",
                    "zh-tw" => "亞紀",
                    "de" => "Subperiode",
                    "es" => "Subperiodo",
                    "ja" => "亜紀",
                    "ko" => "아기",
                    "ru" => "Подпериод",
                    "fr" => "Sous-période",
                    _ => "Sub-Period"
                },
                ChronostratRank.Epoch => key switch
                {
                    "zh-cn" => "世",
                    "zh-tw" => "世",
                    "de" => "Epoche",
                    "es" => "Época",
                    "ja" => "世",
                    "ko" => "세",
                    "ru" => "Эпоха",
                    "fr" => "Époque",
                    _ => "Epoch"
                },
                ChronostratRank.Age => key switch
                {
                    "zh-cn" => "期",
                    "zh-tw" => "期",
                    "de" => "Alter",
                    "es" => "Edad",
                    "ja" => "期",
                    "ko" => "절",
                    "ru" => "Век",
                    "fr" => "Âge",
                    _ => "Age"
                },
                _ => RankRaw
            };
        }

        [JsonIgnore]
        public string DisplayName => GetDisplayName();

        [JsonIgnore]
        public string SubDisplayName => GetSubDisplayName();

        [JsonIgnore]
        public string LocalizedRank => GetLocalizedRank();
    }
}
