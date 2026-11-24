import createLocaleFromEn from './createLocaleFromEn';

const JA = createLocaleFromEn({
  metaTitle: '地球化学の定量計算と科学作図をつなぐ次世代統合研究ワークベンチ',
  nav: { stage: 'ワークベンチ', features: '主な機能', toolkit: 'ポータル・ツール', roadmap: 'ロードマップ', docs: 'ドキュメント ↗', docsMobile: 'ドキュメント', download: '無料ダウンロード', downloadMobile: 'v0.9.0 ダウンロード' },
  hero: {
    eyebrow: 'v0.9.0 公開：EPMA 鉱物化学計算、CIPW・Niggli ノルム計算を追加、クラウドテンプレート継続拡充中 →',
    titleLine1: '地球科学の定量計算と科学作図をひとつに結ぶ',
    titleLine2: '次世代の統合型デスクトップ研究ワークベンチ',
    lead: '地球化学・岩石学・鉱物学・鉱床学の研究者向けに設計。データ整理、ホワイトボックス計算から 10 言語対応の出版品質 SVG 出力まで、「データ・計算・作図・共有」をひとつの洗練された環境で完結します。',
    primaryBtn: 'Windows 版をダウンロード',
    secondaryBtn: '3 分クイックスタートを読む',
    tutorialLink: 'モジュール解説を見る →',
  },
  metrics: [
    { val: '50', suffix: '+', title: '岩石分類・構造判別図テンプレート', lbl: 'クラウド経由で継続的に自動拡充' },
    { val: '20', suffix: '+', title: '地質温度圧力計・EPMA テンプレート', lbl: '再インストール不要で即時更新' },
    { val: '60', suffix: ' 種', title: 'CIPW・Niggli 標準鉱物', lbl: '10⁻⁶ 質量収支ホワイトボックス検証' },
    { val: '10', suffix: '+', title: '多言語 UI と国際誌図解ラベル', lbl: 'ワンクリックで投稿言語を無劣化切替' },
  ],
  features: {
    title: '厳密な学術出版と再現性のために磨き抜かれた設計',
    sub: '多言語での国際誌投稿、計算過程の透明化、オープンな拡張機構を体系的に統合しました。',
    tasTitle: '1 つの図で 10+ 言語：国際誌ラベルをワンクリック無劣化切替',
    svgTitle: '出版品質の SVG 出力と外部ベクターソフトへの直接連携',
    svgBridgeTag: 'SVG 直接転送',
    card3Title: 'ホワイトボックス行単位インスペクタ',
    card4Title: 'Excel ライクな操作性と自由数式シート',
    card5Title: 'オープンクラウドエコシステムと共有',
  },
  toolkit: {
    title: '専門計算だけではない：地球科学ポータルと実用マイクロツール',
    docBtn: 'ホームポータルとツール詳細を見る →',
  },
  roadmap: {
    title: '単なる作図ソフトを超え、成長し続ける地球科学研究基盤へ',
  },
  cta: {
    title: 'より一貫した研究ワークフローを体験し、地球科学ツール基盤を共に築きましょう',
    primaryBtn: 'Windows 最新版をダウンロード (v0.9.0)',
    secondaryBtn: 'アルゴリズム・テンプレート協力案内を読む →',
    copiedText: '✓ クリップボードにコピーしました',
  },
});

export default JA;
