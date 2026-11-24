using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Services
{
    public sealed record TemplateFolderResult(bool Success, string Message = "", int AffectedCount = 0)
    {
        public static TemplateFolderResult Ok(int affectedCount = 0) => new(true, string.Empty, affectedCount);

        public static TemplateFolderResult Fail(string message) => new(false, message);
    }

    /// <summary>
    /// 图解模板的文件夹视图。文件夹路径 = NodeList 除最后一段外的部分，模板名 = 最后一段。
    /// 文件夹操作只改写 NodeList（及 Content 中的副本与哈希），不改动 GraphMapPath 和模板 ID。
    /// </summary>
    public static class TemplateFolderService
    {
        private const string ManifestFileName = "PlotTemplateFolders.json";

        private static readonly object ManifestLock = new();

        public static string ManifestPath => AppDataPathHelper.GetDataPath("Config", ManifestFileName);

        private static string AppLanguage => AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);

        #region 清单读写

        public static TemplateFolderManifest LoadManifest()
        {
            lock (ManifestLock)
            {
                var manifest = JsonHelper.LoadFromFileOrNew<TemplateFolderManifest>(ManifestPath);
                manifest.Custom ??= new List<TemplateFolderEntry>();
                manifest.Official ??= new List<TemplateFolderEntry>();
                return manifest;
            }
        }

        private static void SaveManifest(TemplateFolderManifest manifest)
        {
            JsonHelper.SerializeToJsonFile(manifest, ManifestPath);
        }

        #endregion

        #region 路径与名称

        public static string[] SplitPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Array.Empty<string>();

            return path.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>当前界面语言下的分类路径各段（与模板库树的构建规则一致）。</summary>
        public static string[] GetDisplaySegments(LocalizedString? nodeList)
            => SplitPath(GraphMapTemplateService.GetPathForAppLanguage(nodeList));

        public static string ResolveName(IDictionary<string, string>? names)
            => ResolveName(names, AppLanguage);

        private static string ResolveName(IDictionary<string, string>? names, string language)
            => AppCultureRegistry.GetLocalizedValue(names, language, AppCultureRegistry.DefaultAppLanguage);

        public static string GetRootName(bool isCustom)
        {
            return isCustom
                ? LanguageService.GetString("custom_templates", "Custom Templates")
                : LanguageService.GetString("official_templates", "Official Templates");
        }

        /// <summary>文件夹名与模板名不能包含路径分隔符 &gt; 及文件名非法字符。</summary>
        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return name.IndexOf('>') < 0 && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static bool NameEquals(string? a, string? b)
            => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

        private static Dictionary<string, string> CleanNames(IDictionary<string, string>? names)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (names == null)
                return result;

            foreach (var (lang, value) in names)
            {
                if (!string.IsNullOrWhiteSpace(lang) && !string.IsNullOrWhiteSpace(value))
                    result[lang] = value.Trim();
            }
            return result;
        }

        private static string? ValidateNames(Dictionary<string, string> names)
        {
            if (names.Count == 0)
                return LanguageService.GetString("folder_name_required", "Please enter a folder name.");

            if (names.Values.Any(v => !IsValidName(v)))
                return LanguageService.GetString("invalid_filename_char", "The name contains invalid characters.");

            return null;
        }

        /// <summary>某种语言下的文件夹名称；该语言缺失时按内容语言规则回退。</summary>
        private static string GetFolderName(TemplateFolderInfo folder, string language)
        {
            if (folder.LocalizedNames.TryGetValue(language, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            string resolved = ResolveName(folder.LocalizedNames, language);
            return string.IsNullOrWhiteSpace(resolved) ? folder.Name : resolved;
        }

        private static string PickName(Dictionary<string, string> names, string language)
        {
            if (names.TryGetValue(language, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            return ResolveName(names, language);
        }

        private static List<string> GetLocalizedPrefix(TemplateFolderInfo folder, string language)
            => folder.SelfAndAncestors().Reverse().Select(f => GetFolderName(f, language)).ToList();

        #endregion

        #region 文件夹树

        /// <summary>
        /// 构建某一侧（个人 / 官方）的文件夹树。官方侧的清单文件夹仅在开发者模式下显示。
        /// </summary>
        public static TemplateFolderInfo BuildFolderTree(
            bool isCustom,
            IEnumerable<GraphMapTemplateEntity>? summaries = null,
            TemplateFolderManifest? manifest = null)
        {
            summaries ??= GraphMapDatabaseService.Instance.GetSummaries();
            manifest ??= LoadManifest();

            var root = new TemplateFolderInfo { IsRoot = true, IsCustom = isCustom, Name = GetRootName(isCustom) };

            foreach (var entity in summaries)
            {
                if (entity.IsCustom != isCustom)
                    continue;

                var parts = GetDisplaySegments(entity.NodeList);
                if (parts.Length == 0)
                    continue;

                var folder = root;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    folder = GetOrAddChild(folder, parts[i]);
                    MergeNamesFromNodeList(folder, entity.NodeList, i, parts.Length);
                }
                folder.Templates.Add(entity);
            }

            if (isCustom || AppCultureRegistry.IsDeveloperModeEnabled())
            {
                foreach (var entry in manifest.GetEntries(isCustom))
                {
                    var folder = root;
                    foreach (var names in entry.Path ?? new List<Dictionary<string, string>>())
                    {
                        string name = ResolveName(names);
                        if (string.IsNullOrWhiteSpace(name))
                            break;

                        folder = GetOrAddChild(folder, name);
                        foreach (var (lang, value) in names)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                                folder.LocalizedNames.TryAdd(lang, value.Trim());
                        }
                    }
                }
            }

            return root;
        }

        public static TemplateFolderInfo? FindFolder(TemplateFolderInfo root, IReadOnlyList<string> displayPath)
        {
            var node = root;
            foreach (var part in displayPath)
            {
                node = node.Children.FirstOrDefault(c => string.Equals(c.Name, part, StringComparison.Ordinal));
                if (node == null)
                    return null;
            }
            return node;
        }

        public static TemplateFolderInfo? FindFolder(bool isCustom, IReadOnlyList<string> displayPath)
            => FindFolder(BuildFolderTree(isCustom), displayPath);

        private static TemplateFolderInfo GetOrAddChild(TemplateFolderInfo parent, string name)
        {
            var child = parent.Children.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));
            if (child != null)
                return child;

            child = new TemplateFolderInfo
            {
                Name = name,
                IsCustom = parent.IsCustom,
                Parent = parent,
                DisplayPath = parent.DisplayPath.Append(name).ToArray()
            };
            parent.Children.Add(child);
            return child;
        }

        private static void MergeNamesFromNodeList(TemplateFolderInfo folder, LocalizedString? nodeList, int index, int expectedLength)
        {
            if (nodeList?.Translations == null)
                return;

            foreach (var (lang, path) in nodeList.Translations)
            {
                var segments = SplitPath(path);
                if (segments.Length == expectedLength && index < segments.Length)
                    folder.LocalizedNames.TryAdd(lang, segments[index]);
            }
        }

        /// <summary>编辑器使用的分类片段（不含模板名），带各语言名称。</summary>
        public static List<CategoryPartModel> BuildCategoryParts(TemplateFolderInfo folder)
        {
            return folder.SelfAndAncestors().Reverse()
                .Select(f => new CategoryPartModel
                {
                    DisplayName = f.Name,
                    LocalizedNames = f.LocalizedNames.Count > 0
                        ? new Dictionary<string, string>(f.LocalizedNames, StringComparer.OrdinalIgnoreCase)
                        : null
                })
                .ToList();
        }

        /// <summary>
        /// 把清单中的文件夹（含空文件夹）合并进模板库树。
        /// </summary>
        public static void AppendManifestFolders(GraphMapTemplateNode root, TemplateFolderManifest? manifest = null)
        {
            if (root == null)
                return;

            manifest ??= LoadManifest();

            if (manifest.Custom.Count > 0)
            {
                var customRoot = GetOrAddTreeFolder(root, GetRootName(isCustom: true));
                AppendEntries(customRoot, manifest.Custom);
            }

            if (manifest.Official.Count > 0 && AppCultureRegistry.IsDeveloperModeEnabled())
                AppendEntries(root, manifest.Official);
        }

        private static void AppendEntries(GraphMapTemplateNode parent, IEnumerable<TemplateFolderEntry> entries)
        {
            foreach (var entry in entries)
            {
                var node = parent;
                foreach (var names in entry.Path ?? new List<Dictionary<string, string>>())
                {
                    string name = ResolveName(names);
                    if (string.IsNullOrWhiteSpace(name))
                        break;
                    node = GetOrAddTreeFolder(node, name);
                }
            }
        }

        private static GraphMapTemplateNode GetOrAddTreeFolder(GraphMapTemplateNode parent, string name)
        {
            var existing = parent.Children.FirstOrDefault(c => c.Name == name && string.IsNullOrEmpty(c.GraphMapPath));
            if (existing != null)
                return existing;

            var node = new GraphMapTemplateNode { Name = name, Parent = parent };
            parent.Children.Add(node);
            return node;
        }

        #endregion

        #region 同名检查

        /// <summary>同一文件夹中是否已有同名模板（当前界面语言，忽略大小写）。</summary>
        public static bool HasTemplateNamed(
            bool isCustom,
            IReadOnlyList<string> folderPath,
            string templateName,
            Guid? excludeId = null,
            IEnumerable<GraphMapTemplateEntity>? summaries = null)
        {
            summaries ??= GraphMapDatabaseService.Instance.GetSummaries();

            foreach (var entity in summaries)
            {
                if (entity.IsCustom != isCustom || (excludeId.HasValue && entity.Id == excludeId.Value))
                    continue;

                var parts = GetDisplaySegments(entity.NodeList);
                if (parts.Length != folderPath.Count + 1 || !NameEquals(parts[^1], templateName))
                    continue;

                bool sameFolder = true;
                for (int i = 0; i < folderPath.Count; i++)
                {
                    if (!string.Equals(parts[i], folderPath[i], StringComparison.Ordinal))
                    {
                        sameFolder = false;
                        break;
                    }
                }

                if (sameFolder)
                    return true;
            }

            return false;
        }

        #endregion

        #region 文件夹操作

        public static TemplateFolderResult CreateFolder(
            bool isCustom,
            IReadOnlyList<string> parentPath,
            IDictionary<string, string> names)
        {
            var cleaned = CleanNames(names);
            if (ValidateNames(cleaned) is { } error)
                return TemplateFolderResult.Fail(error);

            lock (ManifestLock)
            {
                var manifest = LoadManifest();
                var root = BuildFolderTree(isCustom, manifest: manifest);
                var parent = FindFolder(root, parentPath);
                if (parent == null)
                    return TemplateFolderResult.Fail(FolderNotFoundMessage);

                string name = ResolveName(cleaned);
                if (parent.Children.Any(c => NameEquals(c.Name, name)))
                    return TemplateFolderResult.Fail(FolderExistsMessage);

                var path = ToManifestPath(parent);
                path.Add(new Dictionary<string, string>(cleaned, StringComparer.OrdinalIgnoreCase));
                manifest.GetEntries(isCustom).Add(new TemplateFolderEntry { Path = path });
                SaveManifest(manifest);
                return TemplateFolderResult.Ok();
            }
        }

        /// <summary>修改文件夹的多语言名称：批量改写其下所有模板 NodeList 的对应段。</summary>
        public static TemplateFolderResult RenameFolder(
            bool isCustom,
            IReadOnlyList<string> folderPath,
            IDictionary<string, string> names)
        {
            var cleaned = CleanNames(names);
            if (ValidateNames(cleaned) is { } error)
                return TemplateFolderResult.Fail(error);

            lock (ManifestLock)
            {
                var manifest = LoadManifest();
                var root = BuildFolderTree(isCustom, manifest: manifest);
                var folder = FindFolder(root, folderPath);
                if (folder == null || folder.IsRoot || folder.Parent == null)
                    return TemplateFolderResult.Fail(FolderNotFoundMessage);

                string newName = ResolveName(cleaned);
                if (folder.Parent.Children.Any(c => !ReferenceEquals(c, folder) && NameEquals(c.Name, newName)))
                    return TemplateFolderResult.Fail(FolderExistsMessage);

                var templates = folder.AllTemplates().ToList();
                if (!isCustom && templates.Any(t => !t.IsInstalled))
                    return TemplateFolderResult.Fail(RequiresDownloadMessage);

                int index = folder.Depth - 1;
                int changed = 0;
                foreach (var template in templates)
                {
                    bool rewritten = RewriteTemplatePath(template.Id, (lang, segments) =>
                    {
                        if (index >= segments.Length - 1)
                            return null;
                        segments[index] = PickName(cleaned, lang);
                        return segments;
                    });
                    if (rewritten)
                        changed++;
                }

                foreach (var entry in manifest.GetEntries(isCustom))
                {
                    if (EntryStartsWith(entry, folder.DisplayPath))
                        entry.Path[index] = new Dictionary<string, string>(cleaned, StringComparer.OrdinalIgnoreCase);
                }

                SaveManifest(manifest);
                return TemplateFolderResult.Ok(changed);
            }
        }

        /// <summary>移动文件夹到另一个文件夹下（targetPath 为空表示根目录）。</summary>
        public static TemplateFolderResult MoveFolder(
            bool isCustom,
            IReadOnlyList<string> folderPath,
            IReadOnlyList<string> targetPath)
        {
            lock (ManifestLock)
            {
                var manifest = LoadManifest();
                var root = BuildFolderTree(isCustom, manifest: manifest);
                var folder = FindFolder(root, folderPath);
                var target = FindFolder(root, targetPath);
                if (folder == null || folder.IsRoot || target == null)
                    return TemplateFolderResult.Fail(FolderNotFoundMessage);

                if (target.IsSameOrDescendantOf(folder))
                    return TemplateFolderResult.Fail(LanguageService.GetString(
                        "folder_move_into_self", "A folder cannot be moved into itself or one of its subfolders."));

                if (ReferenceEquals(folder.Parent, target))
                    return TemplateFolderResult.Fail(AlreadyHereMessage);

                if (target.Children.Any(c => NameEquals(c.Name, folder.Name)))
                    return TemplateFolderResult.Fail(FolderExistsMessage);

                var templates = folder.AllTemplates().ToList();
                if (!isCustom && templates.Any(t => !t.IsInstalled))
                    return TemplateFolderResult.Fail(RequiresDownloadMessage);

                int cut = folder.Depth - 1;
                int changed = 0;
                foreach (var template in templates)
                {
                    bool rewritten = RewriteTemplatePath(template.Id, (lang, segments) =>
                    {
                        if (cut >= segments.Length - 1)
                            return null;
                        return GetLocalizedPrefix(target, lang).Concat(segments.Skip(cut)).ToArray();
                    });
                    if (rewritten)
                        changed++;
                }

                var targetEntryPath = ToManifestPath(target);
                foreach (var entry in manifest.GetEntries(isCustom))
                {
                    if (EntryStartsWith(entry, folder.DisplayPath))
                        entry.Path = targetEntryPath.Select(CloneNames).Concat(entry.Path.Skip(cut)).ToList();
                }

                SaveManifest(manifest);
                return TemplateFolderResult.Ok(changed);
            }
        }

        /// <summary>移动单个模板到指定文件夹（不能是根目录）。</summary>
        public static TemplateFolderResult MoveTemplate(Guid templateId, IReadOnlyList<string> targetPath)
        {
            var summaries = GraphMapDatabaseService.Instance.GetSummaries();
            var entity = summaries.FirstOrDefault(s => s.Id == templateId);
            if (entity == null)
                return TemplateFolderResult.Fail(LanguageService.GetString("diagram_template_source_missing", "Template not found."));

            if (targetPath.Count == 0)
                return TemplateFolderResult.Fail(SelectFolderMessage);

            var root = BuildFolderTree(entity.IsCustom, summaries);
            var target = FindFolder(root, targetPath);
            if (target == null)
                return TemplateFolderResult.Fail(FolderNotFoundMessage);

            var parts = GetDisplaySegments(entity.NodeList);
            if (parts.Length == 0)
                return TemplateFolderResult.Fail(FolderNotFoundMessage);

            if (parts.Take(parts.Length - 1).SequenceEqual(target.DisplayPath, StringComparer.Ordinal))
                return TemplateFolderResult.Fail(AlreadyHereMessage);

            if (HasTemplateNamed(entity.IsCustom, target.DisplayPath, parts[^1], entity.Id, summaries))
                return TemplateFolderResult.Fail(TemplateExistsMessage);

            if (!entity.IsCustom && !entity.IsInstalled)
                return TemplateFolderResult.Fail(RequiresDownloadMessage);

            bool rewritten = RewriteTemplatePath(templateId, (lang, segments) =>
                GetLocalizedPrefix(target, lang).Append(segments[^1]).ToArray());

            return rewritten ? TemplateFolderResult.Ok(1) : TemplateFolderResult.Fail(AlreadyHereMessage);
        }

        /// <summary>
        /// 删除文件夹。官方文件夹只能在为空时删除；个人文件夹连同其中的模板一起删除（调用方负责二次确认）。
        /// </summary>
        public static TemplateFolderResult DeleteFolder(bool isCustom, IReadOnlyList<string> folderPath)
        {
            lock (ManifestLock)
            {
                var manifest = LoadManifest();
                var root = BuildFolderTree(isCustom, manifest: manifest);
                var folder = FindFolder(root, folderPath);
                if (folder == null || folder.IsRoot)
                    return TemplateFolderResult.Fail(FolderNotFoundMessage);

                var templates = folder.AllTemplates().ToList();
                if (!isCustom && templates.Count > 0)
                    return TemplateFolderResult.Fail(LanguageService.GetString(
                        "official_folder_not_empty", "Only empty official folders can be deleted."));

                foreach (var template in templates)
                    GraphMapDatabaseService.Instance.DeleteTemplate(template.Id);

                manifest.GetEntries(isCustom).RemoveAll(e => EntryStartsWith(e, folder.DisplayPath));
                SaveManifest(manifest);
                return TemplateFolderResult.Ok(templates.Count);
            }
        }

        #endregion

        #region 内部实现

        /// <summary>
        /// 按语言逐个改写模板 NodeList；transform 返回 null 表示该语言保持不变。
        /// 同步改写 Content 中的副本并重算哈希；官方模板标记为待发布。
        /// </summary>
        private static bool RewriteTemplatePath(Guid templateId, Func<string, string[], string[]?> transform)
        {
            var db = GraphMapDatabaseService.Instance;
            var entity = db.GetTemplate(templateId);
            if (entity == null)
                return false;

            var source = entity.NodeList ?? new LocalizedString();
            var translations = new Dictionary<string, string>();
            bool changed = false;

            foreach (var (lang, path) in source.Translations ?? new Dictionary<string, string>())
            {
                var segments = SplitPath(path);
                var result = segments.Length == 0 ? null : transform(lang, segments);
                string separator = path.Contains(" > ", StringComparison.Ordinal) ? " > " : ">";
                string rewritten = result == null ? path : string.Join(separator, result);

                if (!string.Equals(rewritten, path, StringComparison.Ordinal))
                    changed = true;
                translations[lang] = rewritten;
            }

            if (!changed)
                return false;

            PersistRewrittenNodeList(db, entity, source, translations);
            return true;
        }

        /// <summary>写回改写后的 NodeList：同步 Content 中的副本并重算哈希，官方模板标记为待发布。</summary>
        private static void PersistRewrittenNodeList(
            GraphMapDatabaseService db,
            GraphMapTemplateEntity entity,
            LocalizedString source,
            Dictionary<string, string> translations)
        {
            entity.NodeList = new LocalizedString { Default = source.Default, Translations = translations };
            if (entity.Content != null)
            {
                entity.Content.NodeList = new LocalizedString
                {
                    Default = source.Default,
                    Translations = new Dictionary<string, string>(translations)
                };
                entity.FileHash = GraphMapTemplateService.ComputeTemplateContentHash(entity.Content);
            }

            entity.LastModified = DateTime.Now;
            if (!entity.IsCustom)
                entity.PendingPublish = true;

            db.UpsertTemplate(entity);
        }

        #region 批量导出 / 导入文件夹多语言名称

        private const string FolderKeySeparator = ">";

        /// <summary>取模板 NodeList 的规范路径（优先 en-US，缺失时按默认语言回退）。</summary>
        private static string[] GetCanonicalSegments(LocalizedString? nodeList)
        {
            if (nodeList?.Translations == null || nodeList.Translations.Count == 0)
                return Array.Empty<string>();

            string path = AppCultureRegistry.GetLocalizedValue(
                nodeList.Translations, AppCultureRegistry.DefaultContentLanguage, nodeList.Default);
            return SplitPath(path);
        }

        private static string JoinKey(IEnumerable<string> segments) => string.Join(FolderKeySeparator, segments);

        private static string GetParentKey(string key)
        {
            int index = key.LastIndexOf(FolderKeySeparator, StringComparison.Ordinal);
            return index < 0 ? string.Empty : key[..index];
        }

        /// <summary>
        /// 收集某一侧所有文件夹的现有多语言名称：key 为规范（en-US）路径，值为 语言码 → 名称。
        /// 来源包括模板 NodeList 与清单里的空文件夹（官方侧清单仅开发者模式）。
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> CollectFolderNames(
            bool isCustom,
            IEnumerable<GraphMapTemplateEntity> summaries,
            TemplateFolderManifest manifest)
        {
            var folders = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            Dictionary<string, string> GetOrAdd(string key)
            {
                if (!folders.TryGetValue(key, out var names))
                {
                    names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    folders[key] = names;
                }
                return names;
            }

            foreach (var entity in summaries)
            {
                if (entity.IsCustom != isCustom)
                    continue;

                var canonical = GetCanonicalSegments(entity.NodeList);
                if (canonical.Length < 2)
                    continue;

                for (int i = 0; i < canonical.Length - 1; i++)
                    GetOrAdd(JoinKey(canonical.Take(i + 1)));

                foreach (var (lang, path) in entity.NodeList.Translations)
                {
                    var segments = SplitPath(path);
                    // 与 MergeNamesFromNodeList 一致：层级数不一致的语言不可信，跳过
                    if (segments.Length != canonical.Length || !AppCultureRegistry.TryNormalize(lang, out var code))
                        continue;

                    for (int i = 0; i < canonical.Length - 1; i++)
                        GetOrAdd(JoinKey(canonical.Take(i + 1))).TryAdd(code, segments[i]);
                }
            }

            if (isCustom || AppCultureRegistry.IsDeveloperModeEnabled())
            {
                foreach (var entry in manifest.GetEntries(isCustom))
                {
                    var chain = new List<string>();
                    foreach (var names in entry.Path ?? new List<Dictionary<string, string>>())
                    {
                        string canonicalName = AppCultureRegistry.GetLocalizedValue(
                            names, AppCultureRegistry.DefaultContentLanguage, AppCultureRegistry.DefaultContentLanguage);
                        if (string.IsNullOrWhiteSpace(canonicalName))
                            break;

                        chain.Add(canonicalName.Trim());
                        var target = GetOrAdd(JoinKey(chain));
                        foreach (var (lang, value) in names)
                        {
                            if (!string.IsNullOrWhiteSpace(value) && AppCultureRegistry.TryNormalize(lang, out var code))
                                target.TryAdd(code, value.Trim());
                        }
                    }
                }
            }

            return folders;
        }

        /// <summary>
        /// 导出某一侧全部文件夹的多语言名称。每个文件夹的 names 按软件支持的全部语言补齐（缺失的为空字符串），
        /// 方便外部工具直接补全翻译。
        /// </summary>
        public static TemplateFolderNameFile ExportFolderNames(bool isCustom)
        {
            var folders = CollectFolderNames(isCustom, GraphMapDatabaseService.Instance.GetSummaries(), LoadManifest());
            var languages = AppCultureRegistry.SupportedCodes.ToList();

            var file = new TemplateFolderNameFile
            {
                Note = "Keep each \"key\" unchanged; it is the only field used to match folders on import. " +
                       "Fill in or translate the values in \"names\" (language code -> folder name). " +
                       "Empty values are ignored. Folder names must not contain \">\" or invalid file-name characters.",
                Languages = languages
            };

            foreach (var key in folders.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                var existing = folders[key];
                var names = new Dictionary<string, string>();
                foreach (var lang in languages)
                    names[lang] = existing.TryGetValue(lang, out var value) ? value : string.Empty;

                // 保留语言表之外的自定义语言，避免导出后丢失
                foreach (var (lang, value) in existing)
                {
                    if (!names.Keys.Any(k => string.Equals(k, lang, StringComparison.OrdinalIgnoreCase)))
                        names[lang] = value;
                }

                file.Folders.Add(new TemplateFolderNameEntry { Key = key, Names = names });
            }

            return file;
        }

        /// <summary>
        /// 导入文件夹多语言名称。按 key 匹配现有文件夹，逐个模板一次性改写 NodeList（可新增语言），
        /// 同时更新清单里的空文件夹。任何校验失败都不会写入。
        /// </summary>
        public static TemplateFolderResult ImportFolderNames(bool isCustom, TemplateFolderNameFile? file)
        {
            if (file?.Folders == null || file.Folders.Count == 0)
                return TemplateFolderResult.Fail("文件中没有文件夹数据。");

            // 1. 清洗导入数据
            var imported = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            foreach (var entry in file.Folders)
            {
                string key = JoinKey(SplitPath(entry?.Key));
                if (string.IsNullOrEmpty(key))
                    continue;

                var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (lang, value) in entry!.Names ?? new Dictionary<string, string>())
                {
                    string? trimmed = value?.Trim();
                    if (string.IsNullOrEmpty(trimmed) || !AppCultureRegistry.TryNormalize(lang, out var code))
                        continue;

                    if (!IsValidName(trimmed))
                        return TemplateFolderResult.Fail($"文件夹“{key}”的 {code} 名称不合法：{trimmed}");
                    names[code] = trimmed;
                }

                if (names.Count > 0)
                    imported[key] = names;
            }

            if (imported.Count == 0)
                return TemplateFolderResult.Fail("文件中没有可导入的名称。");

            lock (ManifestLock)
            {
                var db = GraphMapDatabaseService.Instance;
                var manifest = LoadManifest();
                var summaries = db.GetSummaries().Where(s => s.IsCustom == isCustom).ToList();
                var existing = CollectFolderNames(isCustom, summaries, manifest);

                // 2. 丢弃当前不存在的文件夹
                var unknownKeys = imported.Keys.Where(k => !existing.ContainsKey(k)).ToList();
                foreach (var key in unknownKeys)
                    imported.Remove(key);

                if (imported.Count == 0)
                    return TemplateFolderResult.Fail("文件中的文件夹在当前图解里都找不到，请确认没有修改 key。");

                // 3. 同级重名校验（只检查涉及导入的父级，不被历史数据拖累）
                if (FindSiblingConflict(existing, imported) is { } conflict)
                    return TemplateFolderResult.Fail(conflict);

                // 4. 规划每个模板的改写结果
                var plans = new List<(GraphMapTemplateEntity Summary, Dictionary<string, string> Translations)>();
                foreach (var summary in summaries)
                {
                    var canonical = GetCanonicalSegments(summary.NodeList);
                    if (canonical.Length < 2)
                        continue;

                    var levels = new Dictionary<string, string>?[canonical.Length - 1];
                    bool any = false;
                    for (int i = 0; i < levels.Length; i++)
                    {
                        if (imported.TryGetValue(JoinKey(canonical.Take(i + 1)), out var names))
                        {
                            levels[i] = names;
                            any = true;
                        }
                    }

                    if (!any)
                        continue;

                    var translations = BuildImportedTranslations(summary.NodeList, canonical, levels);
                    if (translations != null)
                        plans.Add((summary, translations));
                }

                if (!isCustom && plans.Any(p => !p.Summary.IsInstalled))
                {
                    int missing = plans.Count(p => !p.Summary.IsInstalled);
                    return TemplateFolderResult.Fail($"{RequiresDownloadMessage}（还有 {missing} 个图解未下载）");
                }

                // 5. 写入
                int changed = 0;
                foreach (var (summary, translations) in plans)
                {
                    var entity = db.GetTemplate(summary.Id);
                    if (entity == null)
                        continue;

                    PersistRewrittenNodeList(db, entity, entity.NodeList ?? new LocalizedString(), translations);
                    changed++;
                }

                bool manifestChanged = false;
                foreach (var entry in manifest.GetEntries(isCustom))
                {
                    var chain = new List<string>();
                    foreach (var names in entry.Path ?? new List<Dictionary<string, string>>())
                    {
                        string canonicalName = AppCultureRegistry.GetLocalizedValue(
                            names, AppCultureRegistry.DefaultContentLanguage, AppCultureRegistry.DefaultContentLanguage);
                        if (string.IsNullOrWhiteSpace(canonicalName))
                            break;

                        // 先取 key 再改名：key 是导入前的规范路径
                        chain.Add(canonicalName.Trim());
                        if (!imported.TryGetValue(JoinKey(chain), out var newNames))
                            continue;

                        foreach (var (lang, value) in newNames)
                        {
                            string? current = names.FirstOrDefault(kv => string.Equals(kv.Key, lang, StringComparison.OrdinalIgnoreCase)).Value;
                            if (!string.Equals(current, value, StringComparison.Ordinal))
                            {
                                names[lang] = value;
                                manifestChanged = true;
                            }
                        }
                    }
                }

                if (manifestChanged)
                    SaveManifest(manifest);

                string message = $"已更新 {changed} 个图解的路径";
                if (unknownKeys.Count > 0)
                    message += $"，{unknownKeys.Count} 个文件夹在当前图解里找不到，已忽略";
                return new TemplateFolderResult(true, message, changed);
            }
        }

        /// <summary>合并导入名称后检查同级同语言是否重名，返回冲突描述；无冲突返回 null。</summary>
        private static string? FindSiblingConflict(
            Dictionary<string, Dictionary<string, string>> existing,
            Dictionary<string, Dictionary<string, string>> imported)
        {
            var touchedParents = imported.Keys.Select(GetParentKey).ToHashSet(StringComparer.Ordinal);

            foreach (var group in existing.Keys
                         .Where(k => touchedParents.Contains(GetParentKey(k)))
                         .GroupBy(GetParentKey, StringComparer.Ordinal))
            {
                var perLanguage = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in group)
                {
                    var merged = new Dictionary<string, string>(existing[key], StringComparer.OrdinalIgnoreCase);
                    if (imported.TryGetValue(key, out var overrides))
                    {
                        foreach (var (lang, value) in overrides)
                            merged[lang] = value;
                    }

                    foreach (var (lang, value) in merged)
                    {
                        if (!perLanguage.TryGetValue(lang, out var set))
                        {
                            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            perLanguage[lang] = set;
                        }

                        if (!set.Add(value))
                        {
                            string parent = string.IsNullOrEmpty(group.Key) ? "根目录" : group.Key;
                            return $"“{parent}”下有重名的文件夹（{lang}）：{value}";
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 按各层级的导入名称生成模板新的 NodeList 翻译表；没有任何变化时返回 null。
        /// 已有语言：层级数一致才改写，否则保持原样；新语言：以规范路径为底，只覆盖有导入名称的层级。
        /// </summary>
        private static Dictionary<string, string>? BuildImportedTranslations(
            LocalizedString? nodeList,
            string[] canonical,
            Dictionary<string, string>?[] levels)
        {
            var existing = nodeList?.Translations ?? new Dictionary<string, string>();
            var result = new Dictionary<string, string>();
            bool changed = false;

            // 已有语言
            foreach (var (lang, path) in existing)
            {
                var segments = SplitPath(path);
                if (segments.Length != canonical.Length)
                {
                    result[lang] = path;
                    continue;
                }

                for (int i = 0; i < levels.Length; i++)
                {
                    if (levels[i] != null && TryGetName(levels[i]!, lang, out var name))
                        segments[i] = name;
                }

                string separator = path.Contains(" > ", StringComparison.Ordinal) ? " > " : ">";
                string rewritten = string.Join(separator, segments);
                if (!string.Equals(rewritten, path, StringComparison.Ordinal))
                    changed = true;
                result[lang] = rewritten;
            }

            // 新增语言
            var newLanguages = levels
                .Where(l => l != null)
                .SelectMany(l => l!.Keys)
                .Where(lang => !existing.Keys.Any(k => string.Equals(k, lang, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var lang in newLanguages)
            {
                var segments = (string[])canonical.Clone();
                for (int i = 0; i < levels.Length; i++)
                {
                    if (levels[i] != null && TryGetName(levels[i]!, lang, out var name))
                        segments[i] = name;
                }

                result[lang] = string.Join(FolderKeySeparator, segments);
                changed = true;
            }

            return changed ? result : null;
        }

        private static bool TryGetName(Dictionary<string, string> names, string language, out string name)
        {
            foreach (var (lang, value) in names)
            {
                if (string.Equals(lang, language, StringComparison.OrdinalIgnoreCase))
                {
                    name = value;
                    return true;
                }
            }

            name = string.Empty;
            return false;
        }

        #endregion

        private static List<Dictionary<string, string>> ToManifestPath(TemplateFolderInfo folder)
        {
            return folder.SelfAndAncestors().Reverse()
                .Select(f => f.LocalizedNames.Count > 0
                    ? new Dictionary<string, string>(f.LocalizedNames, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [AppLanguage] = f.Name })
                .ToList();
        }

        private static Dictionary<string, string> CloneNames(Dictionary<string, string> names)
            => new(names, StringComparer.OrdinalIgnoreCase);

        private static bool EntryStartsWith(TemplateFolderEntry entry, IReadOnlyList<string> displayPath)
        {
            if (entry.Path == null || entry.Path.Count < displayPath.Count)
                return false;

            for (int i = 0; i < displayPath.Count; i++)
            {
                if (!string.Equals(ResolveName(entry.Path[i]), displayPath[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static string FolderNotFoundMessage
            => LanguageService.GetString("folder_not_found", "The folder no longer exists. Please refresh and try again.");

        private static string FolderExistsMessage
            => LanguageService.GetString("folder_name_exists", "A folder with the same name already exists here.");

        private static string TemplateExistsMessage
            => LanguageService.GetString("template_name_exists_in_folder", "A template with the same name already exists in this folder.");

        private static string AlreadyHereMessage
            => LanguageService.GetString("folder_already_in_location", "It is already in this location.");

        private static string SelectFolderMessage
            => LanguageService.GetString("folder_select_target", "Please select a folder.");

        private static string RequiresDownloadMessage
            => LanguageService.GetString("official_folder_requires_download", "Download all templates in this folder before changing it.");

        #endregion
    }
}
