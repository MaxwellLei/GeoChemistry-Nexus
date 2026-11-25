import React, {useState, useEffect, useRef, useCallback} from 'react';
import Link from '@docusaurus/Link';
import {Link as RouterLink} from 'react-router-dom';
import useBrokenLinks from '@docusaurus/useBrokenLinks';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import useBaseUrl from '@docusaurus/useBaseUrl';
import ErrorBoundary from '@docusaurus/ErrorBoundary';
import {PageMetadata, SkipToContentFallbackId} from '@docusaurus/theme-common';
import {useAlternatePageUtils} from '@docusaurus/theme-common/internal';
import LayoutProvider from '@theme/Layout/Provider';
import ErrorPageContent from '@theme/ErrorPageContent';

import {
  LOCALE_LIST,
  LOCALE_TO_TAS_KEY,
  TAS_LANG_BUTTONS,
  TAS_MULTILINGUAL,
  MARQUEE_ROW_1,
  MARQUEE_ROW_2,
} from '@site/src/data/homepageI18n';
// Resolved at build time to src/data/home/<current locale>.js (see docusaurus.config.js)
import HOME_COPY from '@gn/homeCopy';

import './studioHome.css';

/**
 * Internal doc link WITHOUT viewport prefetch.
 * Docusaurus' <Link> prefetches every internal link as soon as it scrolls into
 * view, which competes with the homepage's own assets. Here we only warm the
 * target route on hover / touch (user intent), and keep SPA navigation plus the
 * broken-link check.
 */
function HomeLink({to, children, ...rest}) {
  const href = useBaseUrl(to, {forcePrefix: true});
  const brokenLinks = useBrokenLinks();
  brokenLinks.collectLink(href);
  const warm = () => {
    if (typeof window !== 'undefined' && window.docusaurus) {
      window.docusaurus.preload(href);
    }
  };
  return (
    <RouterLink to={href} onMouseEnter={warm} onTouchStart={warm} {...rest}>
      {children}
    </RouterLink>
  );
}

function NexusCrystalLogo({size = 28, rx = 44}) {
  return (
    <svg width={size} height={size} viewBox="0 0 196 196" aria-hidden="true">
      <rect x="0" y="0" width="196" height="196" rx={rx} fill="#3645D8" />
      <g strokeLinejoin="round" strokeWidth="5">
        <polygon points="70,38 137,38 128,93 53,144 19,132 20,98" fill="#FFFFFF" stroke="#FFFFFF" />
        <polygon points="152,37 175,105 160,120 141,94" fill="#B8C2F2" stroke="#B8C2F2" />
        <polygon points="131,105 148,127 106,159 69,148" fill="#D9DFFF" stroke="#D9DFFF" />
      </g>
    </svg>
  );
}

function centerPillInBar(btnEl) {
  if (!btnEl || !btnEl.parentElement) return;
  const bar = btnEl.parentElement;
  if (bar.scrollWidth <= bar.clientWidth + 2) return;
  const targetLeft = btnEl.offsetLeft - bar.clientWidth / 2 + btnEl.offsetWidth / 2;
  bar.scrollTo({left: Math.max(0, targetLeft), behavior: 'smooth'});
}

