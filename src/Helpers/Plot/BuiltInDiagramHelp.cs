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

        private const string McDonoughSun1995 =
            "McDonough, W. F., & Sun, S.-s. (1995). The composition of the Earth. Chemical Geology, 120, 223–253.";

        private const string SunMcDonough1989 =
            "Sun, S.-s., & McDonough, W. F. (1989). Chemical and isotopic systematics of oceanic basalts: Implications for mantle composition and processes. Geological Society, London, Special Publications, 42, 313–345.";

        private const string Harker1909 =
            "Harker, A. (1909). The Natural History of Igneous Rocks. Methuen.";

        private static readonly Dictionary<string, Block[]> ReeDocuments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] =
            [
                H("稀土元素蛛网图"),
                H("功能概述"),
                P("稀土元素蛛网图又称稀土元素配分图，用于比较样品的稀土元素含量、轻重稀土分异程度及局部元素异常，为分析物质来源、部分熔融和结晶分异等过程提供依据。"),
                P("图中每一行样品数据对应一条配分曲线。横轴为依次排列的稀土元素，纵轴为标准化比值或原始浓度，采用对数坐标显示。"),
                H("数据格式"),
                P("第一列为 Category，填写样品名称或分组名称，该名称用于图层与图例显示。相同名称的多行数据仍分别绘制曲线，并归入同一组进行显示。第二列为 Sample，填写样品编号，仅用于标明数据来自哪个样品，不参与绘图与分组。"),
                P("其后各列为稀土元素，默认顺序为 La、Ce、Pr、Nd、Sm、Eu、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu。"),
                P("所有元素浓度统一使用 ppm，即 μg/g 或 mg/kg。输入原始浓度即可，无需提前进行标准化或取对数。"),
                P("空白单元格、无法识别为数字的内容，以及小于或等于 0 的浓度不参与绘图。未检出数据建议保留为空白，不应直接使用 0 代替实测浓度。"),
                H("标准化与坐标轴"),
                P("标准化默认开启，计算方式为：标准化值 = 样品中某元素的浓度 ÷ 参考标准中同一元素的浓度。"),
                P("标准化值没有单位。纵轴数值为 1，表示样品浓度等于参考浓度；数值为 10，表示样品浓度为参考浓度的 10 倍。"),
                P("软件提供球粒陨石、页岩、陆壳、原始地幔及玄武岩端元等参考方案。可在设置页选择方案，并查看对应参考值与文献信息。比较不同样品或不同图件时，应使用相同的参考方案。"),
                P("若某元素在所选方案中没有参考值，开启标准化后，该元素不会出现在横轴上。关闭标准化后，软件直接绘制原始浓度，纵轴单位为 ppm，仍采用对数坐标。"),
                H("结果解读"),
                P("曲线从 La 向 Lu 整体降低，通常反映轻稀土相对富集；整体升高，通常反映轻稀土相对亏损。解释时应明确采用的标准化方案。"),
                P("Eu 或 Ce 相对邻近元素形成明显峰谷时，可能存在相应的正异常或负异常。异常大小需要采用明确的计算方法定量评价，不能仅凭曲线外观判断。"),
                P("曲线相似可以提示样品具有相近的地球化学特征，但不能单独证明同源。应结合岩相学、年代学、同位素及其他元素资料进行解释。缺失元素之间的连线仅用于连接已有数据点，不代表缺失浓度已被估算。"),
                H("参考文献"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("实际使用时，还应引用所选标准化方案对应的原始文献。")
            ],
            ["zh-TW"] =
            [
                H("稀土元素蛛網圖"),
                H("功能概述"),
                P("稀土元素蛛網圖又稱稀土元素配分圖，用於比較樣品的稀土元素含量、輕重稀土分異程度及局部元素異常，為分析物質來源、部分熔融和結晶分異等過程提供依據。"),
                P("圖中每一行樣品資料對應一條配分曲線。橫軸為依次排列的稀土元素，縱軸為標準化比值或原始濃度，採用對數座標顯示。"),
                H("資料格式"),
                P("第一列為 Category，填寫樣品名稱或分組名稱，該名稱用於圖層與圖例顯示。相同名稱的多行資料仍分別繪製曲線，並歸入同一組進行顯示。第二列為 Sample，填寫樣品編號，僅用於標明資料來自哪個樣品，不參與繪圖與分組。"),
                P("其後各列為稀土元素，預設順序為 La、Ce、Pr、Nd、Sm、Eu、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu。"),
                P("所有元素濃度統一使用 ppm，即 μg/g 或 mg/kg。輸入原始濃度即可，無需提前進行標準化或取對數。"),
                P("空白儲存格、無法識別為數字的內容，以及小於或等於 0 的濃度不參與繪圖。未檢出資料建議保留為空白，不應直接使用 0 代替實測濃度。"),
                H("標準化與座標軸"),
                P("標準化預設開啟，計算方式為：標準化值 = 樣品中某元素的濃度 ÷ 參考標準中同一元素的濃度。"),
                P("標準化值沒有單位。縱軸數值為 1，表示樣品濃度等於參考濃度；數值為 10，表示樣品濃度為參考濃度的 10 倍。"),
                P("軟體提供球粒隕石、頁岩、陸殼、原始地函及玄武岩端元等參考方案。可在設定頁選擇方案，並查看對應參考值與文獻資訊。比較不同樣品或不同圖件時，應使用相同的參考方案。"),
                P("若某元素在所選方案中沒有參考值，開啟標準化後，該元素不會出現在橫軸上。關閉標準化後，軟體直接繪製原始濃度，縱軸單位為 ppm，仍採用對數座標。"),
                H("結果解讀"),
                P("曲線從 La 向 Lu 整體降低，通常反映輕稀土相對富集；整體升高，通常反映輕稀土相對虧損。解釋時應明確採用的標準化方案。"),
                P("Eu 或 Ce 相對鄰近元素形成明顯峰谷時，可能存在相應的正異常或負異常。異常大小需要採用明確的計算方法定量評價，不能僅憑曲線外觀判斷。"),
                P("曲線相似可以提示樣品具有相近的地球化學特徵，但不能單獨證明同源。應結合岩相學、年代學、同位素及其他元素資料進行解釋。缺失元素之間的連線僅用於連接已有資料點，不代表缺失濃度已被估算。"),
                H("參考文獻"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("實際使用時，還應引用所選標準化方案對應的原始文獻。")
            ],
            ["en-US"] =
            [
                H("REE Spider Diagram"),
                H("Overview"),
                P("An REE spider diagram, also called an REE pattern diagram, compares rare-earth concentrations, the degree of light-to-heavy REE fractionation, and local element anomalies. It provides evidence for source characteristics and for processes such as partial melting and fractional crystallization."),
                P("Each row of sample data is drawn as one pattern. The horizontal axis lists the rare earth elements in order. The vertical axis shows the normalized ratio or the raw concentration on a logarithmic scale."),
                H("Data Format"),
                P("The first column is Category. Enter a sample name or a group name. The name is used for layers and the legend. Rows that share a name are still drawn as separate curves and are displayed in the same group. The second column is Sample. Enter the sample ID; it only records which sample the data comes from and is not used for plotting or grouping."),
                P("The remaining columns are the rare earth elements. The default order is La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu."),
                P("Enter every element as a concentration in ppm (μg/g or mg/kg). Use the measured concentration. Do not normalize or take logarithms beforehand."),
                P("Blank cells, values that are not numbers, and concentrations less than or equal to 0 are not plotted. Leave values below detection as blank. Do not replace a measured concentration with 0."),
                H("Normalization and Axes"),
                P("Normalization is on by default. The normalized value is the concentration of an element in the sample divided by the concentration of the same element in the reference standard."),
                P("The normalized value has no unit. A value of 1 means the sample concentration equals the reference concentration. A value of 10 means the sample concentration is 10 times the reference concentration."),
                P("The software provides chondrite, shale, continental-crust, primitive-mantle, and basalt end-member standards. Choose a standard on the settings page, where its reference values and citation are shown. Use the same standard when comparing samples or diagrams."),
                P("If an element has no reference value in the selected standard, it is omitted from the horizontal axis while normalization is on. With normalization off, the raw concentration is plotted. The vertical axis is then in ppm and remains logarithmic."),
                H("Interpretation"),
                P("A pattern that falls overall from La toward Lu usually indicates relative enrichment of the light rare earths. A pattern that rises overall usually indicates relative depletion of the light rare earths. State the normalization standard used in the interpretation."),
                P("A clear peak or trough at Eu or Ce relative to the neighboring elements may indicate a positive or negative anomaly. The size of an anomaly must be evaluated with an explicit calculation. It cannot be judged from the shape of the curve alone."),
                P("Similar curves can suggest similar geochemical characteristics, but they do not by themselves prove a common source. Interpret them together with petrography, geochronology, isotopes, and other element data. A line drawn across a missing element only connects the points that are present. It does not mean the missing concentration has been estimated."),
                H("References"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("Also cite the original source of the normalization standard actually used.")
            ],
            ["de-DE"] =
            [
                H("REE-Spinnendiagramm"),
                H("Überblick"),
                P("Ein REE-Spinnendiagramm, auch Seltenerd-Verteilungsmuster genannt, vergleicht die Gehalte der Seltenerdelemente, das Ausmaß der Fraktionierung zwischen leichten und schweren Seltenerdelementen und lokale Elementanomalien. Es liefert Hinweise auf die Stoffherkunft sowie auf Prozesse wie partielle Schmelze und fraktionierte Kristallisation."),
                P("Jede Zeile der Probendaten wird als eine Verteilungskurve gezeichnet. Die horizontale Achse zeigt die Seltenerdelemente in ihrer Reihenfolge, die vertikale Achse den normierten Quotienten oder die Ausgangskonzentration, jeweils logarithmisch."),
                H("Datenformat"),
                P("Die erste Spalte ist Category. Tragen Sie einen Probennamen oder einen Gruppennamen ein. Der Name dient der Darstellung in Ebenen und in der Legende. Mehrere Zeilen mit demselben Namen werden weiterhin als getrennte Kurven gezeichnet und derselben Gruppe zugeordnet. Die zweite Spalte ist Sample. Tragen Sie die Probenkennung ein; sie dokumentiert nur, aus welcher Probe die Daten stammen, und wird weder zum Zeichnen noch zum Gruppieren verwendet."),
                P("Die übrigen Spalten sind die Seltenerdelemente. Die Standardreihenfolge ist La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu."),
                P("Alle Elementkonzentrationen werden einheitlich in ppm angegeben, also in μg/g oder mg/kg. Geben Sie die gemessene Konzentration ein. Eine vorherige Normierung oder Logarithmierung ist nicht erforderlich."),
                P("Leere Zellen, Inhalte, die nicht als Zahl gelesen werden können, und Konzentrationen kleiner oder gleich 0 werden nicht dargestellt. Werte unter der Nachweisgrenze sollten leer bleiben. Ersetzen Sie eine gemessene Konzentration nicht durch 0."),
                H("Normierung und Achsen"),
                P("Die Normierung ist standardmäßig eingeschaltet. Der normierte Wert ist die Konzentration eines Elements in der Probe geteilt durch die Konzentration desselben Elements im Referenzstandard."),
                P("Der normierte Wert hat keine Einheit. Der Wert 1 bedeutet, dass die Probenkonzentration gleich der Referenzkonzentration ist. Der Wert 10 bedeutet, dass die Probenkonzentration das Zehnfache der Referenzkonzentration beträgt."),
                P("Die Software stellt Referenzen für Chondrit, Schiefer, kontinentale Kruste, primitiven Mantel und Basalt-Endglieder bereit. Die Referenz wird auf der Einstellungsseite gewählt; dort sind die Referenzwerte und die Literatur angegeben. Beim Vergleich verschiedener Proben oder Diagramme ist dieselbe Referenz zu verwenden."),
                P("Fehlt für ein Element in der gewählten Referenz ein Wert, erscheint dieses Element bei eingeschalteter Normierung nicht auf der horizontalen Achse. Ist die Normierung ausgeschaltet, wird die Ausgangskonzentration gezeichnet. Die vertikale Achse hat dann die Einheit ppm und bleibt logarithmisch."),
                H("Interpretation"),
                P("Eine Kurve, die von La nach Lu insgesamt abfällt, zeigt gewöhnlich eine relative Anreicherung der leichten Seltenerdelemente. Ein insgesamt ansteigender Verlauf zeigt gewöhnlich eine relative Verarmung der leichten Seltenerdelemente. Bei der Interpretation ist die verwendete Normierung anzugeben."),
                P("Bilden Eu oder Ce gegenüber den benachbarten Elementen einen deutlichen Gipfel oder ein deutliches Tal, kann eine positive oder negative Anomalie vorliegen. Die Größe einer Anomalie muss mit einem festgelegten Rechenverfahren quantitativ bewertet werden und darf nicht allein nach dem Aussehen der Kurve beurteilt werden."),
                P("Ähnliche Kurven können auf ähnliche geochemische Merkmale hinweisen, beweisen aber für sich genommen keine gemeinsame Herkunft. Die Deutung sollte Petrographie, Geochronologie, Isotope und weitere Elementdaten einbeziehen. Eine Linie über ein fehlendes Element verbindet nur vorhandene Datenpunkte und bedeutet nicht, dass die fehlende Konzentration geschätzt wurde."),
                H("Literatur"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("Zusätzlich ist die Originalarbeit der tatsächlich verwendeten Normierung zu zitieren.")
            ],
            ["es-ES"] =
            [
                H("Diagrama de araña de REE"),
                H("Descripción general"),
                P("El diagrama de araña de REE, también llamado diagrama de patrones de tierras raras, compara los contenidos de tierras raras, el grado de fraccionamiento entre tierras raras ligeras y pesadas y las anomalías locales. Aporta criterios para analizar la procedencia del material y procesos como la fusión parcial y la cristalización fraccionada."),
                P("Cada fila de datos de muestra se dibuja como una curva de patrón. El eje horizontal ordena los elementos de las tierras raras y el eje vertical muestra el cociente normalizado o la concentración original, en escala logarítmica."),
                H("Formato de datos"),
                P("La primera columna es Category. Escriba el nombre de la muestra o del grupo. Ese nombre se usa en las capas y en la leyenda. Las filas con el mismo nombre se dibujan como curvas distintas y se muestran en el mismo grupo. La segunda columna es Sample. Escriba el identificador de la muestra; solo indica de qué muestra proceden los datos y no se usa para dibujar ni agrupar."),
                P("Las demás columnas son los elementos de las tierras raras. El orden predeterminado es La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu."),
                P("Todas las concentraciones se introducen en ppm, es decir, μg/g o mg/kg. Introduzca la concentración medida. No es necesario normalizar ni tomar logaritmos de antemano."),
                P("Las celdas vacías, el contenido que no se reconoce como número y las concentraciones menores o iguales que 0 no se representan. Conviene dejar en blanco los valores no detectados y no sustituir una concentración medida por 0."),
                H("Normalización y ejes"),
                P("La normalización está activada de forma predeterminada. El valor normalizado es la concentración de un elemento en la muestra dividida por la concentración del mismo elemento en el estándar de referencia."),
                P("El valor normalizado no tiene unidad. Un valor de 1 significa que la concentración de la muestra es igual a la de referencia. Un valor de 10 significa que la concentración de la muestra es 10 veces la de referencia."),
                P("El programa ofrece estándares de condrita, lutita, corteza continental, manto primitivo y miembros extremos basálticos. El estándar se elige en la página de configuración, donde se consultan los valores de referencia y la cita. Al comparar muestras o diagramas distintos debe usarse el mismo estándar."),
                P("Si un elemento no tiene valor de referencia en el estándar elegido, no aparece en el eje horizontal mientras la normalización está activada. Con la normalización desactivada se representa la concentración original. El eje vertical queda entonces en ppm y sigue siendo logarítmico."),
                H("Interpretación"),
                P("Una curva que desciende en conjunto de La hacia Lu suele reflejar un enriquecimiento relativo en tierras raras ligeras. Una curva que asciende en conjunto suele reflejar un empobrecimiento relativo en tierras raras ligeras. La interpretación debe indicar el estándar de normalización utilizado."),
                P("Cuando Eu o Ce forman un pico o un valle claro respecto de los elementos vecinos, puede existir la anomalía positiva o negativa correspondiente. La magnitud de una anomalía debe evaluarse de forma cuantitativa con un cálculo explícito y no puede juzgarse solo por el aspecto de la curva."),
                P("Curvas parecidas pueden indicar características geoquímicas próximas, pero no demuestran por sí solas un origen común. La interpretación debe combinar petrografía, geocronología, isótopos y otros datos de elementos. La línea que une elementos ausentes solo conecta los puntos existentes y no significa que la concentración ausente haya sido estimada."),
                H("Referencias"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("En el uso real debe citarse también la publicación original del estándar de normalización elegido.")
            ],
            ["ja-JP"] =
            [
                H("希土類元素スパイダー図"),
                H("機能概要"),
                P("希土類元素スパイダー図は希土類元素パターン図とも呼ばれ、試料の希土類元素濃度、軽希土類と重希土類の分別の程度、および局所的な元素異常を比較します。物質の起源、部分溶融、結晶分化などの過程を検討する根拠になります。"),
                P("試料データの 1 行が 1 本のパターン曲線になります。横軸は希土類元素を順に並べたもので、縦軸は規格化比または生の濃度を対数座標で示します。"),
                H("データ形式"),
                P("最初の列は Category です。試料名またはグループ名を入力します。この名称はレイヤーと凡例に使われます。同じ名称の複数行も、それぞれ別の曲線として描かれ、同じグループにまとめて表示されます。2 番目の列は Sample です。試料番号を入力します。データがどの試料のものかを示すだけで、描画やグループ化には使われません。"),
                P("残りの列は希土類元素です。初期順序は La、Ce、Pr、Nd、Sm、Eu、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu です。"),
                P("すべての元素濃度は ppm、すなわち μg/g または mg/kg で統一して入力します。測定濃度をそのまま入力し、あらかじめ規格化したり対数をとったりする必要はありません。"),
                P("空欄、数値として読めない内容、および 0 以下の濃度は描画されません。未検出のデータは空欄のままにし、測定濃度の代わりに 0 を入れないでください。"),
                H("規格化と座標軸"),
                P("規格化は初期状態で有効です。規格化値 = 試料中のある元素の濃度 ÷ 参照標準における同じ元素の濃度、として計算します。"),
                P("規格化値に単位はありません。縦軸の 1 は試料濃度が参照濃度と等しいことを、10 は試料濃度が参照濃度の 10 倍であることを示します。"),
                P("コンドライト、頁岩、大陸地殻、始原的マントル、玄武岩端成分などの参照方案を選べます。設定ページで方案を選び、対応する参照値と文献を確認できます。異なる試料や図を比較するときは、同じ参照方案を使ってください。"),
                P("選んだ方案に参照値がない元素は、規格化を有効にしているあいだ横軸に現れません。規格化を切ると生の濃度が描かれ、縦軸の単位は ppm のまま、座標は対数のままです。"),
                H("結果の読み方"),
                P("La から Lu に向かって全体が下がる曲線は、通常、軽希土類の相対的濃集を示します。全体が上がる曲線は、通常、軽希土類の相対的枯渇を示します。解釈では用いた規格化方案を明示してください。"),
                P("Eu または Ce が隣接元素に対して明らかな山や谷を作る場合、対応する正異常または負異常がある可能性があります。異常の大きさは、明確な計算方法で定量的に評価する必要があり、曲線の形だけでは判断できません。"),
                P("曲線が似ていることは地球化学的特徴が近いことを示唆しますが、それだけでは同源を証明できません。岩相、年代、同位体、その他の元素データと合わせて解釈してください。欠損元素の間の線は、存在する点をつなぐだけであり、欠損濃度が推定されたことを意味しません。"),
                H("参考文献"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("実際に使うときは、選択した規格化方案の原著も引用してください。")
            ],
            ["ko-KR"] =
            [
                H("희토류 원소 거미줄 그림"),
                H("기능 개요"),
                P("희토류 원소 거미줄 그림은 희토류 원소 패턴 그림이라고도 하며, 시료의 희토류 원소 함량, 경희토류와 중희토류의 분별 정도, 국부 원소 이상을 비교합니다. 물질 기원, 부분 용융, 결정 분화 같은 과정을 살피는 근거가 됩니다."),
                P("시료 데이터 한 행이 하나의 패턴 곡선이 됩니다. 가로축은 희토류 원소를 차례로 배열하고, 세로축은 표준화 비 또는 원시 농도를 로그 좌표로 나타냅니다."),
                H("데이터 형식"),
                P("첫 열은 Category입니다. 시료 이름 또는 그룹 이름을 입력합니다. 이 이름은 레이어와 범례에 사용됩니다. 같은 이름의 여러 행도 각각 별도의 곡선으로 그려지며, 같은 그룹으로 묶어 표시됩니다. 두 번째 열은 Sample입니다. 시료 번호를 입력합니다. 데이터가 어느 시료의 것인지만 나타내며 그리기와 그룹화에는 사용되지 않습니다."),
                P("그 뒤의 열은 희토류 원소입니다. 기본 순서는 La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu입니다."),
                P("모든 원소 농도는 ppm, 즉 μg/g 또는 mg/kg으로 통일해 입력합니다. 측정 농도를 그대로 입력하면 되며, 미리 표준화하거나 로그를 취할 필요는 없습니다."),
                P("빈 칸, 숫자로 읽을 수 없는 내용, 그리고 0 이하의 농도는 그려지지 않습니다. 미검출 값은 빈 칸으로 두는 것이 좋으며, 측정 농도 대신 0을 넣지 마십시오."),
                H("표준화와 좌표축"),
                P("표준화는 기본적으로 켜져 있습니다. 표준화 값 = 시료 속 어떤 원소의 농도 ÷ 참조 표준에서 같은 원소의 농도입니다."),
                P("표준화 값에는 단위가 없습니다. 세로축의 1은 시료 농도가 참조 농도와 같음을, 10은 시료 농도가 참조 농도의 10배임을 뜻합니다."),
                P("콘드라이트, 셰일, 대륙 지각, 원시 맨틀, 현무암 단성분 등의 참조 방안을 제공합니다. 설정 페이지에서 방안을 고르고 해당 참조 값과 문헌을 볼 수 있습니다. 서로 다른 시료나 그림을 비교할 때는 같은 참조 방안을 사용해야 합니다."),
                P("선택한 방안에 참조 값이 없는 원소는 표준화가 켜져 있는 동안 가로축에 나타나지 않습니다. 표준화를 끄면 원시 농도를 그대로 그리며, 세로축 단위는 ppm이고 좌표는 그대로 로그입니다."),
                H("결과 해석"),
                P("La에서 Lu로 가며 전체적으로 낮아지는 곡선은 보통 경희토류의 상대적 부화를 나타냅니다. 전체적으로 높아지면 보통 경희토류의 상대적 결핍을 나타냅니다. 해석할 때는 사용한 표준화 방안을 분명히 밝혀야 합니다."),
                P("Eu 또는 Ce가 이웃 원소에 비해 뚜렷한 봉우리나 골을 이루면 해당하는 양의 이상 또는 음의 이상이 있을 수 있습니다. 이상의 크기는 명확한 계산법으로 정량 평가해야 하며, 곡선 모양만으로 판단할 수 없습니다."),
                P("곡선이 비슷하면 지구화학적 특징이 가깝다는 실마리가 되지만, 그것만으로 동일 기원을 증명하지는 못합니다. 암석학, 연대, 동위원소 및 다른 원소 자료와 함께 해석해야 합니다. 빠진 원소 사이의 선은 있는 점을 연결할 뿐이며, 빠진 농도가 추정되었음을 뜻하지 않습니다."),
                H("참고문헌"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("실제로 사용할 때는 선택한 표준화 방안의 원문헌도 인용해야 합니다.")
            ],
            ["ru-RU"] =
            [
                H("Спайдер-диаграмма РЗЭ"),
                H("Назначение"),
                P("Спайдер-диаграмма РЗЭ, или диаграмма распределения редкоземельных элементов, служит для сравнения содержаний РЗЭ, степени фракционирования лёгких и тяжёлых РЗЭ и локальных аномалий. Она даёт основание для суждения об источнике вещества и о таких процессах, как частичное плавление и кристаллизационная дифференциация."),
                P("Каждая строка данных образца строится как одна кривая распределения. По горизонтальной оси редкоземельные элементы расположены по порядку, по вертикальной — нормированное отношение или исходная концентрация в логарифмическом масштабе."),
                H("Формат данных"),
                P("Первый столбец — Category. Введите название образца или группы. Оно используется в слоях и в легенде. Несколько строк с одним названием по-прежнему строятся отдельными кривыми и отображаются в одной группе. Второй столбец — Sample. Введите номер образца; он лишь показывает, какому образцу принадлежат данные, и не используется ни для построения, ни для группировки."),
                P("Остальные столбцы — редкоземельные элементы. Порядок по умолчанию: La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu."),
                P("Концентрации всех элементов вводятся единообразно в ppm, то есть в мкг/г или мг/кг. Вводите измеренную концентрацию. Предварительная нормировка или логарифмирование не нужны."),
                P("Пустые ячейки, содержимое, не распознанное как число, и концентрации меньше или равные 0 не участвуют в построении. Значения ниже предела обнаружения лучше оставлять пустыми и не подставлять 0 вместо измеренной концентрации."),
                H("Нормировка и оси"),
                P("Нормировка включена по умолчанию. Нормированное значение = концентрация элемента в образце ÷ концентрация того же элемента в эталоне."),
                P("Нормированное значение не имеет единицы измерения. Значение 1 на вертикальной оси означает, что концентрация в образце равна эталонной. Значение 10 означает, что концентрация в образце в 10 раз выше эталонной."),
                P("Доступны эталоны хондрита, сланца, континентальной коры, примитивной мантии и базальтовых конечных членов. Эталон выбирается на странице настроек, где показаны его значения и литературный источник. При сравнении разных образцов или диаграмм следует использовать один и тот же эталон."),
                P("Если у элемента нет значения в выбранном эталоне, при включённой нормировке он не появляется на горизонтальной оси. При выключенной нормировке строится исходная концентрация: единица вертикальной оси — ppm, масштаб остаётся логарифмическим."),
                H("Интерпретация"),
                P("Кривая, в целом понижающаяся от La к Lu, обычно отражает относительное обогащение лёгкими РЗЭ. Кривая, в целом повышающаяся, обычно отражает относительное обеднение лёгкими РЗЭ. При интерпретации нужно явно указать использованный эталон нормировки."),
                P("Если Eu или Ce образуют заметный пик или впадину относительно соседних элементов, возможна соответствующая положительная или отрицательная аномалия. Величину аномалии нужно оценивать количественно по явно заданному расчёту, а не только по виду кривой."),
                P("Сходство кривых может указывать на близкие геохимические особенности, но само по себе не доказывает общий источник. Интерпретацию следует сочетать с петрографией, геохронологией, изотопами и другими элементами. Линия через отсутствующий элемент лишь соединяет имеющиеся точки и не означает, что отсутствующая концентрация была оценена."),
                H("Литература"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("При фактическом использовании следует также ссылаться на исходную работу выбранного эталона нормировки.")
            ],
            ["fr-FR"] =
            [
                H("Diagramme en araignée des terres rares"),
                H("Présentation"),
                P("Le diagramme en araignée des terres rares, aussi appelé diagramme de répartition des terres rares, compare les teneurs en terres rares, le degré de fractionnement entre terres rares légères et lourdes et les anomalies locales. Il fournit des éléments pour examiner la provenance du matériel ainsi que des processus tels que la fusion partielle et la cristallisation fractionnée."),
                P("Chaque ligne de données d’échantillon est tracée comme une courbe de répartition. L’axe horizontal aligne les terres rares dans l’ordre, et l’axe vertical montre le rapport normalisé ou la concentration brute, en échelle logarithmique."),
                H("Format des données"),
                P("La première colonne est Category. Saisissez le nom de l’échantillon ou du groupe. Ce nom sert aux calques et à la légende. Plusieurs lignes portant le même nom sont tout de même tracées comme des courbes distinctes et affichées dans le même groupe. La deuxième colonne est Sample. Saisissez l’identifiant de l’échantillon ; il indique seulement de quel échantillon proviennent les données et n’est utilisé ni pour le tracé ni pour le regroupement."),
                P("Les autres colonnes sont les terres rares. L’ordre par défaut est La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu."),
                P("Toutes les concentrations s’expriment en ppm, c’est-à-dire en μg/g ou en mg/kg. Saisissez la concentration mesurée. Il n’est pas nécessaire de normaliser ni de prendre le logarithme au préalable."),
                P("Les cellules vides, le contenu qui n’est pas reconnu comme un nombre et les concentrations inférieures ou égales à 0 ne sont pas tracés. Les valeurs non détectées doivent rester vides. Ne remplacez pas une concentration mesurée par 0."),
                H("Normalisation et axes"),
                P("La normalisation est activée par défaut. La valeur normalisée est la concentration d’un élément dans l’échantillon divisée par la concentration du même élément dans le standard de référence."),
                P("La valeur normalisée n’a pas d’unité. La valeur 1 signifie que la concentration de l’échantillon est égale à la concentration de référence. La valeur 10 signifie qu’elle est égale à 10 fois la concentration de référence."),
                P("Le logiciel propose des standards de chondrite, de shale, de croûte continentale, de manteau primitif et de pôles basaltiques. Le standard se choisit sur la page de réglages, où figurent les valeurs de référence et la référence bibliographique. La comparaison d’échantillons ou de diagrammes différents doit utiliser le même standard."),
                P("Si un élément n’a pas de valeur de référence dans le standard choisi, il n’apparaît pas sur l’axe horizontal tant que la normalisation est activée. Lorsque la normalisation est désactivée, la concentration brute est tracée. L’axe vertical est alors en ppm et reste logarithmique."),
                H("Interprétation"),
                P("Une courbe qui diminue globalement de La vers Lu reflète en général un enrichissement relatif en terres rares légères. Une courbe qui s’élève globalement reflète en général un appauvrissement relatif en terres rares légères. L’interprétation doit préciser le standard de normalisation utilisé."),
                P("Lorsque Eu ou Ce forme un pic ou un creux net par rapport aux éléments voisins, une anomalie positive ou négative correspondante peut exister. L’ampleur d’une anomalie doit être évaluée quantitativement par un calcul explicite. Elle ne peut pas être jugée sur le seul aspect de la courbe."),
                P("Des courbes semblables peuvent indiquer des caractères géochimiques proches, mais elles ne prouvent pas à elles seules une origine commune. L’interprétation doit s’appuyer aussi sur la pétrographie, la géochronologie, les isotopes et les autres éléments. Un trait reliant des éléments manquants ne fait que joindre les points existants. Il ne signifie pas que la concentration manquante a été estimée."),
                H("Références"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("En pratique, il faut aussi citer la publication originale du standard de normalisation réellement utilisé.")
            ],
            ["pt-BR"] =
            [
                H("Diagrama aranha de ETR"),
                H("Visão geral"),
                P("O diagrama aranha de ETR, também chamado diagrama de padrões de terras raras, compara os teores de terras raras, o grau de fracionamento entre terras raras leves e pesadas e as anomalias locais. Ele oferece base para analisar a origem do material e processos como fusão parcial e cristalização fracionada."),
                P("Cada linha de dados da amostra é desenhada como uma curva de padrão. O eixo horizontal ordena os elementos terras raras, e o eixo vertical mostra a razão normalizada ou a concentração original, em escala logarítmica."),
                H("Formato dos dados"),
                P("A primeira coluna é Category. Informe o nome da amostra ou do grupo. Esse nome é usado nas camadas e na legenda. Linhas com o mesmo nome ainda são desenhadas como curvas separadas e exibidas no mesmo grupo. A segunda coluna é Sample. Informe o identificador da amostra; ele apenas indica de qual amostra os dados provêm e não é usado para desenhar nem para agrupar."),
                P("As demais colunas são os elementos terras raras. A ordem padrão é La, Ce, Pr, Nd, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu."),
                P("Todas as concentrações são informadas em ppm, isto é, μg/g ou mg/kg. Informe a concentração medida. Não é necessário normalizar nem tomar logaritmos de antemão."),
                P("Células vazias, conteúdo que não é reconhecido como número e concentrações menores ou iguais a 0 não são plotados. Valores não detectados devem permanecer em branco. Não substitua uma concentração medida por 0."),
                H("Normalização e eixos"),
                P("A normalização fica ligada por padrão. O valor normalizado é a concentração de um elemento na amostra dividida pela concentração do mesmo elemento no padrão de referência."),
                P("O valor normalizado não tem unidade. O valor 1 significa que a concentração da amostra é igual à concentração de referência. O valor 10 significa que a concentração da amostra é 10 vezes a de referência."),
                P("O programa oferece padrões de condrito, folhelho, crosta continental, manto primitivo e membros extremos basálticos. O padrão é escolhido na página de configuração, onde se consultam os valores de referência e a citação. Ao comparar amostras ou diagramas diferentes, use o mesmo padrão."),
                P("Se um elemento não tiver valor de referência no padrão escolhido, ele não aparece no eixo horizontal enquanto a normalização está ligada. Com a normalização desligada, a concentração original é plotada. O eixo vertical fica então em ppm e continua logarítmico."),
                H("Interpretação"),
                P("Uma curva que cai no conjunto de La para Lu costuma refletir enriquecimento relativo em terras raras leves. Uma curva que sobe no conjunto costuma refletir empobrecimento relativo em terras raras leves. A interpretação deve deixar claro o padrão de normalização utilizado."),
                P("Quando Eu ou Ce formam um pico ou um vale nítido em relação aos elementos vizinhos, pode haver a anomalia positiva ou negativa correspondente. O tamanho de uma anomalia precisa ser avaliado quantitativamente por um cálculo explícito e não pode ser julgado apenas pela aparência da curva."),
                P("Curvas semelhantes podem indicar características geoquímicas próximas, mas não provam sozinhas uma origem comum. A interpretação deve combinar petrografia, geocronologia, isótopos e outros dados de elementos. A linha entre elementos ausentes apenas liga os pontos existentes e não significa que a concentração ausente tenha sido estimada."),
                H("Referências"),
                P(McDonoughSun1995),
                P(SunMcDonough1989),
                P("No uso real, cite também a publicação original do padrão de normalização escolhido.")
            ]
        };

        private static readonly Dictionary<string, Block[]> TraceDocuments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] =
            [
                H("微量元素蛛网图"),
                H("功能概述"),
                P("微量元素蛛网图将多种元素按指定顺序排列，用于比较样品相对于参考组成的富集、亏损及局部异常。常用于研究岩浆源区特征、部分熔融、结晶分异、地壳混染和流体作用等过程。"),
                P("图中每一行样品数据对应一条配分曲线。横轴为元素序列，纵轴为标准化比值或原始浓度，采用对数坐标显示。"),
                H("数据格式"),
                P("第一列为 Category，填写样品名称或分组名称，用于图层与图例显示。相同名称的多行数据分别绘制曲线，并归入同一组进行显示。第二列为 Sample，填写样品编号，仅用于标明数据来自哪个样品，不参与绘图与分组。"),
                P("默认元素顺序为 Cs、Rb、Ba、Th、U、Nb、Ta、K、La、Ce、Pb、Pr、Sr、Nd、Zr、Hf、Sm、Eu、Ti、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu、Y。"),
                P("所有元素均以元素浓度输入，单位统一为 ppm。其中 K、Ti 同样使用 ppm，不能直接填入 K₂O、TiO₂ 的 wt% 数值。"),
                P("若原始数据仅提供氧化物含量，可先进行换算：K（ppm）≈ K₂O（wt%）× 8301；Ti（ppm）≈ TiO₂（wt%）× 5993。上述公式中，wt% 按百分数数值输入，例如 1 wt% 输入为 1。"),
                P("空白单元格、无法识别为数字的内容，以及小于或等于 0 的浓度不参与绘图。输入原始浓度即可，无需提前标准化或取对数。"),
                H("标准化与坐标轴"),
                P("标准化默认开启，推荐参考方案为 Sun & McDonough（1989）原始地幔。软件还提供球粒陨石、陆壳、其他原始地幔及玄武岩端元等方案。"),
                P("计算方式为：标准化值 = 样品中某元素的浓度 ÷ 参考标准中同一元素的浓度。"),
                P("标准化值没有单位。大于 1 表示相对于所选参考组成富集，小于 1 表示相对于所选参考组成亏损。"),
                P("若某元素在当前方案中没有参考值，开启标准化后，该元素不会出现在横轴上。关闭标准化后，软件直接绘制原始浓度，纵轴单位为 ppm，仍采用对数坐标。"),
                P("不同参考方案和元素排列会影响曲线外观。比较图件时，应保持参考方案与元素顺序一致。"),
                H("结果解读"),
                P("解读时可关注整体配分型式，以及 Nb–Ta、Pb、Sr、Eu、Ti 等位置的局部峰谷。这些特征可以为源区组成、矿物分离结晶、混染或流体作用提供线索。"),
                P("局部峰谷表示元素相对于当前参考组成及邻近元素的差异，不直接等同于某一种地质过程。例如，Nb–Ta 负异常不能单独作为构造环境判据，Sr、Eu 和 Ti 异常也需要结合矿物组成及其他证据解释。"),
                P("Rb、Ba、K、Sr 等元素可能受蚀变或流体活动影响。解释曲线前，应检查样品新鲜程度、分析质量和检出限。缺失元素之间的连线不代表该元素浓度已被估算。"),
                H("参考文献"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("实际使用时，还应引用所选标准化方案对应的原始文献。")
            ],
            ["zh-TW"] =
            [
                H("微量元素蛛網圖"),
                H("功能概述"),
                P("微量元素蛛網圖將多種元素按指定順序排列，用於比較樣品相對於參考組成的富集、虧損及局部異常。常用於研究岩漿源區特徵、部分熔融、結晶分異、地殼混染和流體作用等過程。"),
                P("圖中每一行樣品資料對應一條配分曲線。橫軸為元素序列，縱軸為標準化比值或原始濃度，採用對數座標顯示。"),
                H("資料格式"),
                P("第一列為 Category，填寫樣品名稱或分組名稱，用於圖層與圖例顯示。相同名稱的多行資料分別繪製曲線，並歸入同一組進行顯示。第二列為 Sample，填寫樣品編號，僅用於標明資料來自哪個樣品，不參與繪圖與分組。"),
                P("預設元素順序為 Cs、Rb、Ba、Th、U、Nb、Ta、K、La、Ce、Pb、Pr、Sr、Nd、Zr、Hf、Sm、Eu、Ti、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu、Y。"),
                P("所有元素均以元素濃度輸入，單位統一為 ppm。其中 K、Ti 同樣使用 ppm，不能直接填入 K₂O、TiO₂ 的 wt% 數值。"),
                P("若原始資料僅提供氧化物含量，可先進行換算：K（ppm）≈ K₂O（wt%）× 8301；Ti（ppm）≈ TiO₂（wt%）× 5993。上述公式中，wt% 按百分數數值輸入，例如 1 wt% 輸入為 1。"),
                P("空白儲存格、無法識別為數字的內容，以及小於或等於 0 的濃度不參與繪圖。輸入原始濃度即可，無需提前標準化或取對數。"),
                H("標準化與座標軸"),
                P("標準化預設開啟，建議參考方案為 Sun & McDonough（1989）原始地函。軟體還提供球粒隕石、陸殼、其他原始地函及玄武岩端元等方案。"),
                P("計算方式為：標準化值 = 樣品中某元素的濃度 ÷ 參考標準中同一元素的濃度。"),
                P("標準化值沒有單位。大於 1 表示相對於所選參考組成富集，小於 1 表示相對於所選參考組成虧損。"),
                P("若某元素在目前方案中沒有參考值，開啟標準化後，該元素不會出現在橫軸上。關閉標準化後，軟體直接繪製原始濃度，縱軸單位為 ppm，仍採用對數座標。"),
                P("不同參考方案和元素排列會影響曲線外觀。比較圖件時，應保持參考方案與元素順序一致。"),
                H("結果解讀"),
                P("解讀時可關注整體配分型式，以及 Nb–Ta、Pb、Sr、Eu、Ti 等位置的局部峰谷。這些特徵可以為源區組成、礦物分離結晶、混染或流體作用提供線索。"),
                P("局部峰谷表示元素相對於目前參考組成及鄰近元素的差異，不直接等同於某一種地質過程。例如，Nb–Ta 負異常不能單獨作為構造環境判據，Sr、Eu 和 Ti 異常也需要結合礦物組成及其他證據解釋。"),
                P("Rb、Ba、K、Sr 等元素可能受蝕變或流體活動影響。解釋曲線前，應檢查樣品新鮮程度、分析品質和檢出限。缺失元素之間的連線不代表該元素濃度已被估算。"),
                H("參考文獻"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("實際使用時，還應引用所選標準化方案對應的原始文獻。")
            ],
            ["en-US"] =
            [
                H("Trace-Element Spider Diagram"),
                H("Overview"),
                P("A trace-element spider diagram arranges elements in a specified order to compare enrichment, depletion, and local anomalies relative to a reference composition. It is commonly used to examine source characteristics and processes such as partial melting, fractional crystallization, crustal contamination, and fluid interaction."),
                P("Each row of sample data is drawn as one pattern. The horizontal axis is the element sequence. The vertical axis shows the normalized ratio or the raw concentration on a logarithmic scale."),
                H("Data Format"),
                P("The first column is Category. Enter a sample name or a group name for layers and the legend. Rows that share a name are drawn as separate curves and displayed in the same group. The second column is Sample. Enter the sample ID; it only records which sample the data comes from and is not used for plotting or grouping."),
                P("The default element order is Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y."),
                P("Enter every value as an elemental concentration in ppm. K and Ti are also entered in ppm. Do not enter K₂O or TiO₂ values in wt% in those columns."),
                P("If the source data give only oxide contents, convert them first: K (ppm) ≈ K₂O (wt%) × 8301; Ti (ppm) ≈ TiO₂ (wt%) × 5993. In these formulas, wt% is the percentage number, so 1 wt% is entered as 1."),
                P("Blank cells, values that are not numbers, and concentrations less than or equal to 0 are not plotted. Enter the measured concentration. Do not normalize or take logarithms beforehand."),
                H("Normalization and Axes"),
                P("Normalization is on by default. The recommended standard is primitive mantle from Sun & McDonough (1989). Chondrite, continental-crust, other primitive-mantle, and basalt end-member standards are also available."),
                P("The normalized value is the concentration of an element in the sample divided by the concentration of the same element in the reference standard."),
                P("The normalized value has no unit. A value greater than 1 means enrichment relative to the selected reference composition. A value less than 1 means depletion relative to that composition."),
                P("If an element has no reference value in the current standard, it is omitted from the horizontal axis while normalization is on. With normalization off, the raw concentration is plotted. The vertical axis is then in ppm and remains logarithmic."),
                P("The reference standard and the element order both change the appearance of the curves. Keep both the same when comparing diagrams."),
                H("Interpretation"),
                P("Interpretation can consider the overall pattern and local peaks or troughs at Nb–Ta, Pb, Sr, Eu, Ti, and similar positions. These features can provide clues to source composition, fractional crystallization of minerals, contamination, or fluid interaction."),
                P("A local peak or trough shows how an element differs from the current reference composition and from its neighbors. It does not by itself identify a particular geological process. A negative Nb–Ta anomaly, for example, is not a tectonic-setting criterion on its own. Sr, Eu, and Ti anomalies also need to be interpreted with mineralogy and other evidence."),
                P("Rb, Ba, K, Sr, and similar elements can be affected by alteration or fluid activity. Before interpreting a pattern, check sample freshness, analytical quality, and detection limits. A line drawn across a missing element does not mean that concentration has been estimated."),
                H("References"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("Also cite the original source of the normalization standard actually used.")
            ],
            ["de-DE"] =
            [
                H("Spurenelement-Spinnendiagramm"),
                H("Überblick"),
                P("Ein Spurenelement-Spinnendiagramm ordnet mehrere Elemente in einer festgelegten Reihenfolge und vergleicht Anreicherung, Verarmung und lokale Anomalien gegenüber einer Referenzzusammensetzung. Es wird häufig verwendet, um Quellmerkmale sowie Prozesse wie partielle Schmelze, fraktionierte Kristallisation, krustale Kontamination und Fluidwirkung zu untersuchen."),
                P("Jede Zeile der Probendaten wird als eine Verteilungskurve gezeichnet. Die horizontale Achse ist die Elementfolge, die vertikale Achse der normierte Quotient oder die Ausgangskonzentration, jeweils logarithmisch."),
                H("Datenformat"),
                P("Die erste Spalte ist Category. Tragen Sie einen Probennamen oder einen Gruppennamen für Ebenen und Legende ein. Mehrere Zeilen mit demselben Namen werden als getrennte Kurven gezeichnet und derselben Gruppe zugeordnet. Die zweite Spalte ist Sample. Tragen Sie die Probenkennung ein; sie dokumentiert nur, aus welcher Probe die Daten stammen, und wird weder zum Zeichnen noch zum Gruppieren verwendet."),
                P("Die Standardreihenfolge der Elemente ist Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y."),
                P("Alle Werte werden als Elementkonzentrationen in ppm eingegeben. K und Ti werden ebenfalls in ppm angegeben. In diese Spalten dürfen keine K₂O- oder TiO₂-Werte in wt% eingetragen werden."),
                P("Liegen die Ausgangsdaten nur als Oxidgehalte vor, werden sie zuerst umgerechnet: K (ppm) ≈ K₂O (wt%) × 8301; Ti (ppm) ≈ TiO₂ (wt%) × 5993. In diesen Formeln ist wt% der Prozentwert, sodass 1 wt% als 1 eingegeben wird."),
                P("Leere Zellen, Inhalte, die nicht als Zahl gelesen werden können, und Konzentrationen kleiner oder gleich 0 werden nicht dargestellt. Geben Sie die gemessene Konzentration ein. Eine vorherige Normierung oder Logarithmierung ist nicht erforderlich."),
                H("Normierung und Achsen"),
                P("Die Normierung ist standardmäßig eingeschaltet. Empfohlen wird der primitive Mantel nach Sun & McDonough (1989). Weitere Referenzen sind Chondrit, kontinentale Kruste, andere primitive Mantel und Basalt-Endglieder."),
                P("Der normierte Wert ist die Konzentration eines Elements in der Probe geteilt durch die Konzentration desselben Elements im Referenzstandard."),
                P("Der normierte Wert hat keine Einheit. Ein Wert größer als 1 bedeutet Anreicherung gegenüber der gewählten Referenzzusammensetzung, ein Wert kleiner als 1 Verarmung gegenüber dieser Zusammensetzung."),
                P("Fehlt für ein Element in der aktuellen Referenz ein Wert, erscheint dieses Element bei eingeschalteter Normierung nicht auf der horizontalen Achse. Ist die Normierung ausgeschaltet, wird die Ausgangskonzentration gezeichnet. Die vertikale Achse hat dann die Einheit ppm und bleibt logarithmisch."),
                P("Referenz und Elementreihenfolge verändern beide das Aussehen der Kurven. Beim Vergleich von Diagrammen sind Referenz und Reihenfolge beizubehalten."),
                H("Interpretation"),
                P("Bei der Deutung können das Gesamtverteilungsmuster und lokale Gipfel oder Täler bei Nb–Ta, Pb, Sr, Eu, Ti und ähnlichen Positionen betrachtet werden. Diese Merkmale können Hinweise auf die Quellzusammensetzung, fraktionierte Kristallisation von Mineralen, Kontamination oder Fluidwirkung geben."),
                P("Ein lokaler Gipfel oder ein lokales Tal zeigt den Unterschied eines Elements zur aktuellen Referenzzusammensetzung und zu den Nachbarelementen. Er ist nicht unmittelbar mit einem bestimmten geologischen Prozess gleichzusetzen. Eine negative Nb–Ta-Anomalie ist zum Beispiel für sich genommen kein Kriterium für das tektonische Milieu. Auch Sr-, Eu- und Ti-Anomalien sind zusammen mit dem Mineralbestand und weiteren Belegen zu deuten."),
                P("Rb, Ba, K, Sr und ähnliche Elemente können durch Alteration oder Fluidaktivität beeinflusst sein. Vor der Deutung einer Kurve sollten Frische der Probe, Analysenqualität und Nachweisgrenzen geprüft werden. Eine Linie über ein fehlendes Element bedeutet nicht, dass dessen Konzentration geschätzt wurde."),
                H("Literatur"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("Zusätzlich ist die Originalarbeit der tatsächlich verwendeten Normierung zu zitieren.")
            ],
            ["es-ES"] =
            [
                H("Diagrama de araña de elementos traza"),
                H("Descripción general"),
                P("El diagrama de araña de elementos traza ordena varios elementos en una secuencia determinada para comparar el enriquecimiento, el empobrecimiento y las anomalías locales respecto de una composición de referencia. Se usa con frecuencia para estudiar las características de la fuente magmática y procesos como la fusión parcial, la cristalización fraccionada, la contaminación cortical y la acción de fluidos."),
                P("Cada fila de datos de muestra se dibuja como una curva de patrón. El eje horizontal es la secuencia de elementos y el eje vertical muestra el cociente normalizado o la concentración original, en escala logarítmica."),
                H("Formato de datos"),
                P("La primera columna es Category. Escriba el nombre de la muestra o del grupo, que se usa en las capas y en la leyenda. Las filas con el mismo nombre se dibujan como curvas distintas y se muestran en el mismo grupo. La segunda columna es Sample. Escriba el identificador de la muestra; solo indica de qué muestra proceden los datos y no se usa para dibujar ni agrupar."),
                P("El orden predeterminado de los elementos es Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y."),
                P("Todos los valores se introducen como concentración elemental en ppm. K y Ti también se introducen en ppm. No se pueden escribir directamente en esas columnas los valores de K₂O o TiO₂ en wt%."),
                P("Si los datos originales solo aportan contenidos de óxidos, conviértalos antes: K (ppm) ≈ K₂O (wt%) × 8301; Ti (ppm) ≈ TiO₂ (wt%) × 5993. En estas fórmulas, wt% es el valor del porcentaje, de modo que 1 wt% se introduce como 1."),
                P("Las celdas vacías, el contenido que no se reconoce como número y las concentraciones menores o iguales que 0 no se representan. Introduzca la concentración medida. No es necesario normalizar ni tomar logaritmos de antemano."),
                H("Normalización y ejes"),
                P("La normalización está activada de forma predeterminada. El estándar recomendado es el manto primitivo de Sun y McDonough (1989). También hay estándares de condrita, corteza continental, otros mantos primitivos y miembros extremos basálticos."),
                P("El valor normalizado es la concentración de un elemento en la muestra dividida por la concentración del mismo elemento en el estándar de referencia."),
                P("El valor normalizado no tiene unidad. Un valor mayor que 1 indica enriquecimiento respecto de la composición de referencia elegida, y un valor menor que 1 indica empobrecimiento respecto de esa composición."),
                P("Si un elemento no tiene valor de referencia en el estándar actual, no aparece en el eje horizontal mientras la normalización está activada. Con la normalización desactivada se representa la concentración original. El eje vertical queda entonces en ppm y sigue siendo logarítmico."),
                P("El estándar de referencia y el orden de los elementos modifican el aspecto de las curvas. Al comparar diagramas deben mantenerse iguales el estándar y el orden."),
                H("Interpretación"),
                P("La interpretación puede atender al patrón general y a los picos o valles locales en Nb–Ta, Pb, Sr, Eu, Ti y posiciones semejantes. Estos rasgos pueden aportar indicios sobre la composición de la fuente, la cristalización fraccionada de minerales, la contaminación o la acción de fluidos."),
                P("Un pico o un valle local expresa la diferencia de un elemento respecto de la composición de referencia actual y de los elementos vecinos. No equivale por sí mismo a un proceso geológico concreto. Por ejemplo, una anomalía negativa de Nb–Ta no puede usarse sola como criterio de ambiente tectónico. Las anomalías de Sr, Eu y Ti también deben interpretarse junto con la mineralogía y otras evidencias."),
                P("Elementos como Rb, Ba, K y Sr pueden verse afectados por la alteración o por la actividad de fluidos. Antes de interpretar una curva conviene comprobar la frescura de la muestra, la calidad analítica y los límites de detección. La línea entre elementos ausentes no significa que esa concentración haya sido estimada."),
                H("Referencias"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("En el uso real debe citarse también la publicación original del estándar de normalización elegido.")
            ],
            ["ja-JP"] =
            [
                H("微量元素スパイダー図"),
                H("機能概要"),
                P("微量元素スパイダー図は、複数の元素を指定した順序に並べ、参照組成に対する濃集、枯渇、局所異常を比較します。マグマ源の特徴、部分溶融、結晶分化、地殻混染、流体作用などの検討によく使われます。"),
                P("試料データの 1 行が 1 本のパターン曲線になります。横軸は元素の並び、縦軸は規格化比または生の濃度を対数座標で示します。"),
                H("データ形式"),
                P("最初の列は Category です。試料名またはグループ名を入力し、レイヤーと凡例に使います。同じ名称の複数行は、それぞれ別の曲線として描かれ、同じグループにまとめて表示されます。2 番目の列は Sample です。試料番号を入力します。データがどの試料のものかを示すだけで、描画やグループ化には使われません。"),
                P("初期の元素順は Cs、Rb、Ba、Th、U、Nb、Ta、K、La、Ce、Pb、Pr、Sr、Nd、Zr、Hf、Sm、Eu、Ti、Gd、Tb、Dy、Ho、Er、Tm、Yb、Lu、Y です。"),
                P("すべての値は元素濃度として入力し、単位は ppm に統一します。K と Ti も ppm で入力し、K₂O や TiO₂ の wt% をそのまま入れることはできません。"),
                P("元データが酸化物含有量だけの場合は、先に換算します。K（ppm）≈ K₂O（wt%）× 8301、Ti（ppm）≈ TiO₂（wt%）× 5993 です。式中の wt% は百分率の数値で、たとえば 1 wt% は 1 と入力します。"),
                P("空欄、数値として読めない内容、および 0 以下の濃度は描画されません。測定濃度をそのまま入力し、あらかじめ規格化したり対数をとったりする必要はありません。"),
                H("規格化と座標軸"),
                P("規格化は初期状態で有効です。推奨する参照方案は Sun & McDonough（1989）の始原的マントルです。コンドライト、大陸地殻、その他の始原的マントル、玄武岩端成分も選べます。"),
                P("規格化値 = 試料中のある元素の濃度 ÷ 参照標準における同じ元素の濃度、として計算します。"),
                P("規格化値に単位はありません。1 より大きい値は選んだ参照組成に対する濃集、1 より小さい値はそれに対する枯渇を示します。"),
                P("現在の方案に参照値がない元素は、規格化を有効にしているあいだ横軸に現れません。規格化を切ると生の濃度が描かれ、縦軸の単位は ppm のまま、座標は対数のままです。"),
                P("参照方案と元素の並びは、どちらも曲線の形に影響します。図を比較するときは、参照方案と元素順を同じにしてください。"),
                H("結果の読み方"),
                P("解釈では、全体のパターンと、Nb–Ta、Pb、Sr、Eu、Ti などの位置にある局所的な山や谷に注目できます。これらは、源の組成、鉱物の分別結晶、混染、流体作用を考える手がかりになります。"),
                P("局所的な山や谷は、現在の参照組成および隣接元素に対する違いを示すもので、特定の地質過程そのものではありません。たとえば Nb–Ta の負異常だけをテクトニック環境の判定基準にはできません。Sr、Eu、Ti の異常も、鉱物組成や他の証拠と合わせて解釈する必要があります。"),
                P("Rb、Ba、K、Sr などは変質や流体の影響を受けることがあります。曲線を解釈する前に、試料の新鮮さ、分析品質、検出限界を確認してください。欠損元素の間の線は、その濃度が推定されたことを意味しません。"),
                H("参考文献"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("実際に使うときは、選択した規格化方案の原著も引用してください。")
            ],
            ["ko-KR"] =
            [
                H("미량원소 거미줄 그림"),
                H("기능 개요"),
                P("미량원소 거미줄 그림은 여러 원소를 지정한 순서로 배열해, 참조 조성에 대한 부화, 결핍, 국부 이상을 비교합니다. 마그마 근원 특징, 부분 용융, 결정 분화, 지각 혼염, 유체 작용 등을 연구할 때 자주 쓰입니다."),
                P("시료 데이터 한 행이 하나의 패턴 곡선이 됩니다. 가로축은 원소 서열이고, 세로축은 표준화 비 또는 원시 농도를 로그 좌표로 나타냅니다."),
                H("데이터 형식"),
                P("첫 열은 Category입니다. 시료 이름 또는 그룹 이름을 입력하며, 레이어와 범례에 사용됩니다. 같은 이름의 여러 행은 각각 별도의 곡선으로 그려지고 같은 그룹으로 묶어 표시됩니다. 두 번째 열은 Sample입니다. 시료 번호를 입력합니다. 데이터가 어느 시료의 것인지만 나타내며 그리기와 그룹화에는 사용되지 않습니다."),
                P("기본 원소 순서는 Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y입니다."),
                P("모든 값은 원소 농도로 입력하며 단위는 ppm으로 통일합니다. K와 Ti도 ppm으로 입력합니다. K₂O, TiO₂의 wt% 수치를 그 열에 그대로 넣을 수 없습니다."),
                P("원자료가 산화물 함량만 제공하면 먼저 환산합니다. K(ppm) ≈ K₂O(wt%) × 8301, Ti(ppm) ≈ TiO₂(wt%) × 5993입니다. 식에서 wt%는 백분율 수치이므로, 예를 들어 1 wt%는 1로 입력합니다."),
                P("빈 칸, 숫자로 읽을 수 없는 내용, 그리고 0 이하의 농도는 그려지지 않습니다. 측정 농도를 그대로 입력하면 되며, 미리 표준화하거나 로그를 취할 필요는 없습니다."),
                H("표준화와 좌표축"),
                P("표준화는 기본적으로 켜져 있습니다. 권장 참조 방안은 Sun & McDonough(1989)의 원시 맨틀입니다. 콘드라이트, 대륙 지각, 다른 원시 맨틀, 현무암 단성분 방안도 있습니다."),
                P("표준화 값 = 시료 속 어떤 원소의 농도 ÷ 참조 표준에서 같은 원소의 농도입니다."),
                P("표준화 값에는 단위가 없습니다. 1보다 크면 선택한 참조 조성에 대해 부화, 1보다 작으면 그 조성에 대해 결핍을 뜻합니다."),
                P("현재 방안에 참조 값이 없는 원소는 표준화가 켜져 있는 동안 가로축에 나타나지 않습니다. 표준화를 끄면 원시 농도를 그대로 그리며, 세로축 단위는 ppm이고 좌표는 그대로 로그입니다."),
                P("참조 방안과 원소 배열은 모두 곡선 모양에 영향을 줍니다. 그림을 비교할 때는 참조 방안과 원소 순서를 같게 유지해야 합니다."),
                H("결과 해석"),
                P("해석할 때는 전체 패턴과 Nb–Ta, Pb, Sr, Eu, Ti 등 위치의 국부 봉우리와 골을 볼 수 있습니다. 이런 특징은 근원 조성, 광물의 분별 정출, 혼염 또는 유체 작용의 실마리가 됩니다."),
                P("국부 봉우리와 골은 현재 참조 조성과 이웃 원소에 대한 차이를 나타낼 뿐, 특정 지질 과정 그 자체와 같지는 않습니다. 예를 들어 Nb–Ta 음의 이상만으로 지구조 환경을 판단할 수 없습니다. Sr, Eu, Ti 이상도 광물 조성과 다른 증거와 함께 해석해야 합니다."),
                P("Rb, Ba, K, Sr 등은 변질이나 유체 활동의 영향을 받을 수 있습니다. 곡선을 해석하기 전에 시료의 신선도, 분석 품질, 검출 한계를 확인해야 합니다. 빠진 원소 사이의 선은 그 농도가 추정되었음을 뜻하지 않습니다."),
                H("참고문헌"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("실제로 사용할 때는 선택한 표준화 방안의 원문헌도 인용해야 합니다.")
            ],
            ["ru-RU"] =
            [
                H("Спайдер-диаграмма микроэлементов"),
                H("Назначение"),
                P("Спайдер-диаграмма микроэлементов располагает элементы в заданном порядке и служит для сравнения обогащения, обеднения и локальных аномалий относительно эталонного состава. Её часто применяют при изучении особенностей магматического источника, частичного плавления, кристаллизационной дифференциации, коровой контаминации и воздействия флюидов."),
                P("Каждая строка данных образца строится как одна кривая распределения. По горизонтальной оси — последовательность элементов, по вертикальной — нормированное отношение или исходная концентрация в логарифмическом масштабе."),
                H("Формат данных"),
                P("Первый столбец — Category. Введите название образца или группы для слоёв и легенды. Несколько строк с одним названием строятся отдельными кривыми и отображаются в одной группе. Второй столбец — Sample. Введите номер образца; он лишь показывает, какому образцу принадлежат данные, и не используется ни для построения, ни для группировки."),
                P("Порядок элементов по умолчанию: Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y."),
                P("Все значения вводятся как концентрации элементов в ppm. K и Ti также указываются в ppm. В эти столбцы нельзя напрямую вносить K₂O или TiO₂ в wt%."),
                P("Если исходные данные содержат только содержания оксидов, их сначала пересчитывают: K (ppm) ≈ K₂O (wt%) × 8301; Ti (ppm) ≈ TiO₂ (wt%) × 5993. В этих формулах wt% — число процентов, поэтому 1 wt% вводится как 1."),
                P("Пустые ячейки, содержимое, не распознанное как число, и концентрации меньше или равные 0 не участвуют в построении. Вводите измеренную концентрацию. Предварительная нормировка или логарифмирование не нужны."),
                H("Нормировка и оси"),
                P("Нормировка включена по умолчанию. Рекомендуемый эталон — примитивная мантия Sun & McDonough (1989). Также доступны хондрит, континентальная кора, другие составы примитивной мантии и базальтовые конечные члены."),
                P("Нормированное значение = концентрация элемента в образце ÷ концентрация того же элемента в эталоне."),
                P("Нормированное значение не имеет единицы измерения. Значение больше 1 означает обогащение относительно выбранного эталонного состава, значение меньше 1 — обеднение относительно этого состава."),
                P("Если у элемента нет значения в текущем эталоне, при включённой нормировке он не появляется на горизонтальной оси. При выключенной нормировке строится исходная концентрация: единица вертикальной оси — ppm, масштаб остаётся логарифмическим."),
                P("Эталон и порядок элементов влияют на вид кривых. При сравнении диаграмм эталон и порядок следует сохранять одинаковыми."),
                H("Интерпретация"),
                P("При интерпретации можно рассматривать общий спектр и локальные пики или впадины в положениях Nb–Ta, Pb, Sr, Eu, Ti и подобных. Эти особенности могут указывать на состав источника, фракционную кристаллизацию минералов, контаминацию или воздействие флюидов."),
                P("Локальный пик или впадина показывают отличие элемента от текущего эталонного состава и от соседних элементов. Сами по себе они не равнозначны определённому геологическому процессу. Например, отрицательная аномалия Nb–Ta не может служить самостоятельным критерием тектонической обстановки. Аномалии Sr, Eu и Ti также нужно объяснять вместе с минеральным составом и другими данными."),
                P("Rb, Ba, K, Sr и подобные элементы могут быть изменены вторичными изменениями или флюидной активностью. Перед интерпретацией кривой следует проверить свежесть образца, качество анализа и пределы обнаружения. Линия через отсутствующий элемент не означает, что его концентрация была оценена."),
                H("Литература"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("При фактическом использовании следует также ссылаться на исходную работу выбранного эталона нормировки.")
            ],
            ["fr-FR"] =
            [
                H("Diagramme en araignée des éléments en traces"),
                H("Présentation"),
                P("Le diagramme en araignée des éléments en traces aligne plusieurs éléments dans un ordre donné afin de comparer l’enrichissement, l’appauvrissement et les anomalies locales par rapport à une composition de référence. Il sert souvent à étudier les caractères de la source magmatique ainsi que la fusion partielle, la cristallisation fractionnée, la contamination crustale et l’action des fluides."),
                P("Chaque ligne de données d’échantillon est tracée comme une courbe de répartition. L’axe horizontal est la séquence d’éléments, et l’axe vertical montre le rapport normalisé ou la concentration brute, en échelle logarithmique."),
                H("Format des données"),
                P("La première colonne est Category. Saisissez le nom de l’échantillon ou du groupe, utilisé pour les calques et la légende. Plusieurs lignes portant le même nom sont tracées comme des courbes distinctes et affichées dans le même groupe. La deuxième colonne est Sample. Saisissez l’identifiant de l’échantillon ; il indique seulement de quel échantillon proviennent les données et n’est utilisé ni pour le tracé ni pour le regroupement."),
                P("L’ordre par défaut des éléments est Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y."),
                P("Toutes les valeurs sont saisies comme des concentrations élémentaires en ppm. K et Ti sont aussi saisis en ppm. Les teneurs en K₂O ou TiO₂ exprimées en wt% ne peuvent pas être entrées directement dans ces colonnes."),
                P("Si les données d’origine ne donnent que des teneurs en oxydes, convertissez-les d’abord : K (ppm) ≈ K₂O (wt%) × 8301 ; Ti (ppm) ≈ TiO₂ (wt%) × 5993. Dans ces formules, wt% est la valeur du pourcentage : 1 wt% se saisit comme 1."),
                P("Les cellules vides, le contenu qui n’est pas reconnu comme un nombre et les concentrations inférieures ou égales à 0 ne sont pas tracés. Saisissez la concentration mesurée. Il n’est pas nécessaire de normaliser ni de prendre le logarithme au préalable."),
                H("Normalisation et axes"),
                P("La normalisation est activée par défaut. Le standard recommandé est le manteau primitif de Sun & McDonough (1989). Des standards de chondrite, de croûte continentale, d’autres manteaux primitifs et de pôles basaltiques sont aussi disponibles."),
                P("La valeur normalisée est la concentration d’un élément dans l’échantillon divisée par la concentration du même élément dans le standard de référence."),
                P("La valeur normalisée n’a pas d’unité. Une valeur supérieure à 1 indique un enrichissement par rapport à la composition de référence choisie, et une valeur inférieure à 1 un appauvrissement par rapport à cette composition."),
                P("Si un élément n’a pas de valeur de référence dans le standard actuel, il n’apparaît pas sur l’axe horizontal tant que la normalisation est activée. Lorsque la normalisation est désactivée, la concentration brute est tracée. L’axe vertical est alors en ppm et reste logarithmique."),
                P("Le standard de référence et l’ordre des éléments modifient tous deux l’aspect des courbes. Pour comparer des diagrammes, conservez le même standard et le même ordre."),
                H("Interprétation"),
                P("L’interprétation peut porter sur le spectre d’ensemble et sur les pics ou creux locaux aux positions Nb–Ta, Pb, Sr, Eu, Ti et analogues. Ces caractères peuvent fournir des indices sur la composition de la source, la cristallisation fractionnée des minéraux, la contamination ou l’action des fluides."),
                P("Un pic ou un creux local exprime l’écart d’un élément par rapport à la composition de référence actuelle et aux éléments voisins. Il n’équivaut pas directement à un processus géologique particulier. Une anomalie négative Nb–Ta, par exemple, ne constitue pas à elle seule un critère d’environnement tectonique. Les anomalies de Sr, Eu et Ti doivent aussi être interprétées avec la minéralogie et d’autres données."),
                P("Rb, Ba, K, Sr et des éléments semblables peuvent être affectés par l’altération ou par l’activité des fluides. Avant d’interpréter une courbe, vérifiez la fraîcheur de l’échantillon, la qualité analytique et les limites de détection. Un trait reliant des éléments manquants ne signifie pas que cette concentration a été estimée."),
                H("Références"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("En pratique, il faut aussi citer la publication originale du standard de normalisation réellement utilisé.")
            ],
            ["pt-BR"] =
            [
                H("Diagrama aranha de elementos-traço"),
                H("Visão geral"),
                P("O diagrama aranha de elementos-traço ordena vários elementos em uma sequência definida para comparar enriquecimento, empobrecimento e anomalias locais em relação a uma composição de referência. É usado com frequência no estudo das características da fonte magmática e de processos como fusão parcial, cristalização fracionada, contaminação crustal e ação de fluidos."),
                P("Cada linha de dados da amostra é desenhada como uma curva de padrão. O eixo horizontal é a sequência de elementos, e o eixo vertical mostra a razão normalizada ou a concentração original, em escala logarítmica."),
                H("Formato dos dados"),
                P("A primeira coluna é Category. Informe o nome da amostra ou do grupo, usado nas camadas e na legenda. Linhas com o mesmo nome são desenhadas como curvas separadas e exibidas no mesmo grupo. A segunda coluna é Sample. Informe o identificador da amostra; ele apenas indica de qual amostra os dados provêm e não é usado para desenhar nem para agrupar."),
                P("A ordem padrão dos elementos é Cs, Rb, Ba, Th, U, Nb, Ta, K, La, Ce, Pb, Pr, Sr, Nd, Zr, Hf, Sm, Eu, Ti, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Y."),
                P("Todos os valores são informados como concentração elementar em ppm. K e Ti também são informados em ppm. Não se pode inserir diretamente nessas colunas os valores de K₂O ou TiO₂ em wt%."),
                P("Se os dados originais trouxerem apenas teores de óxidos, converta-os antes: K (ppm) ≈ K₂O (wt%) × 8301; Ti (ppm) ≈ TiO₂ (wt%) × 5993. Nessas fórmulas, wt% é o valor percentual, de modo que 1 wt% é informado como 1."),
                P("Células vazias, conteúdo que não é reconhecido como número e concentrações menores ou iguais a 0 não são plotados. Informe a concentração medida. Não é necessário normalizar nem tomar logaritmos de antemão."),
                H("Normalização e eixos"),
                P("A normalização fica ligada por padrão. O padrão recomendado é o manto primitivo de Sun & McDonough (1989). Também há padrões de condrito, crosta continental, outros mantos primitivos e membros extremos basálticos."),
                P("O valor normalizado é a concentração de um elemento na amostra dividida pela concentração do mesmo elemento no padrão de referência."),
                P("O valor normalizado não tem unidade. Um valor maior que 1 indica enriquecimento em relação à composição de referência escolhida, e um valor menor que 1 indica empobrecimento em relação a essa composição."),
                P("Se um elemento não tiver valor de referência no padrão atual, ele não aparece no eixo horizontal enquanto a normalização está ligada. Com a normalização desligada, a concentração original é plotada. O eixo vertical fica então em ppm e continua logarítmico."),
                P("O padrão de referência e a ordem dos elementos alteram a aparência das curvas. Ao comparar diagramas, mantenha iguais o padrão e a ordem."),
                H("Interpretação"),
                P("A interpretação pode considerar o padrão geral e os picos ou vales locais em Nb–Ta, Pb, Sr, Eu, Ti e posições semelhantes. Essas feições podem indicar a composição da fonte, a cristalização fracionada de minerais, a contaminação ou a ação de fluidos."),
                P("Um pico ou vale local mostra a diferença de um elemento em relação à composição de referência atual e aos elementos vizinhos. Ele não equivale por si só a um processo geológico determinado. Uma anomalia negativa de Nb–Ta, por exemplo, não serve sozinha como critério de ambiente tectônico. As anomalias de Sr, Eu e Ti também precisam ser interpretadas com a mineralogia e outras evidências."),
                P("Rb, Ba, K, Sr e elementos semelhantes podem ser afetados por alteração ou por atividade de fluidos. Antes de interpretar uma curva, verifique o grau de alteração da amostra, a qualidade analítica e os limites de detecção. A linha entre elementos ausentes não significa que essa concentração tenha sido estimada."),
                H("Referências"),
                P(SunMcDonough1989),
                P(McDonoughSun1995),
                P("No uso real, cite também a publicação original do padrão de normalização escolhido.")
            ]
        };

        private static readonly Dictionary<string, Block[]> HarkerDocuments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] =
            [
                H("哈克多联图"),
                H("功能概述"),
                P("哈克多联图将多个成分变量分别对同一个横轴变量绘制为散点图，并按统一布局排列，用于观察样品的成分协变关系。"),
                P("经典哈克图以 SiO₂ 为横轴，常用于研究岩浆演化过程中主量氧化物和微量元素的变化趋势，也可辅助识别样品分组、异常值及可能的混合关系。"),
                H("变量与单位"),
                P("横轴变量可选择 SiO₂、MgO、TiO₂ 或 K₂O，默认使用 SiO₂。各面板使用同一个横轴变量，每个面板对应一个纵轴变量。"),
                P("可选纵轴变量包括：主量氧化物 SiO₂、TiO₂、Al₂O₃、Fe₂O₃ᵀ、MgO、CaO、Na₂O、K₂O、P₂O₅、MnO，单位 wt%；总碱 Na₂O+K₂O，单位 wt%；镁值 Mg#，无量纲；微量元素 Zr、Rb、Sr、Ba、Cr、Ni，单位 ppm。"),
                P("已选为横轴的变量不能同时作为纵轴变量。"),
                P("默认纵轴面板为 TiO₂、Al₂O₃、Fe₂O₃ᵀ、MgO、CaO、Na₂O、K₂O 和 P₂O₅。"),
                H("数据格式"),
                P("第一列为 Category，填写样品分组，例如岩性、采样区域或岩浆期次。同一分组在所有面板中使用一致的颜色与符号。第二列为 Sample，填写样品编号，仅用于标明数据来自哪个样品，不参与绘图与分组。"),
                P("第二列为横轴变量，其后各列为按面板顺序排列的纵轴变量。每行代表一个样品，列名使用软件生成的变量键，例如 SiO2、Al2O3、Fe2O3T。"),
                P("主量氧化物使用 wt% 数值，例如 SiO₂ 为 65.2 wt% 时输入 65.2。"),
                P("Fe2O3T 表示以 Fe₂O₃ 形式表达的全铁，不能直接填入以 FeO 表达的全铁数值。"),
                P("Na2O+K2O 和 Mg# 需要直接填写，软件不会根据其他列自动计算。"),
                P("Mg# 常采用 100×Mg/(Mg+Fe²⁺) 的摩尔比定义；使用其他铁价态假设或全铁计算时，应明确说明，并保证样品间计算口径一致。"),
                P("缺失数据保留为空白，避免用 0 代替未知值。"),
                P("某行横轴值缺失或无法识别为数字时，该行不参与绘图。仅某个纵轴值缺失时，只省略对应面板中的样点，其他面板仍可正常显示。所有纵轴值均无效时，该行不参与绘图。"),
                H("结果解读"),
                P("以 SiO₂ 为横轴时，某些成分随 SiO₂ 升高而降低或升高，可以为结晶分异及其他岩浆过程提供线索。趋势转折、离散程度变化或不同分组之间的差异，也可能反映矿物组合变化、源区差异或多期岩浆活动。"),
                P("趋势解释应结合矿物学和其他地球化学证据。线性关系不能单独证明岩浆混合，连续趋势也不能单独证明样品属于同一演化系列。"),
                P("主量氧化物数据受总量约束，可能产生闭合效应。比较不同来源的数据前，应统一铁的表达方式、挥发分处理方式及是否进行无水归一化，并在图注或方法部分注明。"),
                H("参考文献"),
                P(Harker1909)
            ],
            ["zh-TW"] =
            [
                H("哈克多聯圖"),
                H("功能概述"),
                P("哈克多聯圖將多個成分變數分別對同一個橫軸變數繪製為散點圖，並按統一佈局排列，用於觀察樣品的成分共變關係。"),
                P("經典哈克圖以 SiO₂ 為橫軸，常用於研究岩漿演化過程中主量氧化物和微量元素的變化趨勢，也可輔助識別樣品分組、異常值及可能的混合關係。"),
                H("變數與單位"),
                P("橫軸變數可選擇 SiO₂、MgO、TiO₂ 或 K₂O，預設使用 SiO₂。各面板使用同一個橫軸變數，每個面板對應一個縱軸變數。"),
                P("可選縱軸變數包括：主量氧化物 SiO₂、TiO₂、Al₂O₃、Fe₂O₃ᵀ、MgO、CaO、Na₂O、K₂O、P₂O₅、MnO，單位 wt%；總鹼 Na₂O+K₂O，單位 wt%；鎂值 Mg#，無因次；微量元素 Zr、Rb、Sr、Ba、Cr、Ni，單位 ppm。"),
                P("已選為橫軸的變數不能同時作為縱軸變數。"),
                P("預設縱軸面板為 TiO₂、Al₂O₃、Fe₂O₃ᵀ、MgO、CaO、Na₂O、K₂O 和 P₂O₅。"),
                H("資料格式"),
                P("第一列為 Category，填寫樣品分組，例如岩性、採樣區域或岩漿期次。同一分組在所有面板中使用一致的顏色與符號。第二列為 Sample，填寫樣品編號，僅用於標明資料來自哪個樣品，不參與繪圖與分組。"),
                P("第二列為橫軸變數，其後各列為按面板順序排列的縱軸變數。每行代表一個樣品，列名使用軟體產生的變數鍵，例如 SiO2、Al2O3、Fe2O3T。"),
                P("主量氧化物使用 wt% 數值，例如 SiO₂ 為 65.2 wt% 時輸入 65.2。"),
                P("Fe2O3T 表示以 Fe₂O₃ 形式表達的全鐵，不能直接填入以 FeO 表達的全鐵數值。"),
                P("Na2O+K2O 和 Mg# 需要直接填寫，軟體不會根據其他列自動計算。"),
                P("Mg# 常採用 100×Mg/(Mg+Fe²⁺) 的摩爾比定義；使用其他鐵價態假設或全鐵計算時，應明確說明，並保證樣品間計算口徑一致。"),
                P("缺失資料保留為空白，避免用 0 代替未知值。"),
                P("某行橫軸值缺失或無法識別為數字時，該行不參與繪圖。僅某個縱軸值缺失時，只省略對應面板中的樣點，其他面板仍可正常顯示。所有縱軸值均無效時，該行不參與繪圖。"),
                H("結果解讀"),
                P("以 SiO₂ 為橫軸時，某些成分隨 SiO₂ 升高而降低或升高，可以為結晶分異及其他岩漿過程提供線索。趨勢轉折、離散程度變化或不同分組之間的差異，也可能反映礦物組合變化、源區差異或多期岩漿活動。"),
                P("趨勢解釋應結合礦物學和其他地球化學證據。線性關係不能單獨證明岩漿混合，連續趨勢也不能單獨證明樣品屬於同一演化系列。"),
                P("主量氧化物資料受總量約束，可能產生閉合效應。比較不同來源的資料前，應統一鐵的表達方式、揮發分處理方式及是否進行無水歸一化，並在圖注或方法部分註明。"),
                H("參考文獻"),
                P(Harker1909)
            ],
            ["en-US"] =
            [
                H("Harker Diagrams"),
                H("Overview"),
                P("Harker diagrams plot several compositional variables against the same horizontal-axis variable as scatter plots and arrange them in one layout, so compositional covariation among samples can be examined."),
                P("The classic Harker diagram uses SiO₂ on the horizontal axis. It is commonly used to examine trends in major oxides and trace elements during magmatic evolution, and it can also help recognize sample groups, outliers, and possible mixing relations."),
                H("Variables and Units"),
                P("The horizontal-axis variable can be SiO₂, MgO, TiO₂, or K₂O. The default is SiO₂. Every panel uses the same horizontal-axis variable, and each panel corresponds to one vertical-axis variable."),
                P("Available vertical-axis variables: major oxides SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅, and MnO, in wt%; total alkalis Na₂O+K₂O, in wt%; magnesium number Mg#, dimensionless; trace elements Zr, Rb, Sr, Ba, Cr, and Ni, in ppm."),
                P("A variable already chosen for the horizontal axis cannot also be used as a vertical-axis variable."),
                P("The default vertical-axis panels are TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, and P₂O₅."),
                H("Data Format"),
                P("The first column is Category. Enter the sample group, for example lithology, sampling area, or magmatic stage. The same group uses the same color and symbol on every panel. The second column is Sample. Enter the sample ID; it only records which sample the data comes from and is not used for plotting or grouping."),
                P("The second column is the horizontal-axis variable. The following columns are the vertical-axis variables in panel order. Each row is one sample. Column names are the variable keys generated by the software, such as SiO2, Al2O3, and Fe2O3T."),
                P("Enter major oxides as wt% numbers. If SiO₂ is 65.2 wt%, enter 65.2."),
                P("Fe2O3T is total iron expressed as Fe₂O₃. Do not enter total iron that is expressed as FeO."),
                P("Enter Na2O+K2O and Mg# directly. The software does not calculate them from the other columns."),
                P("Mg# is commonly defined as the molar ratio 100×Mg/(Mg+Fe²⁺). If another assumption about iron valence or a total-iron calculation is used, state it and keep the basis of calculation consistent among samples."),
                P("Leave missing data blank. Do not replace an unknown value with 0."),
                P("If the horizontal-axis value of a row is missing or is not a number, that row is not plotted. If only one vertical-axis value is missing, the point is omitted on that panel only and the other panels still show the sample. If every vertical-axis value is invalid, the row is not plotted."),
                H("Interpretation"),
                P("With SiO₂ on the horizontal axis, components that fall or rise as SiO₂ increases can provide clues to fractional crystallization and other magmatic processes. A break in trend, a change in scatter, or a difference between groups may also reflect a change in mineral assemblage, a difference in source, or more than one episode of magmatic activity."),
                P("Interpret trends together with mineralogy and other geochemical evidence. A linear relation does not by itself prove magma mixing, and a continuous trend does not by itself prove that the samples belong to one evolutionary series."),
                P("Major-oxide data are constrained by a constant sum and can produce a closure effect. Before comparing data from different sources, use a consistent expression of iron, a consistent treatment of volatiles, and a consistent choice about anhydrous normalization, and state these choices in the figure caption or the methods."),
                H("References"),
                P(Harker1909)
            ],
            ["de-DE"] =
            [
                H("Harker-Diagramme"),
                H("Überblick"),
                P("Harker-Diagramme stellen mehrere Zusammensetzungsvariablen jeweils gegen dieselbe Variable der horizontalen Achse als Punktdiagramme dar und ordnen sie in einem einheitlichen Layout an. Damit lässt sich die Kovariation der Probenzusammensetzung betrachten."),
                P("Das klassische Harker-Diagramm verwendet SiO₂ auf der horizontalen Achse. Es dient häufig dazu, Trends der Hauptelementoxide und Spurenelemente während der magmatischen Entwicklung zu untersuchen, und kann außerdem Probengruppen, Ausreißer und mögliche Mischungsbeziehungen erkennen helfen."),
                H("Variablen und Einheiten"),
                P("Als Variable der horizontalen Achse können SiO₂, MgO, TiO₂ oder K₂O gewählt werden. Voreingestellt ist SiO₂. Alle Teilbilder verwenden dieselbe Variable der horizontalen Achse, und jedes Teilbild entspricht einer Variablen der vertikalen Achse."),
                P("Verfügbare Variablen der vertikalen Achse: Hauptelementoxide SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅ und MnO, Einheit wt%; Gesamtalkalien Na₂O+K₂O, Einheit wt%; Magnesiumzahl Mg#, dimensionslos; Spurenelemente Zr, Rb, Sr, Ba, Cr und Ni, Einheit ppm."),
                P("Eine Variable, die bereits für die horizontale Achse gewählt ist, kann nicht zugleich Variable der vertikalen Achse sein."),
                P("Die voreingestellten Teilbilder der vertikalen Achse sind TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O und P₂O₅."),
                H("Datenformat"),
                P("Die erste Spalte ist Category. Tragen Sie die Probengruppe ein, zum Beispiel Lithologie, Probengebiet oder magmatische Phase. Dieselbe Gruppe verwendet in allen Teilbildern dieselbe Farbe und dasselbe Symbol. Die zweite Spalte ist Sample. Tragen Sie die Probenkennung ein; sie dokumentiert nur, aus welcher Probe die Daten stammen, und wird weder zum Zeichnen noch zum Gruppieren verwendet."),
                P("Die zweite Spalte ist die Variable der horizontalen Achse. Die folgenden Spalten sind die Variablen der vertikalen Achse in der Reihenfolge der Teilbilder. Jede Zeile ist eine Probe. Die Spaltennamen sind die von der Software erzeugten Variablenschlüssel, zum Beispiel SiO2, Al2O3 und Fe2O3T."),
                P("Hauptelementoxide werden als Zahlen in wt% eingegeben. Beträgt SiO₂ beispielsweise 65,2 wt%, wird 65,2 eingetragen."),
                P("Fe2O3T ist das Gesamteisen, ausgedrückt als Fe₂O₃. Ein als FeO ausgedrückter Gesamteisengehalt darf nicht direkt eingetragen werden."),
                P("Na2O+K2O und Mg# werden direkt eingegeben. Die Software berechnet sie nicht aus den anderen Spalten."),
                P("Mg# wird häufig als Molverhältnis 100×Mg/(Mg+Fe²⁺) definiert. Werden eine andere Annahme zur Eisenwertigkeit oder eine Berechnung mit Gesamteisen verwendet, ist dies anzugeben, und die Berechnungsgrundlage muss zwischen den Proben einheitlich sein."),
                P("Fehlende Daten bleiben leer. Ein unbekannter Wert sollte nicht durch 0 ersetzt werden."),
                P("Fehlt der Wert der horizontalen Achse oder lässt er sich nicht als Zahl lesen, wird die Zeile nicht dargestellt. Fehlt nur ein Wert der vertikalen Achse, entfällt der Punkt allein im entsprechenden Teilbild; die übrigen Teilbilder zeigen die Probe weiterhin. Sind alle Werte der vertikalen Achse ungültig, wird die Zeile nicht dargestellt."),
                H("Interpretation"),
                P("Mit SiO₂ auf der horizontalen Achse können Komponenten, die mit steigendem SiO₂ ab- oder zunehmen, Hinweise auf fraktionierte Kristallisation und andere magmatische Prozesse geben. Knicke im Trend, Änderungen der Streuung oder Unterschiede zwischen Gruppen können außerdem auf einen Wechsel der Mineralvergesellschaftung, unterschiedliche Quellen oder mehrphasige magmatische Aktivität hinweisen."),
                P("Trends sind zusammen mit mineralogischen und weiteren geochemischen Befunden zu deuten. Eine lineare Beziehung beweist für sich genommen keine Magmenmischung, und ein durchgehender Trend beweist für sich genommen nicht, dass die Proben zu einer einzigen Entwicklungsserie gehören."),
                P("Daten der Hauptelementoxide sind durch die Summe eingeschränkt und können einen Schließungseffekt erzeugen. Vor dem Vergleich von Daten verschiedener Herkunft sollten die Ausdrucksform des Eisens, die Behandlung der flüchtigen Bestandteile und eine eventuelle wasserfreie Normierung vereinheitlicht und in der Bildunterschrift oder im Methodenteil angegeben werden."),
                H("Literatur"),
                P(Harker1909)
            ],
            ["es-ES"] =
            [
                H("Diagramas de Harker"),
                H("Descripción general"),
                P("Los diagramas de Harker representan varias variables de composición frente a una misma variable del eje horizontal, como diagramas de dispersión, y las disponen en un diseño común para observar la covariación composicional de las muestras."),
                P("El diagrama de Harker clásico usa SiO₂ en el eje horizontal. Se emplea con frecuencia para estudiar las tendencias de los óxidos mayores y los elementos traza durante la evolución magmática, y también puede ayudar a reconocer grupos de muestras, valores anómalos y posibles relaciones de mezcla."),
                H("Variables y unidades"),
                P("La variable del eje horizontal puede ser SiO₂, MgO, TiO₂ o K₂O. El valor predeterminado es SiO₂. Todos los paneles usan la misma variable del eje horizontal, y cada panel corresponde a una variable del eje vertical."),
                P("Variables disponibles del eje vertical: óxidos mayores SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅ y MnO, en wt%; álcalis totales Na₂O+K₂O, en wt%; número de magnesio Mg#, adimensional; elementos traza Zr, Rb, Sr, Ba, Cr y Ni, en ppm."),
                P("Una variable ya elegida para el eje horizontal no puede usarse al mismo tiempo como variable del eje vertical."),
                P("Los paneles predeterminados del eje vertical son TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O y P₂O₅."),
                H("Formato de datos"),
                P("La primera columna es Category. Escriba el grupo de muestras, por ejemplo litología, zona de muestreo o episodio magmático. El mismo grupo usa el mismo color y el mismo símbolo en todos los paneles. La segunda columna es Sample. Escriba el identificador de la muestra; solo indica de qué muestra proceden los datos y no se usa para dibujar ni agrupar."),
                P("La segunda columna es la variable del eje horizontal. Las columnas siguientes son las variables del eje vertical en el orden de los paneles. Cada fila es una muestra. Los nombres de columna son las claves de variable generadas por el programa, como SiO2, Al2O3 y Fe2O3T."),
                P("Los óxidos mayores se introducen como valores en wt%. Si SiO₂ es 65,2 wt%, se introduce 65,2."),
                P("Fe2O3T es el hierro total expresado como Fe₂O₃. No se puede introducir directamente un hierro total expresado como FeO."),
                P("Na2O+K2O y Mg# se escriben directamente. El programa no los calcula a partir de las otras columnas."),
                P("Mg# suele definirse como la relación molar 100×Mg/(Mg+Fe²⁺). Si se usa otra hipótesis sobre el estado de valencia del hierro o un cálculo con hierro total, debe indicarse y mantenerse el mismo criterio entre las muestras."),
                P("Los datos ausentes se dejan en blanco. No sustituya un valor desconocido por 0."),
                P("Si falta el valor del eje horizontal de una fila o no se reconoce como número, esa fila no se representa. Si solo falta un valor del eje vertical, se omite el punto únicamente en el panel correspondiente y los demás paneles siguen mostrando la muestra. Si todos los valores del eje vertical no son válidos, la fila no se representa."),
                H("Interpretación"),
                P("Con SiO₂ en el eje horizontal, los componentes que disminuyen o aumentan al aumentar SiO₂ pueden aportar indicios de cristalización fraccionada y de otros procesos magmáticos. Un cambio de tendencia, una variación de la dispersión o una diferencia entre grupos también pueden reflejar un cambio de asociación mineral, una diferencia de fuente o más de un episodio magmático."),
                P("La interpretación de las tendencias debe combinar la mineralogía y otras evidencias geoquímicas. Una relación lineal no demuestra por sí sola una mezcla de magmas, y una tendencia continua no demuestra por sí sola que las muestras pertenezcan a una misma serie evolutiva."),
                P("Los datos de óxidos mayores están limitados por la suma total y pueden producir un efecto de cierre. Antes de comparar datos de distinta procedencia, unifique la forma de expresar el hierro, el tratamiento de los volátiles y si se aplica una normalización anhidra, e indíquelo en el pie de figura o en los métodos."),
                H("Referencias"),
                P(Harker1909)
            ],
            ["ja-JP"] =
            [
                H("ハーカー図（多連図）"),
                H("機能概要"),
                P("ハーカー多連図は、複数の組成変数を同じ横軸変数に対する散布図として描き、統一した配置で並べて、試料の組成の共変動を見るための図です。"),
                P("古典的なハーカー図は横軸に SiO₂ を取ります。マグマ進化における主要酸化物と微量元素の変化傾向を調べるためによく使われ、試料グループ、異常値、混合の可能性を見分ける助けにもなります。"),
                H("変数と単位"),
                P("横軸変数は SiO₂、MgO、TiO₂、K₂O から選べ、初期値は SiO₂ です。すべてのパネルは同じ横軸変数を使い、各パネルは 1 つの縦軸変数に対応します。"),
                P("選べる縦軸変数は次のとおりです。主要酸化物 SiO₂、TiO₂、Al₂O₃、Fe₂O₃ᵀ、MgO、CaO、Na₂O、K₂O、P₂O₅、MnO、単位 wt%。総アルカリ Na₂O+K₂O、単位 wt%。マグネシウム値 Mg#、無次元。微量元素 Zr、Rb、Sr、Ba、Cr、Ni、単位 ppm。"),
                P("横軸に選んだ変数は、同時に縦軸変数にはできません。"),
                P("初期の縦軸パネルは TiO₂、Al₂O₃、Fe₂O₃ᵀ、MgO、CaO、Na₂O、K₂O、P₂O₅ です。"),
                H("データ形式"),
                P("最初の列は Category です。岩相、採取地域、マグマ期など、試料のグループを入力します。同じグループはすべてのパネルで同じ色と記号を使います。2 番目の列は Sample です。試料番号を入力します。データがどの試料のものかを示すだけで、描画やグループ化には使われません。"),
                P("2 列目は横軸変数、その後の列はパネル順の縦軸変数です。1 行が 1 試料です。列名はソフトウェアが作る変数キーで、たとえば SiO2、Al2O3、Fe2O3T です。"),
                P("主要酸化物は wt% の数値で入力します。たとえば SiO₂ が 65.2 wt% なら 65.2 と入力します。"),
                P("Fe2O3T は Fe₂O₃ として表した全鉄です。FeO として表した全鉄の数値をそのまま入れることはできません。"),
                P("Na2O+K2O と Mg# は直接入力します。ソフトウェアは他の列から自動計算しません。"),
                P("Mg# は通常、モル比 100×Mg/(Mg+Fe²⁺) で定義します。別の鉄の価数の仮定や全鉄による計算を使う場合は、その旨を明示し、試料間で計算の基準を揃えてください。"),
                P("欠損データは空欄のままにし、未知の値を 0 で代用しないでください。"),
                P("ある行の横軸値が欠損している、または数値として読めない場合、その行は描画されません。縦軸値の 1 つだけが欠損している場合は、対応するパネルの点だけが省かれ、他のパネルは表示されます。すべての縦軸値が無効な場合、その行は描画されません。"),
                H("結果の読み方"),
                P("横軸を SiO₂ にしたとき、SiO₂ の増加に伴って下がる、または上がる成分は、結晶分化やその他のマグマ過程を考える手がかりになります。傾向の折れ、ばらつきの変化、グループ間の違いは、鉱物組み合わせの変化、源の違い、複数期のマグマ活動を反映している可能性もあります。"),
                P("傾向の解釈は鉱物学や他の地球化学的証拠と合わせて行ってください。直線関係だけではマグマ混合を証明できず、連続した傾向だけでも試料が同一の進化系列に属することを証明できません。"),
                P("主要酸化物データは総量に制約されるため、閉鎖効果が生じることがあります。出所の異なるデータを比較する前に、鉄の表し方、揮発成分の扱い、無水換算の有無を揃え、図の説明または方法に記してください。"),
                H("参考文献"),
                P(Harker1909)
            ],
            ["ko-KR"] =
            [
                H("하커 다연도"),
                H("기능 개요"),
                P("하커 다연도는 여러 성분 변수를 같은 가로축 변수에 대한 산점도로 그리고, 하나의 배치로 배열해 시료의 성분 공변화를 살핍니다."),
                P("고전적인 하커 그림은 가로축을 SiO₂로 둡니다. 마그마 진화 과정에서 주성분 산화물과 미량원소의 변화 경향을 연구할 때 자주 쓰이며, 시료 그룹, 이상값, 가능한 혼합 관계를 알아보는 데도 도움이 됩니다."),
                H("변수와 단위"),
                P("가로축 변수는 SiO₂, MgO, TiO₂, K₂O 중에서 고를 수 있으며 기본값은 SiO₂입니다. 모든 패널은 같은 가로축 변수를 쓰고, 각 패널은 하나의 세로축 변수에 대응합니다."),
                P("선택할 수 있는 세로축 변수는 다음과 같습니다. 주성분 산화물 SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅, MnO, 단위 wt%. 총알칼리 Na₂O+K₂O, 단위 wt%. 마그네슘 값 Mg#, 무차원. 미량원소 Zr, Rb, Sr, Ba, Cr, Ni, 단위 ppm."),
                P("가로축으로 이미 고른 변수는 동시에 세로축 변수가 될 수 없습니다."),
                P("기본 세로축 패널은 TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅입니다."),
                H("데이터 형식"),
                P("첫 열은 Category입니다. 암상, 채취 지역, 마그마 기 같은 시료 그룹을 입력합니다. 같은 그룹은 모든 패널에서 같은 색과 기호를 사용합니다. 두 번째 열은 Sample입니다. 시료 번호를 입력합니다. 데이터가 어느 시료의 것인지만 나타내며 그리기와 그룹화에는 사용되지 않습니다."),
                P("둘째 열은 가로축 변수이고, 그 뒤의 열은 패널 순서대로 배열한 세로축 변수입니다. 한 행이 한 시료입니다. 열 이름은 소프트웨어가 만든 변수 키이며, 예를 들어 SiO2, Al2O3, Fe2O3T입니다."),
                P("주성분 산화물은 wt% 수치로 입력합니다. 예를 들어 SiO₂가 65.2 wt%이면 65.2를 입력합니다."),
                P("Fe2O3T는 Fe₂O₃로 나타낸 총 철입니다. FeO로 나타낸 총 철 수치를 그대로 넣을 수 없습니다."),
                P("Na2O+K2O와 Mg#는 직접 입력합니다. 소프트웨어가 다른 열로 자동 계산하지 않습니다."),
                P("Mg#는 보통 몰비 100×Mg/(Mg+Fe²⁺)로 정의합니다. 다른 철 원자가 가정이나 총 철 계산을 쓰면 그 점을 분명히 밝히고, 시료 사이의 계산 기준을 같게 해야 합니다."),
                P("빠진 데이터는 빈 칸으로 두고, 알 수 없는 값을 0으로 바꾸지 마십시오."),
                P("어떤 행의 가로축 값이 없거나 숫자로 읽을 수 없으면 그 행은 그려지지 않습니다. 세로축 값 하나만 없으면 해당 패널의 점만 빠지고 다른 패널은 그대로 표시됩니다. 모든 세로축 값이 유효하지 않으면 그 행은 그려지지 않습니다."),
                H("결과 해석"),
                P("가로축을 SiO₂로 둘 때, SiO₂가 높아짐에 따라 낮아지거나 높아지는 성분은 결정 분화와 다른 마그마 과정의 실마리가 됩니다. 경향의 꺾임, 분산 정도의 변화, 그룹 사이의 차이는 광물 조합의 변화, 근원 차이, 여러 시기의 마그마 활동을 반영할 수도 있습니다."),
                P("경향은 광물학과 다른 지구화학 증거와 함께 해석해야 합니다. 선형 관계만으로 마그마 혼합을 증명할 수 없고, 연속된 경향만으로 시료가 같은 진화 계열에 속한다고 증명할 수도 없습니다."),
                P("주성분 산화물 자료는 총합의 제약을 받으므로 폐쇄 효과가 생길 수 있습니다. 출처가 다른 자료를 비교하기 전에 철의 표현 방식, 휘발 성분 처리, 무수 정규화 여부를 통일하고, 그림 설명이나 방법에 밝혀야 합니다."),
                H("참고문헌"),
                P(Harker1909)
            ],
            ["ru-RU"] =
            [
                H("Диаграммы Харкера"),
                H("Назначение"),
                P("Диаграммы Харкера строят несколько переменных состава против одной и той же переменной горизонтальной оси в виде точечных диаграмм и располагают их в единой компоновке, чтобы рассматривать ковариацию состава образцов."),
                P("Классическая диаграмма Харкера использует SiO₂ на горизонтальной оси. Её часто применяют для изучения трендов главных оксидов и микроэлементов в ходе магматической эволюции; она также помогает выделять группы образцов, выбросы и возможные отношения смешения."),
                H("Переменные и единицы"),
                P("Переменной горизонтальной оси могут быть SiO₂, MgO, TiO₂ или K₂O. По умолчанию это SiO₂. Все панели используют одну и ту же переменную горизонтальной оси, и каждой панели соответствует одна переменная вертикальной оси."),
                P("Доступные переменные вертикальной оси: главные оксиды SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅ и MnO, единица wt%; сумма щелочей Na₂O+K₂O, единица wt%; магнезиальное число Mg#, безразмерное; микроэлементы Zr, Rb, Sr, Ba, Cr и Ni, единица ppm."),
                P("Переменная, уже выбранная для горизонтальной оси, не может одновременно быть переменной вертикальной оси."),
                P("Панели вертикальной оси по умолчанию: TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O и P₂O₅."),
                H("Формат данных"),
                P("Первый столбец — Category. Введите группу образцов, например литологию, район отбора или магматическую фазу. Одна и та же группа использует одинаковые цвет и знак на всех панелях. Второй столбец — Sample. Введите номер образца; он лишь показывает, какому образцу принадлежат данные, и не используется ни для построения, ни для группировки."),
                P("Второй столбец — переменная горизонтальной оси, далее идут переменные вертикальной оси в порядке панелей. Каждая строка — один образец. Имена столбцов — ключи переменных, созданные программой, например SiO2, Al2O3 и Fe2O3T."),
                P("Главные оксиды вводятся числами в wt%. Если SiO₂ составляет 65,2 wt%, вводится 65,2."),
                P("Fe2O3T — общее железо, выраженное как Fe₂O₃. Общее железо, выраженное как FeO, вводить напрямую нельзя."),
                P("Na2O+K2O и Mg# вводятся непосредственно. Программа не вычисляет их по другим столбцам."),
                P("Mg# обычно определяют как мольное отношение 100×Mg/(Mg+Fe²⁺). Если используется другое допущение о валентности железа или расчёт по общему железу, это нужно указать и сохранить единый способ расчёта для всех образцов."),
                P("Отсутствующие данные оставляйте пустыми. Не заменяйте неизвестное значение нулём."),
                P("Если значение горизонтальной оси в строке отсутствует или не распознаётся как число, строка не строится. Если отсутствует только одно значение вертикальной оси, точка опускается лишь на соответствующей панели, а остальные панели продолжают показывать образец. Если все значения вертикальной оси недействительны, строка не строится."),
                H("Интерпретация"),
                P("При SiO₂ на горизонтальной оси компоненты, которые понижаются или повышаются с ростом SiO₂, могут указывать на кристаллизационную дифференциацию и другие магматические процессы. Перелом тренда, изменение разброса или различие между группами могут также отражать смену минеральной ассоциации, различие источников или несколько этапов магматической активности."),
                P("Тренды следует интерпретировать вместе с минералогией и другими геохимическими данными. Линейная зависимость сама по себе не доказывает смешение магм, а непрерывный тренд сам по себе не доказывает, что образцы принадлежат одной эволюционной серии."),
                P("Данные главных оксидов ограничены постоянной суммой и могут давать эффект постоянной суммы. Перед сравнением данных разного происхождения следует унифицировать форму выражения железа, учёт летучих и применение безводного пересчёта и указать это в подписи к рисунку или в разделе методов."),
                H("Литература"),
                P(Harker1909)
            ],
            ["fr-FR"] =
            [
                H("Diagrammes de Harker"),
                H("Présentation"),
                P("Les diagrammes de Harker représentent plusieurs variables de composition en nuages de points face à une même variable d’axe horizontal et les disposent selon une mise en page commune, afin d’observer la covariation de la composition des échantillons."),
                P("Le diagramme de Harker classique place SiO₂ sur l’axe horizontal. Il sert souvent à étudier les tendances des oxydes majeurs et des éléments en traces au cours de l’évolution magmatique, et il peut aussi aider à reconnaître des groupes d’échantillons, des valeurs aberrantes et de possibles relations de mélange."),
                H("Variables et unités"),
                P("La variable de l’axe horizontal peut être SiO₂, MgO, TiO₂ ou K₂O. La valeur par défaut est SiO₂. Tous les panneaux utilisent la même variable d’axe horizontal, et chaque panneau correspond à une variable d’axe vertical."),
                P("Variables d’axe vertical disponibles : oxydes majeurs SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅ et MnO, en wt% ; alcalins totaux Na₂O+K₂O, en wt% ; nombre de magnésium Mg#, sans dimension ; éléments en traces Zr, Rb, Sr, Ba, Cr et Ni, en ppm."),
                P("Une variable déjà choisie pour l’axe horizontal ne peut pas servir en même temps de variable d’axe vertical."),
                P("Les panneaux d’axe vertical par défaut sont TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O et P₂O₅."),
                H("Format des données"),
                P("La première colonne est Category. Saisissez le groupe d’échantillons, par exemple la lithologie, la zone d’échantillonnage ou l’épisode magmatique. Le même groupe utilise la même couleur et le même symbole sur tous les panneaux. La deuxième colonne est Sample. Saisissez l’identifiant de l’échantillon ; il indique seulement de quel échantillon proviennent les données et n’est utilisé ni pour le tracé ni pour le regroupement."),
                P("La deuxième colonne est la variable de l’axe horizontal. Les colonnes suivantes sont les variables de l’axe vertical dans l’ordre des panneaux. Chaque ligne est un échantillon. Les noms de colonnes sont les clés de variables produites par le logiciel, par exemple SiO2, Al2O3 et Fe2O3T."),
                P("Les oxydes majeurs se saisissent en valeurs de wt%. Si SiO₂ vaut 65,2 wt%, saisissez 65,2."),
                P("Fe2O3T est le fer total exprimé en Fe₂O₃. Un fer total exprimé en FeO ne peut pas être saisi directement."),
                P("Na2O+K2O et Mg# se saisissent directement. Le logiciel ne les calcule pas à partir des autres colonnes."),
                P("Mg# est en général défini par le rapport molaire 100×Mg/(Mg+Fe²⁺). Si une autre hypothèse sur la valence du fer ou un calcul sur le fer total est utilisée, il faut l’indiquer et conserver la même base de calcul d’un échantillon à l’autre."),
                P("Laissez les données manquantes vides. Ne remplacez pas une valeur inconnue par 0."),
                P("Si la valeur de l’axe horizontal d’une ligne est absente ou n’est pas reconnue comme un nombre, la ligne n’est pas tracée. Si une seule valeur d’axe vertical manque, le point est omis seulement sur le panneau correspondant et les autres panneaux affichent encore l’échantillon. Si toutes les valeurs d’axe vertical sont invalides, la ligne n’est pas tracée."),
                H("Interprétation"),
                P("Avec SiO₂ sur l’axe horizontal, des constituants qui diminuent ou augmentent quand SiO₂ augmente peuvent fournir des indices de cristallisation fractionnée et d’autres processus magmatiques. Une rupture de tendance, un changement de dispersion ou une différence entre groupes peuvent aussi refléter un changement d’assemblage minéral, une différence de source ou plusieurs épisodes d’activité magmatique."),
                P("Les tendances doivent être interprétées avec la minéralogie et d’autres données géochimiques. Une relation linéaire ne prouve pas à elle seule un mélange de magmas, et une tendance continue ne prouve pas à elle seule que les échantillons appartiennent à une même série évolutive."),
                P("Les données d’oxydes majeurs sont contraintes par une somme constante et peuvent produire un effet de fermeture. Avant de comparer des données d’origines différentes, unifiez l’expression du fer, le traitement des volatils et le choix d’une normalisation anhydre, et indiquez ces choix dans la légende de la figure ou dans les méthodes."),
                H("Références"),
                P(Harker1909)
            ],
            ["pt-BR"] =
            [
                H("Diagramas de Harker"),
                H("Visão geral"),
                P("Os diagramas de Harker representam várias variáveis de composição contra a mesma variável do eixo horizontal, como gráficos de dispersão, e as organizam em um layout comum para observar a covariação composicional das amostras."),
                P("O diagrama de Harker clássico usa SiO₂ no eixo horizontal. É usado com frequência para estudar tendências de óxidos maiores e elementos-traço durante a evolução magmática e também pode ajudar a reconhecer grupos de amostras, valores anômalos e possíveis relações de mistura."),
                H("Variáveis e unidades"),
                P("A variável do eixo horizontal pode ser SiO₂, MgO, TiO₂ ou K₂O. O padrão é SiO₂. Todos os painéis usam a mesma variável do eixo horizontal, e cada painel corresponde a uma variável do eixo vertical."),
                P("Variáveis disponíveis do eixo vertical: óxidos maiores SiO₂, TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O, P₂O₅ e MnO, em wt%; álcalis totais Na₂O+K₂O, em wt%; número de magnésio Mg#, adimensional; elementos-traço Zr, Rb, Sr, Ba, Cr e Ni, em ppm."),
                P("Uma variável já escolhida para o eixo horizontal não pode ser usada ao mesmo tempo como variável do eixo vertical."),
                P("Os painéis padrão do eixo vertical são TiO₂, Al₂O₃, Fe₂O₃ᵀ, MgO, CaO, Na₂O, K₂O e P₂O₅."),
                H("Formato dos dados"),
                P("A primeira coluna é Category. Informe o grupo de amostras, por exemplo litologia, área de amostragem ou episódio magmático. O mesmo grupo usa a mesma cor e o mesmo símbolo em todos os painéis. A segunda coluna é Sample. Informe o identificador da amostra; ele apenas indica de qual amostra os dados provêm e não é usado para desenhar nem para agrupar."),
                P("A segunda coluna é a variável do eixo horizontal. As colunas seguintes são as variáveis do eixo vertical na ordem dos painéis. Cada linha é uma amostra. Os nomes das colunas são as chaves de variável geradas pelo programa, como SiO2, Al2O3 e Fe2O3T."),
                P("Os óxidos maiores são informados como valores em wt%. Se SiO₂ for 65,2 wt%, informe 65,2."),
                P("Fe2O3T é o ferro total expresso como Fe₂O₃. Não se pode inserir diretamente um ferro total expresso como FeO."),
                P("Na2O+K2O e Mg# são digitados diretamente. O programa não os calcula a partir das outras colunas."),
                P("Mg# costuma ser definido pela razão molar 100×Mg/(Mg+Fe²⁺). Se for usada outra hipótese sobre a valência do ferro ou um cálculo com ferro total, isso deve ser declarado e o critério de cálculo deve ser o mesmo entre as amostras."),
                P("Deixe os dados ausentes em branco. Não substitua um valor desconhecido por 0."),
                P("Se o valor do eixo horizontal de uma linha estiver ausente ou não for reconhecido como número, essa linha não é plotada. Se apenas um valor do eixo vertical estiver ausente, o ponto é omitido só no painel correspondente e os outros painéis continuam mostrando a amostra. Se todos os valores do eixo vertical forem inválidos, a linha não é plotada."),
                H("Interpretação"),
                P("Com SiO₂ no eixo horizontal, componentes que diminuem ou aumentam à medida que SiO₂ aumenta podem indicar cristalização fracionada e outros processos magmáticos. Uma quebra de tendência, uma mudança na dispersão ou uma diferença entre grupos também podem refletir mudança na associação mineral, diferença de fonte ou mais de um episódio de atividade magmática."),
                P("As tendências devem ser interpretadas junto com a mineralogia e outras evidências geoquímicas. Uma relação linear não prova sozinha mistura de magmas, e uma tendência contínua não prova sozinha que as amostras pertençam a uma mesma série evolutiva."),
                P("Os dados de óxidos maiores são limitados pela soma constante e podem produzir um efeito de fechamento. Antes de comparar dados de origens diferentes, unifique a forma de expressar o ferro, o tratamento dos voláteis e a escolha de uma normalização anidra, e registre essas escolhas na legenda da figura ou nos métodos."),
                H("Referências"),
                P(Harker1909)
            ]
        };
    }
}
