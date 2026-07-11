/**
 * kit.tsx — the shared "ops console" design system for the approaches sub-site.
 *
 * Every approaches page (the hub and each mutation) is composed from these components,
 * so the pages stay visually consistent while each picks its own accent. Pages should
 * import ONLY from here (plus React) and use Tailwind utility classes for one-off tweaks.
 *
 * Accent: pass `accent` to <Page>; it sets the CSS var --accent for the whole page, and
 * every component below reads var(--accent). Named accents: violet, amber, rose, cyan,
 * emerald — or pass any hex string.
 */
import React, { useState } from "react";

export type AccentName = "violet" | "amber" | "rose" | "cyan" | "emerald";

const ACCENTS: Record<AccentName, string> = {
  violet: "#a78bfa",
  amber: "#f59e0b",
  rose: "#fb7185",
  cyan: "#22d3ee",
  emerald: "#34d399",
};

export const accentHex = (a?: string): string =>
  (a && a in ACCENTS ? ACCENTS[a as AccentName] : a) ?? ACCENTS.violet;

export const cx = (...parts: Array<string | false | null | undefined>): string =>
  parts.filter(Boolean).join(" ");

/* ------------------------------------------------------------------ Page shell */

export interface PageProps {
  accent?: AccentName | string;
  /** Eyebrow line above the title, e.g. "HAND lineage · mutation M1". */
  kicker?: string;
  /** Small monospace chip beside the kicker, e.g. "M1". */
  mnemonic?: string;
  title: string;
  /** One-line essence, rendered italic under the title. */
  lede?: React.ReactNode;
  /** The seed/spark — rendered as a highlighted pull-quote in the hero. */
  spark?: React.ReactNode;
  backHref: string;
  backLabel: string;
  /**
   * Breadcrumb trail, rooted at the portfolio. Each crumb is a label + optional
   * href; the last crumb (the current page) omits href. Rendered as the shared
   * site-wide breadcrumb — mirrors the `.crumbs` component used by the Markdown
   * and self-contained HTML musings so navigation is coherent across the site.
   */
  crumbs?: Crumb[];
  children: React.ReactNode;
}

export interface Crumb {
  label: string;
  href?: string;
}

export function Page({
  accent,
  kicker,
  mnemonic,
  title,
  lede,
  spark,
  backHref,
  backLabel,
  crumbs,
  children,
}: PageProps) {
  const style = { ["--accent" as string]: accentHex(accent) } as React.CSSProperties;
  return (
    <div style={style} className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-[var(--color-line)] bg-[var(--color-ink-900)]">
        <div className="mx-auto max-w-3xl px-6 py-2 text-sm">
          {crumbs && crumbs.length > 0 ? (
            <nav aria-label="Breadcrumb">
              <ol className="flex flex-wrap items-center">
                {crumbs.map((c, i) => (
                  <li key={i} className="flex items-center">
                    {i > 0 && (
                      <span aria-hidden className="mx-0.5 text-fog-500">
                        &rsaquo;
                      </span>
                    )}
                    {c.href ? (
                      <a
                        href={c.href}
                        className="inline-flex min-h-[34px] items-center rounded px-1.5 font-medium text-fog-300 transition-colors hover:text-[var(--accent)]"
                      >
                        {c.label}
                      </a>
                    ) : (
                      <span
                        aria-current="page"
                        className="inline-flex min-h-[34px] items-center px-1.5 text-fog-500"
                      >
                        {c.label}
                      </span>
                    )}
                  </li>
                ))}
              </ol>
            </nav>
          ) : (
            <a
              href={backHref}
              className="inline-flex items-center gap-1.5 py-1 font-medium text-fog-300 transition-colors hover:text-[var(--accent)]"
            >
              <span aria-hidden>←</span>
              {backLabel}
            </a>
          )}
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-6 pb-28">
        <div className="pt-12 sm:pt-16">
          {(kicker || mnemonic) && (
            <div className="mb-4 flex items-center gap-3">
              {mnemonic && (
                <span className="rounded bg-[var(--accent-soft)] px-2 py-0.5 font-mono text-xs font-semibold tracking-wide text-[var(--accent)] ring-1 ring-[var(--accent)]/40">
                  {mnemonic}
                </span>
              )}
              {kicker && (
                <span className="font-mono text-xs uppercase tracking-[0.18em] text-fog-500">
                  {kicker}
                </span>
              )}
            </div>
          )}
          <h1 className="text-balance text-4xl font-bold leading-[1.05] tracking-tight sm:text-5xl">
            {title}
          </h1>
          {lede && (
            <p className="mt-5 text-lg italic leading-relaxed text-fog-300">{lede}</p>
          )}
          {spark && (
            <blockquote className="mt-8 border-l-2 border-[var(--accent)] bg-[var(--accent-soft)] py-3 pl-5 pr-4 text-base leading-relaxed text-fog-100">
              {spark}
            </blockquote>
          )}
        </div>

        <div className="mt-12 space-y-12">{children}</div>
      </main>

      <footer className="border-t border-[var(--color-line)]">
        <div className="mx-auto max-w-3xl px-6 py-8 text-sm text-fog-500">
          A <em>Minimalist Space Logistics</em> design exploration ·{" "}
          <a href={backHref} className="text-fog-400 hover:text-[var(--accent)]">
            {backLabel}
          </a>
        </div>
      </footer>
    </div>
  );
}