function StudioHomepageContent() {
  const {
    i18n: {currentLocale},
    siteConfig,
  } = useDocusaurusContext();
  const alternatePageUtils = useAlternatePageUtils();
  // Homepage screenshots: served from the object-storage bucket of this locale
  // (zh-Hans -> Beijing, others -> Hong Kong; see docusaurus.config.js), or from this
  // site's own /img/... when the CDN is switched off.
  const cdnBases = (siteConfig.customFields && siteConfig.customFields.cdnBases) || {};
  const cdnBase = (cdnBases[currentLocale] || cdnBases.default || '').replace(/\/+$/, '');
  const withBase = useCallback(
    // p always starts with "/img/..."; default-locale root copy is shared by every locale
    (p) => (cdnBase ? `${cdnBase}${p}` : p),
    [cdnBase],
  );

  const copy = HOME_COPY;
  const currentLocaleInfo =
    LOCALE_LIST.find((item) => item.locale === currentLocale) || LOCALE_LIST[2];

  // Navbar Locale Dropdown + Custom Slender Proximity Scrollbar + Mobile Drawer
  const [localeOpen, setLocaleOpen] = useState(false);
  const [nearScroll, setNearScroll] = useState(false);
  const [isDraggingThumb, setIsDraggingThumb] = useState(false);
  const [thumbStyle, setThumbStyle] = useState({height: 40, y: 0, visible: true});
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const localeMenuRef = useRef(null);
  const localeGridRef = useRef(null);
  const localeTrackRef = useRef(null);
  const dragStateRef = useRef({startY: 0, startScroll: 0});

  // Workbench Stage 6-Tab Switcher
  const [activeTabIdx, setActiveTabIdx] = useState(0);
  const [winImgFading, setWinImgFading] = useState(false);

  // 10-Language Interactive TAS Diagram
  const [tasLang, setTasLang] = useState(
    () => LOCALE_TO_TAS_KEY[currentLocale] || 'en',
  );
  const [tasFlash, setTasFlash] = useState(false);
  const tasCardRef = useRef(null);

  // Micro-Tools 5-Tab Switcher
  const [activeUtilIdx, setActiveUtilIdx] = useState(0);

  // Click-to-Copy Contact Pills
  const [copiedKey, setCopiedKey] = useState(null);

  useEffect(() => {
    setTasLang(LOCALE_TO_TAS_KEY[currentLocale] || 'en');
  }, [currentLocale]);

  const syncLocaleScrollThumb = useCallback(() => {
    const grid = localeGridRef.current;
    const track = localeTrackRef.current;
    if (!grid) return;
    const sh = grid.scrollHeight;
    const ch = grid.clientHeight;
    if (sh <= ch + 1) {
      setThumbStyle((prev) => ({...prev, visible: false}));
      return;
    }
    const trackH = (track && track.clientHeight) || ch - 4;
    const thumbH = Math.max(28, (ch / sh) * trackH);
    const maxTop = Math.max(0, trackH - thumbH);
    const ratio = grid.scrollTop / Math.max(1, sh - ch);
    setThumbStyle({
      height: thumbH,
      y: ratio * maxTop,
      visible: true,
    });
  }, []);

  useEffect(() => {
    if (localeOpen) {
      const id = window.requestAnimationFrame(syncLocaleScrollThumb);
      return () => window.cancelAnimationFrame(id);
    }
    return undefined;
  }, [localeOpen, syncLocaleScrollThumb]);

  useEffect(() => {
    const closeMenus = () => {
      setLocaleOpen(false);
      setMobileMenuOpen(false);
    };
    document.addEventListener('click', closeMenus);
    return () => document.removeEventListener('click', closeMenus);
  }, []);

  useEffect(() => {
    if (!isDraggingThumb) return undefined;
    const onMove = (e) => {
      const grid = localeGridRef.current;
      const track = localeTrackRef.current;
      if (!grid) return;
      const sh = grid.scrollHeight;
      const ch = grid.clientHeight;
      const trackH = (track && track.clientHeight) || ch;
      const scrollDelta =
        ((e.clientY - dragStateRef.current.startY) /
          Math.max(1, trackH - thumbStyle.height)) *
        (sh - ch);
      grid.scrollTop = dragStateRef.current.startScroll + scrollDelta;
    };
    const onUp = () => setIsDraggingThumb(false);
    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
    return () => {
      window.removeEventListener('mousemove', onMove);
      window.removeEventListener('mouseup', onUp);
    };
  }, [isDraggingThumb, thumbStyle.height]);

  const handleThumbMouseDown = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (!localeGridRef.current) return;
    dragStateRef.current = {
      startY: e.clientY,
      startScroll: localeGridRef.current.scrollTop,
    };
    setIsDraggingThumb(true);
  };

  const handleLocaleMenuMouseMove = (e) => {
    if (!localeMenuRef.current) return;
    const rect = localeMenuRef.current.getBoundingClientRect();
    const distRight = rect.right - e.clientX;
    setNearScroll(distRight >= 0 && distRight <= 24);
  };

  const handleSelectStageTab = (idx, btnEl) => {
    if (idx === activeTabIdx) return;
    centerPillInBar(btnEl);
    setWinImgFading(true);
    window.setTimeout(() => {
      setActiveTabIdx(idx);
      setWinImgFading(false);
    }, 85);
  };

  const handleMarqueeCardClick = (targetTasKey) => {
    setTasLang(targetTasKey);
    if (tasCardRef.current) {
      tasCardRef.current.scrollIntoView({behavior: 'smooth', block: 'center'});
      setTasFlash(false);
      window.requestAnimationFrame(() => setTasFlash(true));
      window.setTimeout(() => setTasFlash(false), 820);
    }
  };

  const handleCopyText = (key, value) => {
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      navigator.clipboard.writeText(value);
    }
    setCopiedKey(key);
    window.setTimeout(() => {
      setCopiedKey((prev) => (prev === key ? null : prev));
    }, 1450);
  };

  const currentTab = copy.hero.tabs[activeTabIdx] || copy.hero.tabs[0];
  const currentTas = TAS_MULTILINGUAL[tasLang] || TAS_MULTILINGUAL.en;
  const currentUtil = copy.toolkit.items[activeUtilIdx] || copy.toolkit.items[0];

  const doubledRow1 = [...MARQUEE_ROW_1, ...MARQUEE_ROW_1];
  const doubledRow2 = [...MARQUEE_ROW_2, ...MARQUEE_ROW_2];

  return (
    <div id={SkipToContentFallbackId} className="gn-studio-root">
      {/* Floating Frosted Pill Header (Desktop + Mobile Adaptive Drawer) */}
      <div className="nav-wrap">
        <header className="gn-navbar">
          <a href="#top" className="brand">
            <NexusCrystalLogo size={28} rx={44} />
            <span>GeoChemistry Nexus</span>
            <span className="ver-tag">v0.9.0</span>
          </a>

          <nav className="nav-links">
            <a href="#stage">{copy.nav.stage}</a>
            <a href="#features">{copy.nav.features}</a>
            <a href="#toolkit">{copy.nav.toolkit}</a>
            <a href="#roadmap">{copy.nav.roadmap}</a>
            <HomeLink to="/docs/intro">{copy.nav.docs}</HomeLink>
          </nav>

          <div className="nav-right">
            {/* 10-Language Switcher wired directly to Docusaurus i18n routes */}
            <div
              className={`locale-dropdown${localeOpen ? ' open' : ''}`}
              onClick={(e) => e.stopPropagation()}>
              <button
                className="locale-trigger"
                type="button"
                aria-label={copy.nav.chooseLang}
                aria-expanded={localeOpen}
                onClick={() => {
                  setMobileMenuOpen(false);
                  setLocaleOpen((prev) => !prev);
                }}>
                <svg
                  width="15"
                  height="15"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="2" y1="12" x2="22" y2="12" />
                  <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
                </svg>
                <span>{currentLocaleInfo.label}</span>
                <span style={{fontSize: '0.68rem', opacity: 0.7}}>▾</span>
              </button>

              <div
                ref={localeMenuRef}
                className={`locale-menu${nearScroll ? ' near-scroll' : ''}`}
                onMouseMove={handleLocaleMenuMouseMove}
                onMouseLeave={() => setNearScroll(false)}>
                <div
                  ref={localeGridRef}
                  className="locale-grid"
                  onScroll={syncLocaleScrollThumb}>
                  {LOCALE_LIST.map((item) => {
                    const targetTo = `pathname://${alternatePageUtils.createUrl({
                      locale: item.locale,
                      fullyQualified: false,
                    })}`;
                    const isActive = item.locale === currentLocale;
                    return (
          <Link
                        key={item.locale}
                        to={targetTo}
                        target="_self"
                        autoAddBaseUrl={false}
                        className={`locale-opt${isActive ? ' active' : ''}`}
                        onClick={() => {
                          try {
                            // remember the manual choice so the root-page auto redirect respects it
                            window.localStorage.setItem('gn-locale-pref', item.locale);
                          } catch (e) {
                            /* storage unavailable */
                          }
                          setLocaleOpen(false);
                        }}>
                        <span>{item.label}</span>
                        <small>{item.badge}</small>
          </Link>
                    );
                  })}
                </div>
                {thumbStyle.visible && (
                  <div
                    ref={localeTrackRef}
                    className="locale-scroll-track"
                    aria-hidden="true">
                    <div
                      className={`locale-scroll-thumb${
                        isDraggingThumb ? ' dragging' : ''
                      }`}
                      style={{
                        height: `${thumbStyle.height}px`,
                        transform: `translateY(${thumbStyle.y}px)`,
                      }}
                      onMouseDown={handleThumbMouseDown}
                    />
                  </div>
                )}
              </div>
            </div>

            <a
              className="gn-btn gn-btn-secondary gn-btn-sm nav-desktop-btn"
              href="https://github.com/MaxwellLei/GeoChemistry-Nexus"
              target="_blank"
              rel="noopener noreferrer">
              GitHub
            </a>
            <a
              className="gn-btn gn-btn-primary gn-btn-sm nav-desktop-btn"
              href="https://github.com/MaxwellLei/GeoChemistry-Nexus/releases"
              target="_blank"
              rel="noopener noreferrer">
              {copy.nav.download}
            </a>

            <button
              className="mobile-menu-btn"
              type="button"
              aria-label={copy.nav.openMenu}
              aria-expanded={mobileMenuOpen}
              onClick={(e) => {
                e.stopPropagation();
                setLocaleOpen(false);
                setMobileMenuOpen((prev) => !prev);
              }}>
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.2"
                strokeLinecap="round">
                {mobileMenuOpen ? (
                  <>
                    <line x1="6" y1="6" x2="18" y2="18" />
                    <line x1="18" y1="6" x2="6" y2="18" />
                  </>
                ) : (
                  <>
                    <line x1="4" y1="7" x2="20" y2="7" />
                    <line x1="4" y1="12" x2="20" y2="12" />
                    <line x1="4" y1="17" x2="20" y2="17" />
                  </>
                )}
              </svg>
            </button>
          </div>
        </header>

        {/* Mobile Frosted Navigation Sheet */}
        <div
          className={`mobile-nav-drawer${mobileMenuOpen ? ' open' : ''}`}
          onClick={(e) => e.stopPropagation()}>
          <nav className="mobile-nav-list">
            <a href="#stage" onClick={() => setMobileMenuOpen(false)}>
              <span>{copy.nav.stage}</span>
              <span>→</span>
            </a>
            <a href="#features" onClick={() => setMobileMenuOpen(false)}>
              <span>{copy.nav.features}</span>
              <span>→</span>
            </a>
            <a href="#toolkit" onClick={() => setMobileMenuOpen(false)}>
              <span>{copy.nav.toolkit}</span>
              <span>→</span>
            </a>
            <a href="#roadmap" onClick={() => setMobileMenuOpen(false)}>
              <span>{copy.nav.roadmap}</span>
              <span>→</span>
            </a>
            <HomeLink to="/docs/intro" onClick={() => setMobileMenuOpen(false)}>
              <span>{copy.nav.docsMobile}</span>
              <span>↗</span>
            </HomeLink>
          </nav>
          <div className="mobile-nav-actions">
            <a
              className="gn-btn gn-btn-secondary gn-btn-sm"
              href="https://github.com/MaxwellLei/GeoChemistry-Nexus"
              target="_blank"
              rel="noopener noreferrer">
              {copy.nav.githubMobile}
            </a>
            <a
              className="gn-btn gn-btn-primary gn-btn-sm"
              href="https://github.com/MaxwellLei/GeoChemistry-Nexus/releases"
              target="_blank"
              rel="noopener noreferrer">
              {copy.nav.downloadMobile}
            </a>
          </div>
        </div>
      </div>

      <main id="top">
        {/* Studio Minimal Hero */}
        <section className="hero">
          <div className="gn-container">
            <a
              className="eyebrow-pill"
              href="https://github.com/MaxwellLei/GeoChemistry-Nexus/releases"
              target="_blank"
              rel="noopener noreferrer">
              <b>v0.9.0</b>
              <span>{copy.hero.eyebrow}</span>
            </a>

            <h1>
              {copy.hero.titleLine1}
              <br />
              <span>{copy.hero.titleLine2}</span>
            </h1>

            <p className="hero-lead">{copy.hero.lead}</p>

            <div className="hero-actions">
              <a
                className="gn-btn gn-btn-primary gn-btn-lg"
                href="https://github.com/MaxwellLei/GeoChemistry-Nexus/releases"
                target="_blank"
                rel="noopener noreferrer">
                {copy.hero.primaryBtn}
              </a>
              <HomeLink className="gn-btn gn-btn-secondary gn-btn-lg" to="/docs/intro">
                {copy.hero.secondaryBtn}
          </HomeLink>
            </div>

            {/* Segmented Control + Uncropped Desktop Window */}
            <div id="stage">
              <div className="segmented-wrap" role="tablist">
                {copy.hero.tabs.map((tab, idx) => (
                  <button
                    key={tab.key}
                    type="button"
                    role="tab"
                    aria-selected={idx === activeTabIdx}
                    className={`seg-btn${idx === activeTabIdx ? ' active' : ''}`}
                    onClick={(e) => handleSelectStageTab(idx, e.currentTarget)}>
                    {tab.label}
                  </button>
                ))}
              </div>

              <div className="studio-window">
                <div className="win-chrome">
                  <div className="win-lights">
                    <i />
                    <i />
                    <i />
                  </div>
                  <div className="win-title">{currentTab.winTitle}</div>
                  <div className="win-status-group">
                    <span style={{color: 'var(--ink-400)'}}>
                      {copy.hero.moreModules}
                    </span>
                    <span style={{color: 'var(--teal)'}}>● Cloud Sync Ready</span>
                  </div>
                </div>
                <div className="win-body">
                  <img
                    src={withBase(currentTab.img)}
                    alt={currentTab.winTitle}
                    style={{opacity: winImgFading ? 0.4 : 1}}
                  />
                </div>
                <div className="win-footer">
                  <div className="win-meta-text">
                    <strong>{currentTab.strong}</strong>
                    {currentTab.desc}
                  </div>
                  <HomeLink className="win-doc-link" to={currentTab.docTo}>
                    {copy.hero.tutorialLink}
          </HomeLink>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* 4-Card Quantitative Metrics Strip */}
        <div className="metrics-section">
          <div className="gn-container">
            <div className="metrics-grid">
              {copy.metrics.map((m, idx) => (
                <div className="metric-card" key={idx}>
                  <div className="metric-val">
                    {m.val}
                    {m.suffix ? <span>{m.suffix}</span> : null}
                  </div>
                  <div className="metric-title">{m.title}</div>
                  <div className="metric-lbl">{m.lbl}</div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Headerless Dual-Track 10-Language Geoscience Horizontal Marquee */}
        <div
          className="i18n-ribbon-section"
          id="i18nStream"
          aria-label="Multilingual Geoscience Terminology Stream">
          <div className="i18n-marquee-mask">
            <div className="marquee-track track-left">
              {doubledRow1.map((item, idx) => (
                <button
                  key={`r1-${idx}`}
                  type="button"
                  className="lang-stream-card"
                  onClick={() => handleMarqueeCardClick(item.key)}>
                  <span className="lsc-badge">{item.badge}</span>
                  <span className="lsc-native">{item.native}</span>
                  <span className="lsc-sep" />
                  <span className="lsc-terms">{item.terms}</span>
                </button>
              ))}
            </div>
            <div className="marquee-track track-right">
              {doubledRow2.map((item, idx) => (
                <button
                  key={`r2-${idx}`}
                  type="button"
                  className="lang-stream-card"
                  onClick={() => handleMarqueeCardClick(item.key)}>
                  <span className="lsc-badge">{item.badge}</span>
                  <span className="lsc-native">{item.native}</span>
                  <span className="lsc-sep" />
                  <span className="lsc-terms">{item.terms}</span>
                </button>
              ))}
            </div>
          </div>
        </div>
        
        {/* Bento Highlights */}
        <section className="block block-alt" id="features">
          <div className="gn-container">
            <div className="sec-head">
              <div className="sec-kicker">{copy.features.kicker}</div>
              <h2>{copy.features.title}</h2>
              <p>{copy.features.sub}</p>
            </div>

            <div className="bento">
              {/* Left Card: 10-Language Interactive Le Maitre (2002) IUGS TAS Diagram */}
              <div
                ref={tasCardRef}
                id="tasCard"
                className={`bento-item col-7${tasFlash ? ' flash-highlight' : ''}`}>
                <div>
                  <h3>{copy.features.tasTitle}</h3>
                  <p>
                    {copy.features.tasLeadBefore}
                    <strong>{copy.features.tasLeadBold}</strong>
                    {copy.features.tasLeadAfter}
                  </p>
                </div>

                <div className="tas-widget">
                  <div className="tas-header-row">
                    <span className="tas-caption">{copy.features.tasCaption}</span>
                    <span className="tas-locale-badge">{currentTas.code}</span>
                  </div>

                  <div className="tas-lang-bar" role="group">
                    {TAS_LANG_BUTTONS.map((btn) => (
                      <button
                        key={btn.key}
                        type="button"
                        className={`tas-lang-btn${
                          tasLang === btn.key ? ' active' : ''
                        }`}
                        onClick={(e) => {
                          centerPillInBar(e.currentTarget);
                          setTasLang(btn.key);
                        }}>
                        {btn.label}
                      </button>
                    ))}
                    <span className="tas-more-hint">{copy.features.tasMoreHint}</span>
                  </div>

                  {/* Standard Le Maitre (2002) TAS Diagram: X=[40..80]->[64..544] (12px/wt%), Y=[0..16]->[212..20] (12px/wt%) */}
                  <svg
                    viewBox="0 0 572 260"
                    width="100%"
                    style={{
                      background: '#fff',
                      borderRadius: '12px',
                      border: '1px solid var(--border-hairline)',
                      display: 'block',
                    }}>
                    <g stroke="#f1f5f9" strokeWidth="1">
                      <line x1="124" y1="20" x2="124" y2="212" />
                      <line x1="184" y1="20" x2="184" y2="212" />
                      <line x1="244" y1="20" x2="244" y2="212" />
                      <line x1="304" y1="20" x2="304" y2="212" />
                      <line x1="364" y1="20" x2="364" y2="212" />
                      <line x1="424" y1="20" x2="424" y2="212" />
                      <line x1="484" y1="20" x2="484" y2="212" />
                      <line x1="64" y1="164" x2="544" y2="164" />
                      <line x1="64" y1="116" x2="544" y2="116" />
                      <line x1="64" y1="68" x2="544" y2="68" />
                    </g>

                    <g
                      fill="none"
                      stroke="#334155"
                      strokeWidth="1.2"
                      strokeLinejoin="round"
                      strokeLinecap="round">
                      <polyline points="76,212 76,176 124,176" />
                      <line x1="124" y1="212" x2="124" y2="152" />
                      <line x1="208" y1="212" x2="208" y2="152" />
                      <line x1="268" y1="212" x2="268" y2="141.2" />
                      <line x1="340" y1="212" x2="340" y2="128" />
                      <polyline points="124,152 208,152 268,141.2 340,128 412,116" />
                      <polyline points="508,212 412,116 412,56" />
                      <polyline points="124,152 176.8,124.4 220,100.4 275.2,71.6 340,38" />
                      <line x1="176.8" y1="124.4" x2="208" y2="152" />
                      <line x1="220" y1="100.4" x2="268" y2="141.2" />
                      <line x1="275.2" y1="71.6" x2="340" y2="128" />
                      <polyline points="76,128 124,99.2 164.8,74 214,44 260,20" />
                      <line x1="124" y1="152" x2="124" y2="99.2" />
                      <line x1="176.8" y1="124.4" x2="124" y2="99.2" />
                      <line x1="220" y1="100.4" x2="164.8" y2="74" />
                      <line x1="275.2" y1="71.6" x2="214" y2="44" />
                    </g>

                    <path
                      d="M 108,200 Q 210,145 382,102"
                      fill="none"
                      stroke="#64748b"
                      strokeWidth="1.15"
                      strokeDasharray="5 3.5"
                    />

                    <rect
                      x="64"
                      y="20"
                      width="480"
                      height="192"
                      fill="none"
                      stroke="#0f172a"
                      strokeWidth="1.35"
                    />
                    <g stroke="#0f172a" strokeWidth="1.2">
                      <line x1="64" y1="212" x2="64" y2="217" />
                      <line x1="124" y1="212" x2="124" y2="217" />
                      <line x1="184" y1="212" x2="184" y2="217" />
                      <line x1="244" y1="212" x2="244" y2="217" />
                      <line x1="304" y1="212" x2="304" y2="217" />
                      <line x1="364" y1="212" x2="364" y2="217" />
                      <line x1="424" y1="212" x2="424" y2="217" />
                      <line x1="484" y1="212" x2="484" y2="217" />
                      <line x1="544" y1="212" x2="544" y2="217" />
                      <line x1="59" y1="212" x2="64" y2="212" />
                      <line x1="59" y1="164" x2="64" y2="164" />
                      <line x1="59" y1="116" x2="64" y2="116" />
                      <line x1="59" y1="68" x2="64" y2="68" />
                      <line x1="59" y1="20" x2="64" y2="20" />
                    </g>

                    <g
                      fontFamily="JetBrains Mono, monospace"
                      fontSize="9.5"
                      fill="#475569"
                      textAnchor="middle">
                      <text x="64" y="229">40</text>
                      <text x="124" y="229">45</text>
                      <text x="184" y="229">50</text>
                      <text x="244" y="229">55</text>
                      <text x="304" y="229">60</text>
                      <text x="364" y="229">65</text>
                      <text x="424" y="229">70</text>
                      <text x="484" y="229">75</text>
                      <text x="544" y="229">80</text>
                    </g>
                    <g
                      fontFamily="JetBrains Mono, monospace"
                      fontSize="9.5"
                      fill="#475569"
                      textAnchor="end">
                      <text x="55" y="215">0</text>
                      <text x="55" y="167">4</text>
                      <text x="55" y="119">8</text>
                      <text x="55" y="71">12</text>
                      <text x="55" y="24">16</text>
                    </g>

                    <g stroke="#ffffff" strokeWidth="1.2">
                      <circle cx="152" cy="171" r="4.3" fill="#3645d8" />
                      <circle cx="182" cy="166" r="4.3" fill="#3645d8" />
                      <circle cx="228" cy="162" r="4.3" fill="#0d9488" />
                      <circle cx="249" cy="155" r="4.3" fill="#0d9488" />
                      <circle cx="290" cy="153" r="4.3" fill="#0d9488" />
                      <circle cx="318" cy="147" r="4.3" fill="#d97706" />
                      <circle cx="372" cy="143" r="4.3" fill="#d97706" />
                      <circle cx="456" cy="94" r="4.3" fill="#e11d48" />
                      <circle cx="494" cy="86" r="4.3" fill="#e11d48" />
                    </g>

                    <rect
                      x="422"
                      y="28"
                      width="112"
                      height="22"
                      rx="6"
                      fill="rgba(248,250,252,0.92)"
                      stroke="#e2e8f0"
                      strokeWidth="1"
                    />
                    <circle cx="434" cy="39" r="3.6" fill="#3645d8" />
                    <text x="444" y="42.5" fontSize="9.5" fontWeight="600" fill="#334155">
                      {currentTas.lleg}
                    </text>

                    <text x="166" y="194" fontSize="10.5" fontWeight="600" fill="#1e293b" textAnchor="middle">
                      {currentTas.l1}
                    </text>
                    <text x="238" y="194" fontFamily="JetBrains Mono, monospace" fontSize="9" fill="#94a3b8" textAnchor="middle">
                      O₁
                    </text>
                    <text x="304" y="186" fontSize="10.5" fontWeight="600" fill="#1e293b" textAnchor="middle">
                      {currentTas.l2}
                    </text>
                    <text x="392" y="184" fontSize="10.5" fontWeight="600" fill="#1e293b" textAnchor="middle">
                      {currentTas.l3}
                    </text>
                    <text x="482" y="132" fontSize="10.5" fontWeight="600" fill="#1e293b" textAnchor="middle">
                      {currentTas.l4}
                    </text>
                    <text x="356" y="88" fontSize="10.5" fontWeight="600" fill="#334155" textAnchor="middle">
                      {currentTas.l5}
                    </text>
                    <text x="286" y="45" fontSize="9.8" fontWeight="600" fill="#475569" textAnchor="middle">
                      {currentTas.l6}
                    </text>

                    <text x="304" y="251" fontSize="11" fontWeight="700" fill="#0f172a" textAnchor="middle">
                      {currentTas.lx}
                    </text>
                    <text
                      x="19"
                      y="116"
                      fontSize="10.5"
                      fontWeight="700"
                      fill="#0f172a"
                      textAnchor="middle"
                      transform="rotate(-90 19 116)">
                      {currentTas.ly}
                    </text>
                  </svg>
                </div>
              </div>

              {/* Right Card: Publication-Grade SVG & Third-Party Vector Software Bridge */}
              <div className="bento-item col-5">
                <div>
                  <h3>{copy.features.svgTitle}</h3>
                  <p>{copy.features.svgDesc}</p>
                </div>

                <div className="vector-bridge-hub">
                  <div className="vbh-source">
                    <div className="app-squircle" style={{background: '#3645D8'}}>
                      <svg width="26" height="26" viewBox="0 0 196 196" aria-hidden="true">
                        <g strokeLinejoin="round" strokeWidth="6">
                          <polygon points="70,38 137,38 128,93 53,144 19,132 20,98" fill="#FFFFFF" stroke="#FFFFFF" />
                          <polygon points="152,37 175,105 160,120 141,94" fill="#B8C2F2" stroke="#B8C2F2" />
                          <polygon points="131,105 148,127 106,159 69,148" fill="#D9DFFF" stroke="#D9DFFF" />
                        </g>
                      </svg>
                    </div>
                    <span>Nexus</span>
                  </div>

                  {/* Desktop Horizontal Connector */}
                  <div className="vbh-arrow" aria-hidden="true">
                    <span className="vbh-arrow-tag">{copy.features.svgBridgeTag}</span>
                    <svg width="64" height="14" viewBox="0 0 64 14" fill="none">
                      <line x1="2" y1="7" x2="55" y2="7" stroke="#3645D8" strokeWidth="1.6" strokeDasharray="4 3" />
                      <polyline points="50,2.5 57,7 50,11.5" stroke="#3645D8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </div>

                  {/* Mobile 1-to-3 Symmetric SVG Branch Connector */}
                  <div className="vbh-mobile-branch" aria-hidden="true">
                    <svg viewBox="0 0 264 62" preserveAspectRatio="none" fill="none">
                      <line x1="132" y1="2" x2="132" y2="55" stroke="#3645D8" strokeWidth="1.6" strokeDasharray="3.5 3" />
                      <path d="M 44 55 L 44 47 Q 44 42 49 42 L 215 42 Q 220 42 220 47 L 220 55" stroke="#3645D8" strokeWidth="1.6" strokeDasharray="3.5 3" />
                      <polyline points="40.2,51 44,56.2 47.8,51" stroke="#3645D8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                      <polyline points="128.2,51 132,56.2 135.8,51" stroke="#3645D8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                      <polyline points="216.2,51 220,56.2 223.8,51" stroke="#3645D8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                    <span className="vbh-arrow-tag">{copy.features.svgBridgeTag}</span>
                  </div>

                  <div className="vbh-targets">
                    <div className="vbh-app">
                      <div className="app-squircle" style={{background: '#18181B'}}>
                        <svg width="25" height="25" viewBox="0 0 32 32" fill="none" aria-hidden="true">
                          <path d="M16 4L27 15.5C28.2 16.7 28 18.6 26.5 19.6L21.5 22.8L24 25.5C24.5 26.2 23.8 27.2 22.8 27L16 25.2L9.2 27C8.2 27.2 7.5 26.2 8 25.5L10.5 22.8L5.5 19.6C4 18.6 3.8 16.7 5 15.5L16 4Z" fill="#FFFFFF" />
                          <path d="M16 6.8L11.2 12H14.5L16 10.2L17.5 12H20.8L16 6.8Z" fill="#18181B" />
                        </svg>
                      </div>
                      <span>Inkscape</span>
                    </div>

                    <div className="vbh-app">
                      <div
                        className="app-squircle"
                        style={{background: 'linear-gradient(145deg,#34B556,#1E873B)'}}>
                        <svg width="24" height="24" viewBox="0 0 32 32" fill="none" aria-hidden="true">
                          <path d="M16 4C10.5 4 6.5 8.4 6.5 13.8C6.5 19.2 12.2 24.4 16 28C19.8 24.4 25.5 19.2 25.5 13.8C25.5 8.4 21.5 4 16 4Z" fill="#FFFFFF" />
                          <path d="M16 7C13.6 9.8 12.6 13.6 14.4 19.8L16 24.5L17.6 19.8C19.4 13.6 18.4 9.8 16 7Z" fill="#239942" />
                        </svg>
                      </div>
                      <span>CorelDRAW</span>
                    </div>

                    <div className="vbh-app">
                      <div
                        className="app-squircle"
                        style={{background: '#300000', border: '1.5px solid #FF9A00'}}>
                        <span
                          style={{
                            fontFamily: 'Inter, sans-serif',
                            fontWeight: 800,
                            fontSize: '0.95rem',
                            color: '#FF9A00',
                            letterSpacing: '-0.03em',
                          }}>
                          Ai
                        </span>
                      </div>
                      <span>Illustrator</span>
                    </div>
                  </div>
                </div>

                <div className="vector-stack">
                  {copy.features.svgRows.map((row, idx) => (
                    <div className="vector-row" key={idx}>
                      <span className="vr-left">
                        <i
                          className="vr-mini-icon"
                          style={{
                            background: row.bg,
                            color: row.fg,
                            border: row.border ? `1px solid ${row.border}` : undefined,
                          }}>
                          {row.icon}
                        </i>
                        {row.name}
                      </span>
                      <span>{row.tag}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Bottom 3 Bento Cards */}
              <div className="bento-item col-4">
                <div>
                  <h3>{copy.features.card3Title}</h3>
                  <p>
                    {copy.features.card3Before}
                    <code>apfu</code>
                    {copy.features.card3After}
                  </p>
                </div>
                <div className="mini-list">
                  <div className="mini-item">
                    <span>● Granite-01</span>
                    <span style={{color: 'var(--teal)'}}>ΔMass 0.0000%</span>
                  </div>
                  <div className="mini-item">
                    <span>● Bt-Sample02</span>
                    <span style={{color: 'var(--brand)'}}>T = 516.6 °C</span>
                  </div>
                </div>
              </div>

              <div className="bento-item col-4">
                <div>
                  <h3>{copy.features.card4Title}</h3>
                  <p>
                    {copy.features.card4Before}
                    <code>=FuncName()</code>
                    {copy.features.card4After}
                  </p>
                </div>
                <div className="mini-list">
                  <div className="mini-item">
                    <span>=TiInBiotite(B2:M2)</span>
                    <span>516.6 °C</span>
                  </div>
                  <div className="mini-item">
                    <span>=ZrSat_Watson(N2:U2)</span>
                    <span>784.2 °C</span>
                  </div>
                </div>
              </div>

              <div className="bento-item col-4">
                <div>
                  <h3>{copy.features.card5Title}</h3>
                  <p>{copy.features.card5Desc}</p>
                </div>
                <div className="mini-list">
                  <div className="mini-item">
                    <span>{copy.features.card5Row1Left}</span>
                    <span style={{color: 'var(--teal)'}}>{copy.features.card5Row1Right}</span>
                  </div>
                  <div className="mini-item">
                    <span>{copy.features.card5Row2Left}</span>
                    <span>{copy.features.card5Row2Right}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Geoscience Portal & Micro-Tools */}
        <section className="block" id="toolkit">
          <div className="gn-container">
            <div className="sec-head">
              <div className="sec-kicker">{copy.toolkit.kicker}</div>
              <h2>{copy.toolkit.title}</h2>
              <p>{copy.toolkit.sub}</p>
            </div>

            <div className="util-tabs-bar">
              {copy.toolkit.items.map((item, idx) => (
                <button
                  key={item.key}
                  type="button"
                  className={`util-pill${idx === activeUtilIdx ? ' active' : ''}`}
                  onClick={(e) => {
                    centerPillInBar(e.currentTarget);
                    setActiveUtilIdx(idx);
                  }}>
                  {item.pill}
                </button>
              ))}
            </div>

            <div className="util-showcase">
              <div className="util-img-box">
                <img src={withBase(currentUtil.img)} alt={currentUtil.title} />
              </div>
              <div>
                <div className="sec-kicker" style={{marginBottom: '6px'}}>
                  {copy.toolkit.widgetKicker}
                </div>
                <h3 style={{fontSize: '1.5rem', marginBottom: '12px'}}>
                  {currentUtil.title}
                </h3>
                <p style={{color: 'var(--ink-500)', marginBottom: '14px'}}>
                  {currentUtil.desc}
                </p>
                <p
                  style={{
                    fontSize: '0.84rem',
                    color: 'var(--ink-400)',
                    marginBottom: '22px',
                  }}>
                  {copy.toolkit.note}
                </p>
                <HomeLink className="gn-btn gn-btn-secondary gn-btn-sm" to="/docs/Tutodials/Home">
                  {copy.toolkit.docBtn}
                </HomeLink>
              </div>
            </div>
          </div>
        </section>

        {/* Vision & Long-Term Roadmap */}
        <section className="block block-alt" id="roadmap">
          <div className="gn-container">
            <div className="sec-head">
              <div className="sec-kicker">{copy.roadmap.kicker}</div>
              <h2>{copy.roadmap.title}</h2>
              <p>
                {copy.roadmap.subBefore}
                <strong>{copy.roadmap.subBold}</strong>
                {copy.roadmap.subAfter}
              </p>
            </div>

            <div className="roadmap-cols">
              {copy.roadmap.stages.map((st, idx) => (
                <div className="rm-card" key={idx}>
                  <div>
                    <span className={`rm-badge ${st.tone}`}>{st.badge}</span>
                    <h3>{st.title}</h3>
                    <ul>
                      {st.items.map((li, i) => (
                        <li key={i}>{li}</li>
                      ))}
                    </ul>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Light CTA Style D: Borderless Airy Minimalist Finale */}
        <section className="block" id="ctaSection">
          <div className="gn-container">
            <div className="cta-light-d">
              <div className="sec-kicker" style={{marginBottom: '10px'}}>
                {copy.cta.kicker}
              </div>
              <h2>{copy.cta.title}</h2>
              <p>{copy.cta.desc}</p>
              <div className="cta-d-actions">
                <a
                  className="gn-btn gn-btn-primary gn-btn-lg"
                  href="https://github.com/MaxwellLei/GeoChemistry-Nexus/releases"
                  target="_blank"
                  rel="noopener noreferrer">
                  {copy.cta.primaryBtn}
                </a>
                <HomeLink
                  className="gn-btn gn-btn-secondary gn-btn-lg"
                  to="/docs/Invitation Letter">
                  {copy.cta.secondaryBtn}
                </HomeLink>
              </div>
              <div className="contact-pills-light">
                <button
                  type="button"
                  className="c-pill-light"
                  onClick={() => handleCopyText('qq', '1076647740')}>
                  {copiedKey === 'qq' ? copy.cta.copiedText : copy.cta.qqPill}
                </button>
                <button
                  type="button"
                  className="c-pill-light"
                  onClick={() => handleCopyText('email', 'maxwelllei@qq.com')}>
                  {copiedKey === 'email' ? copy.cta.copiedText : copy.cta.emailPill}
                </button>
                <a
                  className="c-pill-light"
                  href="https://discord.gg/mRm8dbwa4W"
                  target="_blank"
                  rel="noopener noreferrer">
                  {copy.cta.discordPill}
                </a>
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer className="gn-footer">
        <div className="gn-container">
          <div className="footer-grid">
            <div className="footer-brand-col">
              <div className="brand" style={{marginBottom: '10px'}}>
                <span>GeoChemistry Nexus</span>
              </div>
              <p style={{maxWidth: '340px'}}>{copy.footer.tagline}</p>
            </div>

            <div>
              <h4>{copy.footer.col1Title}</h4>
              <ul>
                {copy.footer.col1.map((item, idx) => (
                  <li key={idx}>
                    <HomeLink to={item.to}>{item.label}</HomeLink>
                  </li>
                ))}
              </ul>
            </div>

            <div>
              <h4>{copy.footer.col2Title}</h4>
              <ul>
                {copy.footer.col2.map((item, idx) => (
                  <li key={idx}>
                    <HomeLink to={item.to}>{item.label}</HomeLink>
                  </li>
                ))}
              </ul>
            </div>

            <div>
              <h4>{copy.footer.col3Title}</h4>
              <ul>
                {copy.footer.col3.map((item, idx) => (
                  <li key={idx}>
                    {item.href ? (
                      <a
                        href={item.href}
                        target="_blank"
                        rel="noopener noreferrer">
                        {item.label}
                      </a>
                    ) : (
                      <HomeLink to={item.to}>{item.label}</HomeLink>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          </div>

          <div className="footer-bottom-bar">
            <span>Copyright © 2024–2026 GeoChemistry Nexus</span>
            <span>GPL-3.0 Open Source</span>
          </div>
        </div>
      </footer>
      </div>
  );
}

export default function Home() {
  const copy = HOME_COPY;

  return (
    <LayoutProvider>
      <PageMetadata title={copy.metaTitle} description={copy.metaDesc} />
      <ErrorBoundary fallback={(params) => <ErrorPageContent {...params} />}>
        <StudioHomepageContent />
      </ErrorBoundary>
    </LayoutProvider>
  );
}
