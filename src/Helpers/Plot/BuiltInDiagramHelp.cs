using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 内置图解（稀土蛛网图、微量元素蛛网图、哈克多联图）的帮助说明。
    /// 文本直接写在程序中，并按当前图解语言显示在帮助面板里。
    /// </summary>
    public static class BuiltInDiagramHelp
    {
        public enum Kind
        {
            ReeSpider,
            TraceElementSpider,
            Harker
        }

        private readonly record struct Block(bool IsHeading, string Text);

        /// <summary>
        /// 把指定图解、指定语言的帮助写入帮助面板。缺少该语言时使用 en-US。
        /// </summary>
        public static void Show(RichTextBox richTextBox, Kind kind, string? languageCode)
        {
            var document = richTextBox.Document;
            document.Blocks.Clear();
            document.FontFamily = new FontFamily("Microsoft YaHei, Segoe UI");
            document.FontSize = 14;
            document.Foreground = Brushes.Black;
            document.PagePadding = new Thickness(18, 12, 18, 18);

            bool first = true;
            foreach (var block in Resolve(kind, languageCode))
            {
                document.Blocks.Add(new Paragraph(new Run(block.Text))
                {
                    Margin = block.IsHeading
                        ? new Thickness(0, first ? 0 : 14, 0, 6)
                        : new Thickness(0, 0, 0, 6),
                    FontSize = block.IsHeading ? 16 : 14,
                    FontWeight = block.IsHeading ? FontWeights.Bold : FontWeights.Normal
                });
                first = false;
            }
        }

        private static Block[] Resolve(Kind kind, string? languageCode)
        {
            var documents = kind switch
            {
                Kind.ReeSpider => ReeDocuments,
                Kind.TraceElementSpider => TraceDocuments,
                _ => HarkerDocuments
            };

            if (AppCultureRegistry.TryNormalize(languageCode, out string normalized) &&
                documents.TryGetValue(normalized, out Block[]? document))
            {
                return document;
            }

            return documents[AppCultureRegistry.DefaultContentLanguage];
        }

        private static Block H(string text) => new(true, text);

        private static Block P(string text) => new(false, text);

        private static readonly Dictionary<string, Block[]> ReeDocuments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] =
            [
                H("稀土元素蛛网图"),
                H("数据格式"),
                P("第一列是 Category，填写这一行的样品名称，并显示在图例中。每一行对应一条配分曲线。"),
                P("其后各列为稀土元素，单位都是 ppm。默认顺序为 La、Ce、Pr、Nd、Sm、Eu、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu。可以在设置页调整顺序，或取消不需要的元素。"),
                P("空白单元格、无法识别为数字的内容，以及小于或等于 0 的浓度不会投点。"),
                H("说明"),
                P("稀土元素蛛网图用于比较样品的稀土配分型式。"),
                P("标准化默认开启。纵轴是样品浓度与所选参考值之比，按对数坐标显示。推荐方案为球粒陨石（Sun & McDonough, 1995）。还可以选择其他球粒陨石、页岩、陆壳、原始地幔和玄武岩端元方案。所选方案的文献显示在该方案旁边。"),
                P("某个元素在当前方案中没有参考值时，标准化开启后该元素不会出现在横轴上。关闭标准化后，纵轴改为浓度（ppm）的对数坐标。"),
                H("参考文献"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["zh-TW"] =
            [
                H("稀土元素蛛網圖"),
                H("資料格式"),
                P("第一列是 Category，填寫該行的樣品名稱，並顯示在圖例中。每一行對應一條配分曲線。"),
                P("其後各列為稀土元素，單位都是 ppm。預設順序為 La、Ce、Pr、Nd、Sm、Eu、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu。可以在設定頁調整順序，或取消不需要的元素。"),
                P("空白儲存格、無法識別為數字的內容，以及小於或等於 0 的濃度不會投點。"),
                H("說明"),
                P("稀土元素蛛網圖用於比較樣品的稀土配分型式。"),
                P("標準化預設開啟。縱軸是樣品濃度與所選參考值之比，按對數座標顯示。建議方案為球粒隕石（Sun & McDonough, 1995）。還可以選擇其他球粒隕石、頁岩、陸殼、原始地函和玄武岩端元方案。所選方案的文獻顯示在該方案旁邊。"),
                P("某個元素在目前方案中沒有參考值時，標準化開啟後該元素不會出現在橫軸上。關閉標準化後，縱軸改為濃度（ppm）的對數座標。"),
                H("參考文獻"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["en-US"] =
            [
                H("REE Spider Diagram"),
                H("Data Format"),
                P("The first column is Category. Enter the sample name for that row. It is shown in the legend, and each row is drawn as one pattern."),
                P("The remaining columns are the rare earth elements, in ppm. The default order is La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu. On the settings page you can change the order or leave out elements."),
                P("Blank cells, values that are not numbers, and concentrations less than or equal to 0 are not plotted."),
                H("Notes"),
                P("An REE spider diagram compares the rare-earth patterns of samples."),
                P("Normalization is on by default. The vertical axis is a logarithmic axis of the sample concentration divided by the selected reference value. The recommended standard is chondrite (Sun & McDonough, 1995). Other chondrite, shale, continental-crust, primitive-mantle, and basalt end-member standards are also available. The citation for the selected values is shown next to that standard."),
                P("An element that has no reference value in the selected standard is omitted from the horizontal axis while normalization is on. With normalization off, the vertical axis is the logarithm of concentration in ppm."),
                H("References"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["de-DE"] =
            [
                H("REE-Spinnendiagramm"),
                H("Datenformat"),
                P("Die erste Spalte ist Category. Tragen Sie dort den Probennamen dieser Zeile ein. Er erscheint in der Legende, und jede Zeile wird als ein Muster gezeichnet."),
                P("Die übrigen Spalten sind die Seltenerdelemente in ppm. Die Standardreihenfolge ist La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu. Auf der Einstellungsseite können Sie die Reihenfolge ändern oder Elemente weglassen."),
                P("Leere Zellen, Werte, die keine Zahlen sind, und Konzentrationen kleiner oder gleich 0 werden nicht dargestellt."),
                H("Hinweise"),
                P("Ein REE-Spinnendiagramm vergleicht die Seltenerd-Muster von Proben."),
                P("Die Normierung ist standardmäßig eingeschaltet. Die vertikale Achse ist eine logarithmische Achse aus Probenkonzentration geteilt durch den gewählten Referenzwert. Empfohlen wird Chondrit (Sun & McDonough, 1995). Weitere Standards für Chondrit, Tonstein, kontinentale Kruste, primitiven Mantel und Basalt-Endglieder stehen zur Verfügung. Die Quellenangabe der gewählten Werte steht neben diesem Standard."),
                P("Ein Element ohne Referenzwert im gewählten Standard erscheint bei eingeschalteter Normierung nicht auf der horizontalen Achse. Ohne Normierung zeigt die vertikale Achse den Logarithmus der Konzentration in ppm."),
                H("Literatur"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["es-ES"] =
            [
                H("Diagrama de araña de REE"),
                H("Formato de datos"),
                P("La primera columna es Category. Escriba el nombre de la muestra de esa fila. Aparece en la leyenda, y cada fila se dibuja como un patrón."),
                P("Las demás columnas son los elementos de las tierras raras, en ppm. El orden predeterminado es La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu. En la página de configuración puede cambiar el orden u omitir elementos."),
                P("Las celdas vacías, los valores que no son números y las concentraciones menores o iguales que 0 no se representan."),
                H("Notas"),
                P("Un diagrama de araña de REE compara los patrones de tierras raras de las muestras."),
                P("La normalización está activada de forma predeterminada. El eje vertical es un eje logarítmico de la concentración de la muestra dividida por el valor de referencia elegido. El estándar recomendado es la condrita (Sun y McDonough, 1995). También hay otros estándares de condrita, lutita, corteza continental, manto primitivo y miembros extremos basálticos. La cita de los valores elegidos aparece junto a ese estándar."),
                P("Un elemento sin valor de referencia en el estándar elegido no aparece en el eje horizontal mientras la normalización está activada. Si se desactiva, el eje vertical es el logaritmo de la concentración en ppm."),
                H("Referencias"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["ja-JP"] =
            [
                H("希土類元素スパイダー図"),
                H("データ形式"),
                P("最初の列は Category です。その行の試料名を入力します。凡例に表示され、1 行が 1 本のパターンになります。"),
                P("残りの列は希土類元素で、単位はすべて ppm です。初期順序は La、Ce、Pr、Nd、Sm、Eu、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu です。設定ページで順序を変えるか、不要な元素を外せます。"),
                P("空欄、数値として読めない内容、および 0 以下の濃度はプロットされません。"),
                H("説明"),
                P("希土類元素スパイダー図は、試料の希土類パターンを比較するために使います。"),
                P("規格化は初期状態で有効です。縦軸は、試料濃度を選択した参照値で割った値の対数軸です。推奨はコンドライト（Sun & McDonough, 1995）です。ほかにコンドライト、頁岩、大陸地殻、始原的マントル、玄武岩端成分の方案を選べます。選択中の値の文献は、その方案の横に表示されます。"),
                P("選択した方案に参照値がない元素は、規格化が有効なあいだ横軸に出ません。規格化を切ると、縦軸は濃度（ppm）の対数軸になります。"),
                H("参考文献"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["ko-KR"] =
            [
                H("희토류 원소 거미줄 그림"),
                H("데이터 형식"),
                P("첫 열은 Category입니다. 그 행의 시료 이름을 입력합니다. 범례에 표시되며, 한 행이 하나의 패턴이 됩니다."),
                P("나머지 열은 희토류 원소이며 단위는 모두 ppm입니다. 기본 순서는 La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu입니다. 설정 페이지에서 순서를 바꾸거나 원소를 뺄 수 있습니다."),
                P("빈 칸, 숫자로 읽을 수 없는 값, 0 이하의 농도는 표시되지 않습니다."),
                H("설명"),
                P("희토류 원소 거미줄 그림은 시료의 희토류 패턴을 비교하는 데 사용합니다."),
                P("표준화는 기본적으로 켜져 있습니다. 세로축은 시료 농도를 선택한 기준값으로 나눈 값의 로그 축입니다. 권장 기준은 콘드라이트(Sun & McDonough, 1995)입니다. 다른 콘드라이트, 셰일, 대륙 지각, 원시 맨틀, 현무암 단성분 기준도 선택할 수 있습니다. 선택한 값의 문헌은 그 기준 옆에 표시됩니다."),
                P("선택한 기준에 기준값이 없는 원소는 표준화가 켜져 있는 동안 가로축에 나타나지 않습니다. 표준화를 끄면 세로축은 농도(ppm)의 로그 축이 됩니다."),
                H("참고문헌"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["ru-RU"] =
            [
                H("Спайдер-диаграмма РЗЭ"),
                H("Формат данных"),
                P("Первый столбец — Category. Введите название образца этой строки. Оно показывается в легенде, и каждая строка строится как одна кривая."),
                P("Остальные столбцы — редкоземельные элементы, в ppm. Порядок по умолчанию: La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu. На странице настроек можно изменить порядок или исключить элементы."),
                P("Пустые ячейки, значения, которые не являются числами, и концентрации меньше или равные 0 не наносятся."),
                H("Примечания"),
                P("Спайдер-диаграмма РЗЭ служит для сравнения спектров редкоземельных элементов."),
                P("Нормировка включена по умолчанию. Вертикальная ось — логарифмическая ось отношения концентрации в образце к выбранному эталону. Рекомендуемый эталон — хондрит (Sun & McDonough, 1995). Доступны также другие эталоны хондрита, сланца, континентальной коры, примитивной мантии и базальтовых конечных членов. Ссылка на выбранные значения показана рядом с эталоном."),
                P("Элемент, для которого в выбранном эталоне нет значения, при включённой нормировке не появляется на горизонтальной оси. Если нормировку выключить, вертикальная ось показывает логарифм концентрации в ppm."),
                H("Литература"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["fr-FR"] =
            [
                H("Diagramme en araignée des terres rares"),
                H("Format des données"),
                P("La première colonne est Category. Saisissez le nom de l’échantillon de la ligne. Il apparaît dans la légende, et chaque ligne est tracée comme un spectre."),
                P("Les autres colonnes sont les terres rares, en ppm. L’ordre par défaut est La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu. Sur la page de réglages, vous pouvez modifier l’ordre ou retirer des éléments."),
                P("Les cellules vides, les valeurs qui ne sont pas des nombres et les concentrations inférieures ou égales à 0 ne sont pas tracées."),
                H("Remarques"),
                P("Le diagramme en araignée des terres rares sert à comparer les spectres de terres rares des échantillons."),
                P("La normalisation est activée par défaut. L’axe vertical est un axe logarithmique du rapport entre la concentration de l’échantillon et la valeur de référence choisie. La référence recommandée est la chondrite (Sun & McDonough, 1995). D’autres références de chondrite, de shale, de croûte continentale, de manteau primitif et de pôles basaltiques sont disponibles. La référence bibliographique des valeurs choisies est affichée à côté de cette norme."),
                P("Un élément sans valeur de référence dans la norme choisie n’apparaît pas sur l’axe horizontal tant que la normalisation est activée. Sans normalisation, l’axe vertical est le logarithme de la concentration en ppm."),
                H("Références"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["pt-BR"] =
            [
                H("Diagrama aranha de ETR"),
                H("Formato dos dados"),
                P("A primeira coluna é Category. Informe o nome da amostra da linha. Ele aparece na legenda, e cada linha é desenhada como um padrão."),
                P("As demais colunas são os elementos terras raras, em ppm. A ordem padrão é La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu. Na página de configuração é possível mudar a ordem ou omitir elementos."),
                P("Células vazias, valores que não são números e concentrações menores ou iguais a 0 não são plotados."),
                H("Notas"),
                P("O diagrama aranha de ETR compara os padrões de terras raras das amostras."),
                P("A normalização fica ligada por padrão. O eixo vertical é um eixo logarítmico da concentração da amostra dividida pelo valor de referência escolhido. O padrão recomendado é o condrito (Sun & McDonough, 1995). Também há outros padrões de condrito, folhelho, crosta continental, manto primitivo e membros extremos basálticos. A citação dos valores escolhidos aparece ao lado desse padrão."),
                P("Um elemento sem valor de referência no padrão escolhido fica fora do eixo horizontal enquanto a normalização está ligada. Com a normalização desligada, o eixo vertical é o logaritmo da concentração em ppm."),
                H("Referências"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ]
        };

        private static readonly Dictionary<string, Block[]> TraceDocuments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] =
            [
                H("微量元素蛛网图"),
                H("数据格式"),
                P("第一列是 Category，填写这一行的样品名称，并显示在图例中。每一行对应一条配分曲线。"),
                P("其后各列为微量元素，单位都是 ppm。K 和 Ti 也按 ppm 填写，不按 wt% 填写。默认顺序为 Cs、Rb、Ba、Th、U、Nb、Ta、K、La、Ce、Pb、Pr、Sr、Nd、Zr、Hf、Sm、Eu、Ti、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu、Y。可以在设置页调整顺序，或取消不需要的元素。"),
                P("空白单元格、无法识别为数字的内容，以及小于或等于 0 的浓度不会投点。"),
                H("说明"),
                P("微量元素蛛网图用于比较样品的不相容元素配分型式。"),
                P("标准化默认开启。纵轴是样品浓度与所选参考值之比，按对数坐标显示。推荐方案为原始地幔（Sun & McDonough, 1989）。还可以选择球粒陨石、陆壳、原始地幔和玄武岩端元方案。所选方案的文献显示在该方案旁边。"),
                P("某个元素在当前方案中没有参考值时，标准化开启后该元素不会出现在横轴上。关闭标准化后，纵轴改为浓度（ppm）的对数坐标。"),
                H("参考文献"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["zh-TW"] =
            [
                H("微量元素蛛網圖"),
                H("資料格式"),
                P("第一列是 Category，填寫該行的樣品名稱，並顯示在圖例中。每一行對應一條配分曲線。"),
                P("其後各列為微量元素，單位都是 ppm。K 和 Ti 也按 ppm 填寫，不按 wt% 填寫。預設順序為 Cs、Rb、Ba、Th、U、Nb、Ta、K、La、Ce、Pb、Pr、Sr、Nd、Zr、Hf、Sm、Eu、Ti、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu、Y。可以在設定頁調整順序，或取消不需要的元素。"),
                P("空白儲存格、無法識別為數字的內容，以及小於或等於 0 的濃度不會投點。"),
                H("說明"),
                P("微量元素蛛網圖用於比較樣品的不相容元素配分型式。"),
                P("標準化預設開啟。縱軸是樣品濃度與所選參考值之比，按對數座標顯示。建議方案為原始地函（Sun & McDonough, 1989）。還可以選擇球粒隕石、陸殼、原始地函和玄武岩端元方案。所選方案的文獻顯示在該方案旁邊。"),
                P("某個元素在目前方案中沒有參考值時，標準化開啟後該元素不會出現在橫軸上。關閉標準化後，縱軸改為濃度（ppm）的對數座標。"),
                H("參考文獻"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["en-US"] =
            [
                H("Trace-Element Spider Diagram"),
                H("Data Format"),
                P("The first column is Category. Enter the sample name for that row. It is shown in the legend, and each row is drawn as one pattern."),
                P("The remaining columns are trace elements, in ppm. Enter K and Ti in ppm as well, not in wt%. The default order is Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y. On the settings page you can change the order or leave out elements."),
                P("Blank cells, values that are not numbers, and concentrations less than or equal to 0 are not plotted."),
                H("Notes"),
                P("A trace-element spider diagram compares incompatible-element patterns."),
                P("Normalization is on by default. The vertical axis is a logarithmic axis of the sample concentration divided by the selected reference value. The recommended standard is primitive mantle (Sun & McDonough, 1989). Chondrite, continental-crust, primitive-mantle, and basalt end-member standards are also available. The citation for the selected values is shown next to that standard."),
                P("An element that has no reference value in the selected standard is omitted from the horizontal axis while normalization is on. With normalization off, the vertical axis is the logarithm of concentration in ppm."),
                H("References"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["de-DE"] =
            [
                H("Spurenelement-Spinnendiagramm"),
                H("Datenformat"),
                P("Die erste Spalte ist Category. Tragen Sie dort den Probennamen dieser Zeile ein. Er erscheint in der Legende, und jede Zeile wird als ein Muster gezeichnet."),
                P("Die übrigen Spalten sind Spurenelemente in ppm. K und Ti werden ebenfalls in ppm eingetragen, nicht in wt%. Die Standardreihenfolge ist Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y. Auf der Einstellungsseite können Sie die Reihenfolge ändern oder Elemente weglassen."),
                P("Leere Zellen, Werte, die keine Zahlen sind, und Konzentrationen kleiner oder gleich 0 werden nicht dargestellt."),
                H("Hinweise"),
                P("Ein Spurenelement-Spinnendiagramm vergleicht Muster inkompatibler Elemente."),
                P("Die Normierung ist standardmäßig eingeschaltet. Die vertikale Achse ist eine logarithmische Achse aus Probenkonzentration geteilt durch den gewählten Referenzwert. Empfohlen wird der primitive Mantel (Sun & McDonough, 1989). Standards für Chondrit, kontinentale Kruste, primitiven Mantel und Basalt-Endglieder stehen ebenfalls zur Verfügung. Die Quellenangabe der gewählten Werte steht neben diesem Standard."),
                P("Ein Element ohne Referenzwert im gewählten Standard erscheint bei eingeschalteter Normierung nicht auf der horizontalen Achse. Ohne Normierung zeigt die vertikale Achse den Logarithmus der Konzentration in ppm."),
                H("Literatur"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["es-ES"] =
            [
                H("Diagrama de araña de elementos traza"),
                H("Formato de datos"),
                P("La primera columna es Category. Escriba el nombre de la muestra de esa fila. Aparece en la leyenda, y cada fila se dibuja como un patrón."),
                P("Las demás columnas son elementos traza, en ppm. K y Ti también se introducen en ppm, no en wt%. El orden predeterminado es Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y. En la página de configuración puede cambiar el orden u omitir elementos."),
                P("Las celdas vacías, los valores que no son números y las concentraciones menores o iguales que 0 no se representan."),
                H("Notas"),
                P("Un diagrama de araña de elementos traza compara patrones de elementos incompatibles."),
                P("La normalización está activada de forma predeterminada. El eje vertical es un eje logarítmico de la concentración de la muestra dividida por el valor de referencia elegido. El estándar recomendado es el manto primitivo (Sun y McDonough, 1989). También hay estándares de condrita, corteza continental, manto primitivo y miembros extremos basálticos. La cita de los valores elegidos aparece junto a ese estándar."),
                P("Un elemento sin valor de referencia en el estándar elegido no aparece en el eje horizontal mientras la normalización está activada. Si se desactiva, el eje vertical es el logaritmo de la concentración en ppm."),
                H("Referencias"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["ja-JP"] =
            [
                H("微量元素スパイダー図"),
                H("データ形式"),
                P("最初の列は Category です。その行の試料名を入力します。凡例に表示され、1 行が 1 本のパターンになります。"),
                P("残りの列は微量元素で、単位はすべて ppm です。K と Ti も wt% ではなく ppm で入力します。初期順序は Cs、Rb、Ba、Th、U、Nb、Ta、K、La、Ce、Pb、Pr、Sr、Nd、Zr、Hf、Sm、Eu、Ti、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu、Y です。設定ページで順序を変えるか、不要な元素を外せます。"),
                P("空欄、数値として読めない内容、および 0 以下の濃度はプロットされません。"),
                H("説明"),
                P("微量元素スパイダー図は、試料の不適合元素パターンを比較するために使います。"),
                P("規格化は初期状態で有効です。縦軸は、試料濃度を選択した参照値で割った値の対数軸です。推奨は始原的マントル（Sun & McDonough, 1989）です。コンドライト、大陸地殻、始原的マントル、玄武岩端成分の方案も選べます。選択中の値の文献は、その方案の横に表示されます。"),
                P("選択した方案に参照値がない元素は、規格化が有効なあいだ横軸に出ません。規格化を切ると、縦軸は濃度（ppm）の対数軸になります。"),
                H("参考文献"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["ko-KR"] =
            [
                H("미량원소 거미줄 그림"),
                H("데이터 형식"),
                P("첫 열은 Category입니다. 그 행의 시료 이름을 입력합니다. 범례에 표시되며, 한 행이 하나의 패턴이 됩니다."),
                P("나머지 열은 미량원소이며 단위는 모두 ppm입니다. K와 Ti도 wt%가 아니라 ppm으로 입력합니다. 기본 순서는 Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y입니다. 설정 페이지에서 순서를 바꾸거나 원소를 뺄 수 있습니다."),
                P("빈 칸, 숫자로 읽을 수 없는 값, 0 이하의 농도는 표시되지 않습니다."),
                H("설명"),
                P("미량원소 거미줄 그림은 시료의 불호정 원소 패턴을 비교하는 데 사용합니다."),
                P("표준화는 기본적으로 켜져 있습니다. 세로축은 시료 농도를 선택한 기준값으로 나눈 값의 로그 축입니다. 권장 기준은 원시 맨틀(Sun & McDonough, 1989)입니다. 콘드라이트, 대륙 지각, 원시 맨틀, 현무암 단성분 기준도 선택할 수 있습니다. 선택한 값의 문헌은 그 기준 옆에 표시됩니다."),
                P("선택한 기준에 기준값이 없는 원소는 표준화가 켜져 있는 동안 가로축에 나타나지 않습니다. 표준화를 끄면 세로축은 농도(ppm)의 로그 축이 됩니다."),
                H("참고문헌"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["ru-RU"] =
            [
                H("Спайдер-диаграмма микроэлементов"),
                H("Формат данных"),
                P("Первый столбец — Category. Введите название образца этой строки. Оно показывается в легенде, и каждая строка строится как одна кривая."),
                P("Остальные столбцы — микроэлементы, в ppm. K и Ti также вводятся в ppm, а не в wt%. Порядок по умолчанию: Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y. На странице настроек можно изменить порядок или исключить элементы."),
                P("Пустые ячейки, значения, которые не являются числами, и концентрации меньше или равные 0 не наносятся."),
                H("Примечания"),
                P("Спайдер-диаграмма микроэлементов служит для сравнения спектров несовместимых элементов."),
                P("Нормировка включена по умолчанию. Вертикальная ось — логарифмическая ось отношения концентрации в образце к выбранному эталону. Рекомендуемый эталон — примитивная мантия (Sun & McDonough, 1989). Доступны также эталоны хондрита, континентальной коры, примитивной мантии и базальтовых конечных членов. Ссылка на выбранные значения показана рядом с эталоном."),
                P("Элемент, для которого в выбранном эталоне нет значения, при включённой нормировке не появляется на горизонтальной оси. Если нормировку выключить, вертикальная ось показывает логарифм концентрации в ppm."),
                H("Литература"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["fr-FR"] =
            [
                H("Diagramme en araignée des éléments en traces"),
                H("Format des données"),
                P("La première colonne est Category. Saisissez le nom de l’échantillon de la ligne. Il apparaît dans la légende, et chaque ligne est tracée comme un spectre."),
                P("Les autres colonnes sont les éléments en traces, en ppm. K et Ti se saisissent aussi en ppm, et non en wt%. L’ordre par défaut est Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y. Sur la page de réglages, vous pouvez modifier l’ordre ou retirer des éléments."),
                P("Les cellules vides, les valeurs qui ne sont pas des nombres et les concentrations inférieures ou égales à 0 ne sont pas tracées."),
                H("Remarques"),
                P("Le diagramme en araignée des éléments en traces sert à comparer les spectres d’éléments incompatibles."),
                P("La normalisation est activée par défaut. L’axe vertical est un axe logarithmique du rapport entre la concentration de l’échantillon et la valeur de référence choisie. La référence recommandée est le manteau primitif (Sun & McDonough, 1989). Des références de chondrite, de croûte continentale, de manteau primitif et de pôles basaltiques sont aussi disponibles. La référence bibliographique des valeurs choisies est affichée à côté de cette norme."),
                P("Un élément sans valeur de référence dans la norme choisie n’apparaît pas sur l’axe horizontal tant que la normalisation est activée. Sans normalisation, l’axe vertical est le logarithme de la concentration en ppm."),
                H("Références"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ],
            ["pt-BR"] =
            [
                H("Diagrama aranha de elementos-traço"),
                H("Formato dos dados"),
                P("A primeira coluna é Category. Informe o nome da amostra da linha. Ele aparece na legenda, e cada linha é desenhada como um padrão."),
                P("As demais colunas são elementos-traço, em ppm. K e Ti também são informados em ppm, e não em wt%. A ordem padrão é Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y. Na página de configuração é possível mudar a ordem ou omitir elementos."),
                P("Células vazias, valores que não são números e concentrações menores ou iguais a 0 não são plotados."),
                H("Notas"),
                P("O diagrama aranha de elementos-traço compara padrões de elementos incompatíveis."),
                P("A normalização fica ligada por padrão. O eixo vertical é um eixo logarítmico da concentração da amostra dividida pelo valor de referência escolhido. O padrão recomendado é o manto primitivo (Sun & McDonough, 1989). Também há padrões de condrito, crosta continental, manto primitivo e membros extremos basálticos. A citação dos valores escolhidos aparece ao lado desse padrão."),
                P("Um elemento sem valor de referência no padrão escolhido fica fora do eixo horizontal enquanto a normalização está ligada. Com a normalização desligada, o eixo vertical é o logaritmo da concentração em ppm."),
                H("Referências"),
                P("Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. In A. D. Saunders & M. J. Norry (Eds.), Magmatism in the ocean basins (pp. 313–345). Geological Society of London."),
                P("Sun, S.-s., & McDonough, W. F. (1995). The composition of the Earth. Chemical Geology, 120(3–4), 223–253.")
            ]
        };

        private static readonly Dictionary<string, Block[]> HarkerDocuments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] =
            [
                H("哈克图解多联图"),
                H("数据格式"),
                P("第一列是 Category，表示样品分组。同一个分组在所有面板中使用相同的符号和颜色。"),
                P("其余列由设置页决定：先是各面板共用的 X 变量，再按面板顺序排列各个 Y 变量。列名使用变量键，例如 SiO2、Al2O3、Fe2O3T。"),
                P("主量氧化物的单位是 wt%。Fe2O3T 表示以 Fe2O3 计的全铁。微量元素 Zr、Rb、Sr、Ba、Cr、Ni 的单位是 ppm。"),
                P("Na2O+K2O 和 Mg# 需要直接填写，程序不会用其他列计算。Na2O+K2O 的单位是 wt%；Mg# 是无量纲镁值。"),
                P("X 变量可以是 SiO2、MgO、TiO2 或 K2O，默认是 SiO2。默认的 Y 面板为 TiO2、Al2O3、Fe2O3T、MgO、CaO、Na2O、K2O、P2O5。"),
                P("某一行缺少 X，或所有 Y 都没有有效数字时，该行不投点。某个 Y 缺失时，只在对应面板中省略该点。"),
                H("说明"),
                P("哈克图解把多个变量对同一个横轴排成一组图，用来观察岩浆分异等过程中的成分协变。经典用法是以 SiO2 为横轴。"),
                P("各面板共用横轴。进入绘图后，横轴联动默认打开。"),
                P("悬停联动也默认打开。鼠标停在某个面板的样点上时，其余面板会标出同一个样点。"),
                H("参考文献"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["zh-TW"] =
            [
                H("哈克圖解多聯圖"),
                H("資料格式"),
                P("第一列是 Category，表示樣品分組。同一個分組在所有面板中使用相同的符號和顏色。"),
                P("其餘列由設定頁決定：先是各面板共用的 X 變數，再按面板順序排列各個 Y 變數。列名使用變數鍵，例如 SiO2、Al2O3、Fe2O3T。"),
                P("主量氧化物的單位是 wt%。Fe2O3T 表示以 Fe2O3 計的全鐵。微量元素 Zr、Rb、Sr、Ba、Cr、Ni 的單位是 ppm。"),
                P("Na2O+K2O 和 Mg# 需要直接填寫，程式不會用其他列計算。Na2O+K2O 的單位是 wt%；Mg# 是無因次鎂值。"),
                P("X 變數可以是 SiO2、MgO、TiO2 或 K2O，預設是 SiO2。預設的 Y 面板為 TiO2、Al2O3、Fe2O3T、MgO、CaO、Na2O、K2O、P2O5。"),
                P("某一行缺少 X，或所有 Y 都沒有有效數字時，該行不投點。某個 Y 缺失時，只在對應面板中省略該點。"),
                H("說明"),
                P("哈克圖解把多個變數對同一個橫軸排成一組圖，用來觀察岩漿分異等過程中的成分共變。經典用法是以 SiO2 為橫軸。"),
                P("各面板共用橫軸。進入繪圖後，橫軸聯動預設打開。"),
                P("懸停聯動也預設打開。滑鼠停在某個面板的樣點上時，其餘面板會標出同一個樣點。"),
                H("參考文獻"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["en-US"] =
            [
                H("Harker Diagrams"),
                H("Data Format"),
                P("The first column is Category, the sample group. The same group uses the same symbol and color on every panel."),
                P("The other columns come from the settings page: the X variable shared by every panel, then each Y variable in panel order. Column headers are the variable keys, such as SiO2, Al2O3, and Fe2O3T."),
                P("Major oxides are in wt%. Fe2O3T is total iron expressed as Fe2O3. The trace elements Zr, Rb, Sr, Ba, Cr, and Ni are in ppm."),
                P("Type Na2O+K2O and Mg# directly. The program does not calculate them from the other columns. Na2O+K2O is in wt%. Mg# is the dimensionless magnesium number."),
                P("X can be SiO2, MgO, TiO2, or K2O. The default is SiO2. The default Y panels are TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O, and P2O5."),
                P("A row is skipped when X is missing, or when none of the Y values is a number. A missing Y omits the point on that panel only."),
                H("Notes"),
                P("Harker diagrams place several variables against one shared horizontal axis, so covariation during processes such as magmatic differentiation can be compared. The classic choice is SiO2 on X."),
                P("The panels share the horizontal axis. Linking of that axis is on when plotting starts."),
                P("Sample hover is linked as well. Resting on a point in one panel marks the same sample on the other panels."),
                H("References"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["de-DE"] =
            [
                H("Harker-Diagramme"),
                H("Datenformat"),
                P("Die erste Spalte ist Category, die Probengruppe. Dieselbe Gruppe verwendet in jedem Teilbild dasselbe Symbol und dieselbe Farbe."),
                P("Die übrigen Spalten ergeben sich aus der Einstellungsseite: zuerst die von allen Teilbildern gemeinsam verwendete X-Variable, danach jede Y-Variable in der Reihenfolge der Teilbilder. Die Spaltenköpfe sind die Variablenschlüssel, zum Beispiel SiO2, Al2O3 und Fe2O3T."),
                P("Hauptelementoxide werden in wt% angegeben. Fe2O3T ist das Gesamteisen, ausgedrückt als Fe2O3. Die Spurenelemente Zr, Rb, Sr, Ba, Cr und Ni werden in ppm angegeben."),
                P("Na2O+K2O und Mg# werden direkt eingetragen. Das Programm berechnet sie nicht aus den anderen Spalten. Na2O+K2O ist in wt% anzugeben. Mg# ist die dimensionslose Magnesiumzahl."),
                P("X kann SiO2, MgO, TiO2 oder K2O sein. Voreingestellt ist SiO2. Die voreingestellten Y-Teilbilder sind TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O und P2O5."),
                P("Eine Zeile wird ausgelassen, wenn X fehlt oder keiner der Y-Werte eine Zahl ist. Fehlt nur ein Y-Wert, entfällt der Punkt allein in diesem Teilbild."),
                H("Hinweise"),
                P("Harker-Diagramme stellen mehrere Variablen gegen dieselbe horizontale Achse, damit Kovariation bei Vorgängen wie magmatischer Differentiation vergleichbar wird. Die klassische Wahl für X ist SiO2."),
                P("Die Teilbilder teilen sich die horizontale Achse. Die Verknüpfung dieser Achse ist beim Start der Darstellung eingeschaltet."),
                P("Die Kopplung beim Zeigen ist ebenfalls eingeschaltet. Zeigt der Mauszeiger auf einen Punkt in einem Teilbild, wird dieselbe Probe in den anderen Teilbildern markiert."),
                H("Literatur"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["es-ES"] =
            [
                H("Diagramas de Harker"),
                H("Formato de datos"),
                P("La primera columna es Category, el grupo de muestras. El mismo grupo usa el mismo símbolo y el mismo color en todos los paneles."),
                P("Las demás columnas proceden de la página de configuración: primero la variable X compartida por todos los paneles y después cada variable Y en el orden de los paneles. Los encabezados son las claves de las variables, como SiO2, Al2O3 y Fe2O3T."),
                P("Los óxidos mayores se expresan en wt%. Fe2O3T es el hierro total expresado como Fe2O3. Los elementos traza Zr, Rb, Sr, Ba, Cr y Ni se expresan en ppm."),
                P("Na2O+K2O y Mg# se escriben directamente. El programa no los calcula a partir de las otras columnas. Na2O+K2O está en wt%. Mg# es el número de magnesio, sin unidades."),
                P("X puede ser SiO2, MgO, TiO2 o K2O. El valor predeterminado es SiO2. Los paneles Y predeterminados son TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O y P2O5."),
                P("Una fila se omite si falta X o si ninguno de los valores Y es numérico. Si falta un Y, el punto solo se omite en ese panel."),
                H("Notas"),
                P("Los diagramas de Harker colocan varias variables frente a un mismo eje horizontal para comparar la covariación durante procesos como la diferenciación magmática. La elección clásica es SiO2 en X."),
                P("Los paneles comparten el eje horizontal. El enlace de ese eje está activado al empezar a representar."),
                P("El enlace del cursor también está activado. Al situarse sobre un punto de un panel, la misma muestra se marca en los demás."),
                H("Referencias"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["ja-JP"] =
            [
                H("ハーカー図（多連図）"),
                H("データ形式"),
                P("最初の列は Category で、試料のグループです。同じグループはすべてのパネルで同じ記号と色を使います。"),
                P("残りの列は設定ページで決まります。最初が全パネル共通の X 変数、その後にパネル順の各 Y 変数が続きます。列名は SiO2、Al2O3、Fe2O3T のような変数キーです。"),
                P("主要酸化物の単位は wt% です。Fe2O3T は Fe2O3 として表した全鉄です。微量元素 Zr、Rb、Sr、Ba、Cr、Ni の単位は ppm です。"),
                P("Na2O+K2O と Mg# は直接入力します。プログラムは他の列から計算しません。Na2O+K2O の単位は wt%、Mg# は無次元のマグネシウム値です。"),
                P("X 変数は SiO2、MgO、TiO2、K2O から選べ、初期値は SiO2 です。初期の Y パネルは TiO2、Al2O3、Fe2O3T、MgO、CaO、Na2O、K2O、P2O5 です。"),
                P("X がない行、または有効な数値が一つもない Y の行はプロットされません。ある Y だけが欠けている場合は、そのパネルの点だけが省かれます。"),
                H("説明"),
                P("ハーカー図は、複数の変数を共通の横軸に対して並べ、マグマ分化などの過程における組成の共変動を見るための図です。古典的には横軸に SiO2 を取ります。"),
                P("各パネルは横軸を共有します。作図を始めると、横軸の連動は初期状態で有効です。"),
                P("ホバー連動も初期状態で有効です。あるパネルの試料点に合わせると、他のパネルでも同じ試料点が示されます。"),
                H("参考文献"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["ko-KR"] =
            [
                H("하커 다연도"),
                H("데이터 형식"),
                P("첫 열은 Category이며 시료 그룹입니다. 같은 그룹은 모든 패널에서 같은 기호와 색을 사용합니다."),
                P("나머지 열은 설정 페이지에서 정해집니다. 먼저 모든 패널이 공유하는 X 변수가 오고, 이어서 패널 순서대로 각 Y 변수가 옵니다. 열 이름은 SiO2, Al2O3, Fe2O3T 같은 변수 키입니다."),
                P("주성분 산화물의 단위는 wt%입니다. Fe2O3T는 Fe2O3로 나타낸 총 철입니다. 미량원소 Zr, Rb, Sr, Ba, Cr, Ni의 단위는 ppm입니다."),
                P("Na2O+K2O와 Mg#는 직접 입력합니다. 프로그램이 다른 열로 계산하지 않습니다. Na2O+K2O의 단위는 wt%이고, Mg#는 무차원 마그네슘 값입니다."),
                P("X 변수는 SiO2, MgO, TiO2, K2O 중에서 고를 수 있으며 기본값은 SiO2입니다. 기본 Y 패널은 TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O, P2O5입니다."),
                P("X가 없거나 유효한 Y 숫자가 하나도 없는 행은 표시되지 않습니다. 어떤 Y만 없으면 그 패널의 점만 생략됩니다."),
                H("설명"),
                P("하커 다연도는 여러 변수를 하나의 가로축에 맞춰 나란히 그려, 마그마 분화 같은 과정에서의 성분 공변화를 비교합니다. 고전적인 선택은 가로축을 SiO2로 두는 것입니다."),
                P("패널은 가로축을 공유합니다. 작도를 시작하면 가로축 연동이 기본적으로 켜집니다."),
                P("가리키기 연동도 기본적으로 켜집니다. 한 패널의 시료점에 마우스를 올리면 다른 패널에서도 같은 시료점이 표시됩니다."),
                H("참고문헌"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["ru-RU"] =
            [
                H("Диаграммы Харкера"),
                H("Формат данных"),
                P("Первый столбец — Category, группа образцов. Одна и та же группа использует одинаковые знак и цвет на всех панелях."),
                P("Остальные столбцы задаются на странице настроек: сначала общая для всех панелей переменная X, затем каждая переменная Y в порядке панелей. Заголовки — ключи переменных, например SiO2, Al2O3 и Fe2O3T."),
                P("Главные оксиды указываются в wt%. Fe2O3T — общее железо в пересчёте на Fe2O3. Микроэлементы Zr, Rb, Sr, Ba, Cr и Ni указываются в ppm."),
                P("Na2O+K2O и Mg# вводятся непосредственно. Программа не вычисляет их по другим столбцам. Na2O+K2O указывается в wt%. Mg# — безразмерное магнезиальное число."),
                P("X может быть SiO2, MgO, TiO2 или K2O. По умолчанию это SiO2. Панели Y по умолчанию: TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O и P2O5."),
                P("Строка пропускается, если нет X или если ни одно значение Y не является числом. Если отсутствует только одно Y, точка опускается лишь на соответствующей панели."),
                H("Примечания"),
                P("Диаграммы Харкера располагают несколько переменных против одной общей горизонтальной оси, чтобы сравнить ковариацию при процессах вроде магматической дифференциации. Классический выбор оси X — SiO2."),
                P("Панели используют общую горизонтальную ось. Связывание этой оси включено при начале построения."),
                P("Связывание при наведении тоже включено. При наведении на точку на одной панели та же проба отмечается на остальных."),
                H("Литература"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["fr-FR"] =
            [
                H("Diagrammes de Harker"),
                H("Format des données"),
                P("La première colonne est Category, le groupe d’échantillons. Le même groupe utilise le même symbole et la même couleur sur chaque panneau."),
                P("Les autres colonnes viennent de la page de réglages : d’abord la variable X commune à tous les panneaux, puis chaque variable Y dans l’ordre des panneaux. Les en-têtes sont les clés des variables, par exemple SiO2, Al2O3 et Fe2O3T."),
                P("Les oxydes majeurs sont en wt%. Fe2O3T est le fer total exprimé en Fe2O3. Les éléments en traces Zr, Rb, Sr, Ba, Cr et Ni sont en ppm."),
                P("Na2O+K2O et Mg# se saisissent directement. Le programme ne les calcule pas à partir des autres colonnes. Na2O+K2O est en wt%. Mg# est le nombre de magnésium, sans unité."),
                P("X peut être SiO2, MgO, TiO2 ou K2O. La valeur par défaut est SiO2. Les panneaux Y par défaut sont TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O et P2O5."),
                P("Une ligne est ignorée si X manque, ou si aucune valeur Y n’est un nombre. Si un seul Y manque, le point est omis seulement sur ce panneau."),
                H("Remarques"),
                P("Les diagrammes de Harker placent plusieurs variables face à un même axe horizontal, afin de comparer la covariation au cours de processus tels que la différenciation magmatique. Le choix classique est SiO2 en X."),
                P("Les panneaux partagent l’axe horizontal. La liaison de cet axe est activée au début du tracé."),
                P("La liaison au survol est également activée. Survoler un point sur un panneau marque le même échantillon sur les autres."),
                H("Références"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ],
            ["pt-BR"] =
            [
                H("Diagramas de Harker"),
                H("Formato dos dados"),
                P("A primeira coluna é Category, o grupo de amostras. O mesmo grupo usa o mesmo símbolo e a mesma cor em todos os painéis."),
                P("As outras colunas vêm da página de configuração: primeiro a variável X compartilhada por todos os painéis e depois cada variável Y na ordem dos painéis. Os cabeçalhos são as chaves das variáveis, como SiO2, Al2O3 e Fe2O3T."),
                P("Os óxidos maiores estão em wt%. Fe2O3T é o ferro total expresso como Fe2O3. Os elementos-traço Zr, Rb, Sr, Ba, Cr e Ni estão em ppm."),
                P("Na2O+K2O e Mg# são digitados diretamente. O programa não os calcula a partir das outras colunas. Na2O+K2O está em wt%. Mg# é o número de magnésio, sem unidade."),
                P("X pode ser SiO2, MgO, TiO2 ou K2O. O padrão é SiO2. Os painéis Y padrão são TiO2, Al2O3, Fe2O3T, MgO, CaO, Na2O, K2O e P2O5."),
                P("Uma linha é ignorada quando X está ausente ou quando nenhum valor Y é numérico. Se apenas um Y estiver ausente, o ponto é omitido só naquele painel."),
                H("Notas"),
                P("Os diagramas de Harker colocam várias variáveis contra um mesmo eixo horizontal, para comparar a covariação em processos como a diferenciação magmática. A escolha clássica é SiO2 no eixo X."),
                P("Os painéis compartilham o eixo horizontal. A vinculação desse eixo fica ligada ao iniciar o gráfico."),
                P("A vinculação do cursor também fica ligada. Ao passar sobre um ponto em um painel, a mesma amostra é marcada nos demais."),
                H("Referências"),
                P("Harker, A. (1909). The natural history of igneous rocks. Methuen.")
            ]
        };
    }
}