/* ------------------------------------------------------------------ Sections */

export interface SectionProps {
  id?: string;
  /** Small uppercase eyebrow above the heading. */
  eyebrow?: string;
  title: string;
  children: React.ReactNode;
}

export function Section({ id, eyebrow, title, children }: SectionProps) {
  return (
    <section id={id} className="scroll-mt-20">
      {eyebrow && (
        <div className="mb-2 font-mono text-xs uppercase tracking-[0.18em] text-[var(--accent)]">
          {eyebrow}
        </div>
      )}
      <h2 className="text-2xl font-semibold tracking-tight text-fog-100">{title}</h2>
      <div className="mt-5 space-y-4 leading-relaxed text-fog-300">{children}</div>
    </section>
  );
}

export function Subheading({ children }: { children: React.ReactNode }) {
  return (
    <h3 className="mt-8 text-lg font-semibold text-fog-100">{children}</h3>
  );
}

export function Lead({ children }: { children: React.ReactNode }) {
  return <p className="text-lg leading-relaxed text-fog-200">{children}</p>;
}

/* ------------------------------------------------------------------ Handles + decisions */

/** A referenceable handle chip, e.g. <Handle id="M1.3" />. */
export function Handle({ id }: { id: string }) {
  return (
    <span className="inline-block rounded bg-[var(--accent-soft)] px-1.5 py-0.5 font-mono text-[0.8em] font-semibold text-[var(--accent)]">
      {id}
    </span>
  );
}

export interface DecisionItem {
  handle: string;
  title: React.ReactNode;
  body: React.ReactNode;
}

/** An ordered list of load-bearing decisions, each with a stable handle chip. */
export function DecisionList({ items }: { items: DecisionItem[] }) {
  return (
    <ol className="space-y-4">
      {items.map((it) => (
        <li
          key={it.handle}
          className="rounded-lg border border-[var(--color-line)] bg-[var(--color-ink-850)]/60 p-4"
        >
          <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
            <Handle id={it.handle} />
            <span className="font-semibold text-fog-100">{it.title}</span>
          </div>
          <div className="mt-2 leading-relaxed text-fog-300">{it.body}</div>
        </li>
      ))}
    </ol>
  );
}

/* ------------------------------------------------------------------ Callouts */

type CalloutKind = "note" | "key" | "warn";

const CALLOUT_LABEL: Record<CalloutKind, string> = {
  note: "Note",
  key: "Key",
  warn: "Risk",
};

export function Callout({
  kind = "note",
  title,
  children,
}: {
  kind?: CalloutKind;
  title?: React.ReactNode;
  children: React.ReactNode;
}) {
  const tone =
    kind === "warn"
      ? "border-rose-400/50 bg-rose-500/5"
      : kind === "key"
        ? "border-[var(--accent)]/50 bg-[var(--accent-soft)]"
        : "border-[var(--color-line)] bg-[var(--color-ink-850)]/60";
  return (
    <div className={cx("rounded-lg border p-4", tone)}>
      <div className="mb-1 font-mono text-xs uppercase tracking-[0.16em] text-fog-400">
        {title ?? CALLOUT_LABEL[kind]}
      </div>
      <div className="leading-relaxed text-fog-200">{children}</div>
    </div>
  );
}

/* ------------------------------------------------------------------ Stats */

export interface StatItem {
  value: React.ReactNode;
  label: React.ReactNode;
}

export function StatGrid({ items }: { items: StatItem[] }) {
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {items.map((s, i) => (
        <div
          key={i}
          className="rounded-lg border border-[var(--color-line)] bg-[var(--color-ink-850)]/60 p-4"
        >
          <div className="font-mono text-2xl font-bold text-[var(--accent)]">{s.value}</div>
          <div className="mt-1 text-xs uppercase tracking-wide text-fog-500">{s.label}</div>
        </div>
      ))}
    </div>
  );
}

/* ------------------------------------------------------------------ Figure / diagram */

export function Figure({
  children,
  caption,
}: {
  children: React.ReactNode;
  caption?: React.ReactNode;
}) {
  return (
    <figure className="overflow-hidden rounded-xl border border-[var(--color-line)] bg-[var(--color-ink-900)]">
      <div className="p-4 sm:p-6">{children}</div>
      {caption && (
        <figcaption className="border-t border-[var(--color-line)] px-4 py-2 text-center text-xs text-fog-500">
          {caption}
        </figcaption>
      )}
    </figure>
  );
}

/* ------------------------------------------------------------------ Compare table */

export interface CompareTableProps {
  columns: React.ReactNode[];
  rows: Array<{ label: React.ReactNode; cells: React.ReactNode[] }>;
  /** Highlight one data column (0-based over `columns` minus the row-label col). */
  highlight?: number;
}

export function CompareTable({ columns, rows, highlight }: CompareTableProps) {
  return (
    <div className="overflow-x-auto rounded-xl border border-[var(--color-line)]">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="bg-[var(--color-ink-850)]">
            <th className="px-3 py-2.5 text-left font-semibold text-fog-400"> </th>
            {columns.map((c, i) => (
              <th
                key={i}
                className={cx(
                  "px-3 py-2.5 text-left font-semibold",
                  highlight === i ? "text-[var(--accent)]" : "text-fog-200",
                )}
              >
                {c}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, ri) => (
            <tr key={ri} className="border-t border-[var(--color-line)]">
              <th className="whitespace-nowrap px-3 py-2.5 text-left font-medium text-fog-400">
                {row.label}
              </th>
              {row.cells.map((cell, ci) => (
                <td
                  key={ci}
                  className={cx(
                    "px-3 py-2.5 align-top text-fog-300",
                    highlight === ci && "bg-[var(--accent-soft)] text-fog-100",
                  )}
                >
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/* ------------------------------------------------------------------ Tags */

export function Tag({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-[var(--color-line)] bg-[var(--color-ink-850)] px-2.5 py-1 font-mono text-xs text-fog-300">
      {children}
    </span>
  );
}

export function TagRow({ children }: { children: React.ReactNode }) {
  return <div className="flex flex-wrap gap-2">{children}</div>;
}

/* ------------------------------------------------------------------ Tabs (client) */

export interface TabSpec {
  id: string;
  label: React.ReactNode;
  content: React.ReactNode;
}

export function Tabs({ tabs }: { tabs: TabSpec[] }) {
  const [active, setActive] = useState(tabs[0]?.id);
  const current = tabs.find((t) => t.id === active) ?? tabs[0];
  return (
    <div>
      <div className="flex flex-wrap gap-1 border-b border-[var(--color-line)]">
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setActive(t.id)}
            className={cx(
              "-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors",
              t.id === current?.id
                ? "border-[var(--accent)] text-fog-100"
                : "border-transparent text-fog-500 hover:text-fog-300",
            )}
          >
            {t.label}
          </button>
        ))}
      </div>
      <div className="pt-4 leading-relaxed text-fog-300">{current?.content}</div>
    </div>
  );
}

/* ------------------------------------------------------------------ Cards (hub) */

export interface CardProps {
  href?: string;
  mnemonic?: string;
  title: React.ReactNode;
  children: React.ReactNode;
  accent?: AccentName | string;
  /** Footer line, e.g. the headline tech. */
  meta?: React.ReactNode;
}

export function Card({ href, mnemonic, title, children, accent, meta }: CardProps) {
  const style = accent
    ? ({ ["--accent" as string]: accentHex(accent) } as React.CSSProperties)
    : undefined;
  const inner = (
    <>
      <div className="flex items-center gap-2">
        {mnemonic && (
          <span className="rounded bg-[var(--accent-soft)] px-1.5 py-0.5 font-mono text-xs font-semibold text-[var(--accent)]">
            {mnemonic}
          </span>
        )}
        <h3 className="text-lg font-semibold text-fog-100 group-hover:text-[var(--accent)]">
          {title}
        </h3>
      </div>
      <p className="mt-2 text-sm leading-relaxed text-fog-400">{children}</p>
      {meta && (
        <div className="mt-3 border-t border-[var(--color-line)] pt-3 font-mono text-xs text-fog-500">
          {meta}
        </div>
      )}
    </>
  );
  const cls =
    "group block h-full rounded-xl border border-[var(--color-line)] bg-[var(--color-ink-850)]/60 p-5 transition-colors hover:border-[var(--accent)]/60";
  return href ? (
    <a href={href} style={style} className={cls}>
      {inner}
    </a>
  ) : (
    <div style={style} className={cls}>
      {inner}
    </div>
  );
}

export function CardGrid({ children }: { children: React.ReactNode }) {
  return <div className="grid gap-4 sm:grid-cols-2">{children}</div>;
}
