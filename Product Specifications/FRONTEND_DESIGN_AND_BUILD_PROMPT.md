# Trinetra Frontend: Design System and Build Prompt (React)

You are the frontend lead for **Trinetra**, an autonomous IT incident response platform. The backend is ASP.NET Core 8 with Semantic Kernel agents, PostgreSQL with pgvector, and Redis. Your job is the React frontend. First remove the existing Angular template. Then build the design system and the application described here.

Read this whole file before writing code. Where it conflicts with `Product Specifications/` (docs 01 to 09), this file wins for frontend decisions. Log every conflict in HANDOFF.md.

---

## 0. Working rules

These rules come from the owner's setup prompt and still apply.

- Work in phases F0 to F7 (section 12). Keep the plan and task status in HANDOFF.md. Do not ask me to approve the plan. Start immediately.
- At the end of each phase, stop, post a phase report (what was built, how to run it, test and axe and Lighthouse results, deviations, next phase), then wait for "continue".
- Ask me only when a decision changes product scope or cannot be reversed. Otherwise choose the safest default, record it in HANDOFF.md, and keep going.
- Checkpoint after every task: update HANDOFF.md (CURRENT STATE, task status, DECISIONS, SPEC ISSUES, DEVICE PENDING WORK, SESSION LOG), commit code and HANDOFF.md together, and push.
- I work on two laptops. The office laptop has restricted installs and may lack Docker or Playwright browser binaries. If a step cannot run on the current device, record it under DEVICE PENDING WORK with the exact command and the device it needs. Do not stall the phase.
- Never write secrets into the repo. `.env.example` holds placeholders. `.env` stays gitignored.
- If `Product Specifications/design/` contains reference screenshots or style files, read them before F1 and reconcile them with this brief. This brief takes precedence. Log any differences.

---

## 1. Mission and scope

Build a production-quality React application with two surfaces on one design system:

1. **Public landing page at `/`.** It explains Trinetra and shows the agent workflow. This is where the expressive motion lives.
2. **Authenticated operations console.** It contains the five screens in doc 09 plus Runbooks (read-only) and a Settings stub. It must be dense, fast, keyboard-first, and built for engineers on call.

Motion in the console only communicates state. The landing page can be expressive. The console must stay calm and legible at 3 a.m.

---

## 2. Phase F0 first: remove Angular

1. Locate the Angular project. Check `src/`, `frontend/`, and `src/frontend/`. Look for `angular.json` and `@angular/core` in `package.json`.
2. Record in HANDOFF.md what it contains and confirm it is only a template. Then delete it in one commit: `frontend: remove Angular template`.
3. Do not modify `Product Specifications/`, `HANDOFF.md`, the backend projects, or `docker-compose.yml`. The one exception is adding the frontend service entry in F0.
4. Scaffold the React app in `src/frontend` with Vite, React 19, and TypeScript in strict mode. Commit: `frontend: scaffold react + vite + typescript`.

---

## 3. Stack (decided; log as DECISIONS)

| Concern | Choice |
|---|---|
| Framework | React 19, TypeScript strict, Vite |
| Styling | Tailwind CSS v4 with `@theme`. Every value comes from CSS custom-property tokens. |
| Accessible primitives | react-aria-components. Our components wrap these so there is one accessibility layer. |
| Component pattern | shadcn/ui style: source copied into `src/components/ui/`, owned by the repo, variants via cva. No runtime shadcn dependency. |
| Animation | Motion (`motion/react`) for UI and landing. No GSAP unless a specific landing effect needs it, and the cost is recorded. |
| Icons | lucide-react only, 1.5px stroke |
| Routing | React Router v7 (data router) |
| Server state | TanStack Query |
| UI state | Zustand |
| Forms | React Hook Form + Zod |
| Toasts | Sonner |
| Charts | Recharts, styled with tokens |
| Real-time | @microsoft/signalr (the backend is ASP.NET Core, so SignalR, not Socket.io) |
| i18n | react-i18next. All strings extracted. English only at launch. |
| Mocks | MSW in development and tests |
| Tests | Vitest + Testing Library (unit). Playwright on chromium, firefox, and webkit (e2e), with @axe-core/playwright. |
| Design docs | Storybook (React + Vite framework) with addon-a11y |
| Lint | ESLint flat config, eslint-plugin-jsx-a11y, Prettier. Custom rules reject raw hex colors, raw px radii, raw z-index numbers, and hardcoded user-facing strings outside token files and i18n resources. |

Do not add other UI kits (MUI, Bootstrap, Chakra, Ant Design) or state libraries (Redux, NgRx).

---

## 4. Design direction

Each design category has an assigned reference. Apply the rule in the table. Where the reference is a product name, use that product's publicly known approach as the reference. Do not copy any site's assets, logos, or copy.

### 4.1 Visual foundation

| Category | Reference | Rule |
|---|---|---|
| Theme | Linear | Dark-first. Calm, precise, dense in the console. Off-black canvas, hairline borders, one accent. |
| Color palette | Stripe | Indigo-violet accent family plus cool navy-black neutrals (section 5). Status colors are separate and never decorative. |
| Light and dark mode | Vercel | Both are first-class. Dark is the default. Follow the system setting on first visit, then persist the user's choice. Every text and UI token pair must pass contrast in both themes. |
| Color gradients | Stripe | Only on the landing hero, feature highlights, and the logo. Never on text, buttons, or table cells. |
| Typography | Apple | Hierarchy comes from size, not weight. Tight tracking at display sizes. Weights 400, 500, and 600 only. Optical sizing on. Apple's own fonts are not available on the web here, so Inter stands in with the same scale principles. |
| Font pairing | Notion | Inter for interface and body text. Instrument Serif (italic) for landing display headlines and empty-state headings only. Geist Mono for IDs, commands, log lines, and timestamps. |
| Spacing system | Linear | 4px base scale (section 5). Components use scale tokens only. |
| Layout design | Apple | Landing: centered 12-column grid, max 1200px, generous section rhythm. Console: fixed sidebar, topbar, fluid content up to 1440px. |
| Visual hierarchy | Stripe | One focal point per screen. At most one primary button per view. Secondary information uses size and surface steps, not extra decoration. |
| Alignment | Apple | Strong left edges. Optically align icons with adjacent text. Right-align numbers in tables. |
| Whitespace | Notion | Generous in prose and on the landing page. Compact in tables, lists, and timelines, where density helps scanning. |
| Border radius | Linear | 4px for tags and small chips, 6px for controls, 8px for inputs, 12px for cards, 16px for landing feature cards. 9999px only for badges, avatars, and toggles. No other radii. |
| Shadows and elevation | Vercel | Elevation comes from surface color and 1px borders. Shadows appear only on floating layers: menus, popovers, dialogs, and toasts. |
| Glassmorphism | Apple | Only on floating layers over content: landing nav, command palette, popovers. Use a solid fallback where backdrop-filter is unsupported. Never on dense data panels. |
| Neumorphism | none | Do not use. |
| Background effects | Framer | Landing only: a faint dot grid and grain drawn with CSS. Static under reduced motion. No WebGL. |
| Mesh gradients | Stripe | Landing hero only: three or four blurred radial layers in accent colors. Use a static fallback under reduced motion. |

### 4.2 Interaction and motion

| Category | Reference | Rule |
|---|---|---|
| Hover effects | Linear | Background or border tint in 120ms. No movement on console controls. The magnetic effect is for the landing primary CTA only (section 8). |
| Focus states | GitHub | 2px focus ring with 2px offset, shown with `:focus-visible`. Contrast at least 3:1 on every background. Never remove an outline without a replacement. |
| Active states | Linear | Pressed state is one surface step darker. No scale change on table rows or list items. |
| Button interactions | Stripe | Hover: tint. Press: scale 0.98 for 80ms, landing CTAs only. Loading: spinner replaces the label and the width is kept. |
| Microinteractions | Linear | Success checkmark morph on approve. One-time highlight when a new critical incident arrives. Counters tween when values change. All of these become static state changes under reduced motion. |
| Transitions | Linear | Color and opacity 120ms. Layout and panel changes 200ms. Dialogs and drawers 240ms. |
| Easing functions | Apple | Standard: `cubic-bezier(0.2, 0, 0, 1)`. Enter: `cubic-bezier(0, 0, 0.2, 1)`. Exit: `cubic-bezier(0.4, 0, 1, 1)`. Springs (Motion) on the landing page only. |
| Animations | Apple | Landing hero reveal: staggered 60ms steps, opacity plus 16px translate, 640ms total. |
| Page transitions | Framer | Motion `AnimatePresence` for console route changes: fade, 160ms. Use the View Transitions API as an enhancement where supported. |
| Scroll animations | Apple | Landing only. Reveal once with Motion `whileInView` (`once: true`, `amount: 0.2`). No scroll hijacking. No parallax on text. |
| Motion performance | Apple | Animate only transform and opacity. Animate layout only for small list reorders. Set `will-change` only during an active animation. Pause looping animations when offscreen. |
| Reduced motion | Apple | Honor `prefers-reduced-motion` everywhere. No transforms, no loops, instant state changes, cross-fades allowed. Test it with Playwright. |
| Touch interactions | Apple | 44 by 44px minimum hit area. No information available only on hover. No custom gestures for critical actions. |
| Interactive feedback | Linear | Acknowledge every action within 100ms, using optimistic state where it is safe. |
| Interactive prototyping | Framer | Not a deliverable. Verify interaction states with Storybook stories that use play functions. |

### 4.3 States and feedback

| Category | Reference | Rule |
|---|---|---|
| Loading states | GitHub | Inline spinner inside buttons. Top progress bar for route loads. Never show a blank screen. Every spinner has a timeout and an error path. |
| Skeleton loaders | LinkedIn | Skeletons match the real layout dimensions to prevent layout shift. Subtle shimmer, static under reduced motion. |
| Tooltips | GitHub | Open after 400ms of hover or on keyboard focus. Dismiss with Esc. Inverted surface. An icon-only button always has an accessible name, and the tooltip does not replace it. |
| Toast notifications | Linear | Sonner, bottom-right, at most three visible. Success auto-dismisses after 4s. Errors stay until dismissed. Toasts never carry validation errors. |
| Form validation | Stripe | Validate on blur, then on change after the first error. Error text sits under the field and is linked with `aria-describedby`. A summary appears at the top on submit. |
| Error states | Stripe | Say what happened, what to do next, and offer a retry. No raw stack traces. |
| Success states | Stripe | Confirm the result and state the next step, for example "Denied. Incident escalated to the lead." |
| Empty states | Linear | Say what the empty state means and what to do next. Include a last-checked time when it applies, for example "No active incidents. Last checked 14:02:11." |
| Perceived performance | YouTube | Skeletons appear immediately. Approve and deny are optimistic, with rollback on failure. Routes prefetch on hover and focus. |

### 4.4 Icons, illustrations, images

| Category | Reference | Rule |
|---|---|---|
| Iconography | Lucide | lucide-react only. 16px inline, 20px in controls, 1.5px stroke. Never mix icon sets. Never use emoji as icons. |
| Illustrations | Dropbox | Minimal two-tone line illustrations for empty states and the landing page only. Colors from tokens. Inline SVG. |
| Image optimization | Airbnb | Prefer SVG. For rasters, use AVIF or WebP, srcset, and explicit width and height. |
| Lazy loading | Google Images | Route-level code splitting with `React.lazy`. Below-the-fold landing sections and images load lazily, using `loading="lazy"` and `decoding="async"`. |

### 4.5 Responsive and layout engineering

| Category | Reference | Rule |
|---|---|---|
| Responsive design | Airbnb | Design every screen at 375, 768, 1280, and 1920px. Test all four. |
| Mobile-first | Instagram | Write base styles for phones, then add `min-width` queries. On-call engineers use phones: the incident list, incident detail, and approve or deny must all work one-handed. |
| Breakpoints | Bootstrap | 576, 768, 992, 1200, 1400px (`min-width`), exposed as tokens. |
| CSS grid | Vercel | Page-level layout uses grid. |
| Flexbox | Linear | Component internals use flex. |
| Container queries | Shopify | Cards and panels that appear in both the sidebar and the main column use `@container`. |
| Responsive typography | Stripe | `clamp()` for landing display headings only. Console type sizes are fixed. |
| Layout shift prevention | Google Search | Reserve space for images, charts, and async content. Font fallbacks use size-adjust metrics. Target CLS of 0.1 or less. |

### 4.6 Information and navigation

| Category | Reference | Rule |
|---|---|---|
| UX design | Airbnb | Every console screen answers three questions: what is happening, what is affected, and what should I do now. |
| Information architecture | Notion | Console nav: Incidents (Active, History), Agents, Approvals, Runbooks, Settings. Account and sign-out sit in the sidebar footer. |
| Navigation design | Stripe | Persistent sidebar, 240px, collapsible to 64px with icons and tooltips. The active item is marked by a background and an indicator, not by color alone. |
| Content overflow | Notion | Truncate with an ellipsis. Show the full value in a tooltip and in the detail view. Long log lines wrap or scroll inside their own container. |
| Progressive enhancement | GOV.UK | Landing content is readable before JavaScript loads. Use a prerender step for `/` if it fits in under one day. Otherwise skip it and log the decision. The console shows a meaningful shell and a plain explanation if JavaScript fails. |

### 4.7 Systems and code

| Category | Reference | Rule |
|---|---|---|
| Design systems | Shopify Polaris | Every component is documented in Storybook: purpose, anatomy, states, do and don't, accessibility notes. Content rules: sentence case, verb-first button labels. |
| Design tokens | Material Design | Three tiers. Primitive: raw values. Semantic: for example `--color-surface-raised`. Component: for example `--button-bg`. Components use semantic or component tokens only. Export `tokens.json` in Tokens Studio format for handoff. |
| Reusable components | shadcn/ui | Copy-in pattern under `src/components/ui/`, with cva variants. React 19 style, no `forwardRef`. |
| CSS architecture | GitHub Primer | Themes are CSS custom properties switched by `data-theme` on `<html>`. No global component CSS. Tailwind utilities consume tokens only. |
| Component state management | React Aria | Style states using the data attributes react-aria exposes: `data-hovered`, `data-pressed`, `data-focus-visible`, `data-disabled`, `data-selected`. |
| Z-index management | Material UI | Token scale: base 0, sticky 100, dropdown 200, overlay 300, modal 400, toast 500, tooltip 600. No other numbers. |
| Layering | Apple | Four surface layers: canvas, raised, overlay (glass), floating (shadowed). Each layer maps to one z-index token. |
| Visual consistency | Linear | Same radii, spacing, and type ramp on every screen. Enforced by the lint rules in section 3. |
| UI consistency | Linear | One Button component with four variants (primary, secondary, ghost, danger) and three sizes. No ad hoc buttons anywhere. |
| Design-to-development handoff | Figma | Tokens exported as `tokens.json`. The Storybook site is the spec that design and development review together. |

### 4.8 Data visualization

| Category | Reference | Rule |
|---|---|---|
| Data visualization | Grafana | Severity colors mean the same thing everywhere. Show thresholds on charts. Trend charts get a time-range selector. Every chart has a text summary and a table view for screen readers. |
| Chart styling | Vercel | Minimal axes, faint gridlines, 4px bar radius, tooltips on hover and focus. No 3D. No pie charts with more than three slices; use bars. |

### 4.9 Accessibility and internationalization

| Category | Reference | Rule |
|---|---|---|
| WCAG | GOV.UK | Target WCAG 2.2 AA. Automated axe checks in Playwright on every route. A manual keyboard pass on every screen before F7 closes. |
| Keyboard navigation | GitHub | Every action is reachable by keyboard. Skip-to-content link. Roving tabindex in lists and tables. Shortcuts: `/` focuses search, `g` then `i` goes to Incidents, `?` shows the shortcut list, `Esc` closes overlays. |
| Screen reader support | GOV.UK | Landmarks for header, nav, and main. New critical incidents announced through `aria-live="polite"`, at most one announcement per 10 seconds. Status is never conveyed by color alone. |
| Color contrast | GOV.UK | Body text at least 4.5:1. Large text and UI components at least 3:1. A unit test reads the token pairs in both themes and fails the build if any pair falls below its threshold. |
| Semantic HTML | GOV.UK | One `h1` per page. No skipped heading levels. Real buttons for actions and real links for navigation. Tables use `th` with `scope`. |
| ARIA attributes | React Aria | Use react-aria-components for every interactive primitive. Add ARIA only where the primitive lacks it. |
| Right-to-left | Material Design | Use logical CSS properties (`margin-inline-start`, `inset-inline`) everywhere. One Playwright test renders `dir="rtl"`. |
| Internationalization | Airbnb | react-i18next. No hardcoded user-facing strings (lint rule). Dates and numbers formatted with `Intl`. |

### 4.10 Performance, compatibility, and QA

| Category | Reference | Rule |
|---|---|---|
| Performance optimization | Google Search | On a mid-tier Android profile over emulated 4G: LCP at most 2.5s, INP at most 200ms, CLS at most 0.1. Initial JavaScript at most 250KB gzipped for the console and 300KB for the landing page. Measure with Lighthouse CI. |
| Browser compatibility | MDN | Support the last two versions of Chrome, Edge, Firefox, and Safari, plus iOS 16 and later. Check every API against MDN before use. Feature-detect `backdrop-filter` and View Transitions. |
| Cross-browser testing | BrowserStack | Playwright projects for chromium, firefox, and webkit run in CI. BrowserStack is optional for final manual QA and goes under DEVICE PENDING WORK. |
| Cross-device testing | Airbnb | Playwright viewport matrix (375, 768, 1280, 1920) plus touch emulation for the incident list and the approve flow. |

---

## 5. Design tokens

Build three tiers. Primitive values are defined once in `tokens.primitive.css`. Components never read primitives directly. Semantic and component tokens are defined in `tokens.css`. Both themes use the same semantic names.

```css
/* src/styles/tokens.css. Components read semantic tokens only. */
:root,
[data-theme="dark"] {
  --color-canvas: #08090d;
  --color-surface-raised: #0f1117;
  --color-surface-overlay: rgb(22 26 35 / 0.72);
  --color-surface-hover: #1b2030;
  --color-surface-pressed: #222839;
  --color-border-subtle: rgb(255 255 255 / 0.08);
  --color-border-strong: rgb(255 255 255 / 0.16);
  --color-text-primary: #f4f6fa;
  --color-text-secondary: #a4adbe;
  --color-text-tertiary: #7d8698;
  --color-accent: #8b84ff;
  --color-accent-text: #a9a3ff;
  --color-focus-ring: #b3aeff;
  --color-critical: #ff6b6b;
  --color-high: #ff9f4a;
  --color-medium: #f5c542;
  --color-low: #5b9dff;
  --color-success: #3dd68c;
  --shadow-floating: 0 0 0 1px rgb(255 255 255 / 0.06), 0 8px 24px -8px rgb(0 0 0 / 0.6);
  --shadow-modal: 0 0 0 1px rgb(255 255 255 / 0.08), 0 24px 56px -12px rgb(0 0 0 / 0.7);
}

[data-theme="light"] {
  --color-canvas: #fafafb;
  --color-surface-raised: #ffffff;
  --color-surface-overlay: rgb(255 255 255 / 0.7);
  --color-surface-hover: #f2f3f6;
  --color-surface-pressed: #e9ebf0;
  --color-border-subtle: rgb(14 17 22 / 0.08);
  --color-border-strong: rgb(14 17 22 / 0.16);
  --color-text-primary: #0e1116;
  --color-text-secondary: #4a5162;
  --color-text-tertiary: #6b7285;
  --color-accent: #635bff;
  --color-accent-text: #4b43e0;
  --color-focus-ring: #3b33d6;
  --color-critical: #d92d2d;
  --color-high: #c2560b;
  --color-medium: #9a6b00;
  --color-low: #1d5fcc;
  --color-success: #17804a;
  --shadow-floating: 0 0 0 1px rgb(14 17 22 / 0.06), 0 8px 24px -8px rgb(14 17 22 / 0.18);
  --shadow-modal: 0 0 0 1px rgb(14 17 22 / 0.08), 0 24px 56px -12px rgb(14 17 22 / 0.25);
}
```

**Palette rules**
- The accent is Stripe-derived (`#635bff` in light mode), lightened for dark-mode contrast. It is the only brand color in the console.
- Severity and status colors are semantic. Critical, high, medium, and low map to red, orange, amber, and blue. Success is green. Every status pairs its color with an icon and a text label.
- Gradients are landing-only tokens. Use the accent with a cool cyan (`#00d1ff`) and a warm coral (`#ff8a65`) at low alpha on the hero. Never use these colors for status.
- Use the Trinetra mark for the logo. Build it as an original SVG: three nested arcs that suggest an eye, a 1.5px monoline, no gradient. Do not reproduce any existing logo.

**Type ramp** (Inter variable with `opsz`; Instrument Serif for display; Geist Mono for code)
- Console: 12, 13, 14 (default), 16, 18, 20, 24, 32px.
- Landing: body 16 and 18px. Display uses `clamp()` from 40px to 88px, Instrument Serif, with one italic word per headline.
- Tracking: display -0.035em, headings -0.02em, body 0, uppercase labels +0.06em at 12px.
- Line height: 1.1 for display, 1.3 for headings, 1.5 for body.
- Weights: 400, 500, 600.

**Spacing scale** (4px base): `space-0` 0, `space-1` 4, `space-2` 8, `space-3` 12, `space-4` 16, `space-5` 20, `space-6` 24, `space-8` 32, `space-10` 40, `space-12` 48, `space-16` 64, `space-20` 80, `space-24` 96, `space-32` 128.

**Radii:** `radius-xs` 4, `radius-sm` 6, `radius-md` 8, `radius-lg` 12, `radius-xl` 16, `radius-full` 9999.

**Breakpoints** (min-width): sm 576, md 768, lg 992, xl 1200, 2xl 1400.

**Motion tokens:** `duration-instant` 80ms, `duration-fast` 120ms, `duration-base` 200ms, `duration-slow` 240ms, `duration-reveal` 640ms. Easings as listed in section 4.2. Motion springs: `spring-soft` (stiffness 260, damping 28) for landing and `spring-snappy` (stiffness 400, damping 35) for micro-interactions.

**Z-index:** base 0, sticky 100, dropdown 200, overlay 300, modal 400, toast 500, tooltip 600.

---

## 6. Component inventory

Build these first, in Storybook, each with every state (default, hover, focus-visible, pressed, disabled, loading, error, selected):

- **Actions:** Button (primary, secondary, ghost, danger; sm, md, lg; loading), IconButton, Link, KeyboardShortcut (kbd)
- **Inputs:** TextField, TextArea, Select, Combobox (filters), Checkbox, Switch, RadioGroup, SegmentedControl, DateRange (history filter)
- **Navigation:** Sidebar, Topbar, Breadcrumb, Tabs, Drawer (mobile navigation), CommandPalette (optional; log the decision)
- **Display:** Badge (severity, status, environment), StatusDot, Avatar, Card, Panel, MetricTile, Countdown, CodeBlock (Geist Mono, copy button, wraps or scrolls), MarkdownView (sanitized)
- **Data:** Table (roving focus; virtualized above 200 rows), DataList, Timeline (agent events), ConfidenceBar (0 to 1 with an 0.80 threshold marker), StateProgress (incident state machine), Chart wrappers (trend, bar, histogram) with table fallbacks
- **Overlays:** Tooltip, Popover, Menu, Dialog (confirm), Toast (Sonner wrapper)
- **Feedback:** Skeleton, Spinner, InlineAlert, EmptyState, ErrorState, ProgressBar
- **Landing-only:** HeroHeadline, MeshBackdrop, WorkflowDiagram, FeatureCard (with spotlight), CTABand, Footer, Logo

---

## 7. Screens

### 7.1 Landing `/` (public)
- **Hero:** Headline with one italic serif word. Subcopy describes what the agents do. Primary CTA "Open console" goes to `/login` and uses the magnetic effect. Secondary "See how it works" scrolls to the workflow section.
- **Agent workflow:** An interactive SVG of the eight agents: the coordinator, three parallel investigators (logs, metrics, runbooks), root cause analysis, remediation proposal, approval gate or auto-executor, and audit. Parallel branches reveal together to show concurrency. Hovering a node shows its role in one sentence. Keyboard users can step through nodes with arrow keys.
- **Guardrails:** What agents can and cannot do: blocked command classes, approval rules for medium and high risk, the full audit trail, and confidence thresholds.
- **Targets:** Show the doc 09 targets (MTTR under 15 minutes, MTTA under 2 minutes, auto-resolution above 60%) labeled as **targets**. Never present them as measured results.
- **Copy rules:** No fake logos, testimonials, customer counts, or invented metrics. No lorem ipsum. Facts come from the specs only.

### 7.2 Login `/login`
Email and password form with Zod validation and inline errors. The auth provider sits behind an `AuthProvider` interface with a mock implementation for now (section 10). Include a route guard for all console routes.

### 7.3 Active incidents `/incidents`
- Table on desktop and cards on phones. Columns: severity, title, services, state, age (refreshes every 30s), assignee.
- Filters: status, severity, service. Filter state lives in the URL.
- New incidents arrive over SignalR (mocked) and appear at the top with a one-time highlight. Under reduced motion they appear with no animation.
- Keyboard: `j` and `k` move between rows, `Enter` opens the detail view.

### 7.4 Incident detail `/incidents/:id`
- Header: title, severity, state, age, assignee, and the StateProgress bar showing triggered, investigating, analysis_complete, awaiting_approval, executing_remediation, monitoring_recovery, and resolved, or escalated as a branch.
- Root cause card: category, confidence (ConfidenceBar with the 0.80 threshold marked), contributing factors, and evidence chain.
- Remediation options: each shows risk level, steps, pre-flight and post-flight checks, and rollback. Show the backend's verdict. Never show "allowed" when the backend says blocked.
- Agent timeline from `incident_events`: each entry expands to show tool name, parameters, and result. Guardrail-triggered entries are visually distinct and labeled in text.
- Snapshots: metrics summary, a log excerpt in Geist Mono (virtualized), and linked runbooks.

### 7.5 Agent activity `/agents`
Live list of agents for in-progress incidents: status (idle, working, waiting, failed), current tool call, elapsed time, and a count of guardrail violations. Filter to in-progress by default.

### 7.6 Approvals `/approvals`
- Queue sorted by timeout, soonest first. Each card shows:
  - the full command in Geist Mono, copyable
  - risk level and required approver level
  - a Countdown to the 15-minute timeout (seconds are visible in the last minute, and screen readers get updates only at minute boundaries)
  - the rollback description
- Approve opens a confirm Dialog. High-risk actions require typing the incident ID before the button enables.
- Deny requires a reason.
- If the current user's role cannot act, the buttons are disabled and the reason is shown next to them.
- Approve and deny are optimistic. On failure, the card rolls back and an InlineAlert explains what happened.
- The frontend never decides guardrail outcomes. It displays backend verdicts only.

### 7.7 History `/incidents/history`
- Date range selector (7, 30, or 90 days).
- Charts: MTTR trend, incidents by root cause category, and a resolution-time histogram. Each chart has a text summary and a table view.
- Table of resolved incidents with links to post-mortems. No post-mortem endpoint exists yet, so show "Not generated" and log the gap.

### 7.8 Runbooks `/runbooks` and Settings `/settings`
- Runbooks: searchable list and detail, read-only. Markdown renders through the sanitized MarkdownView.
- Settings stub: theme (system, dark, light), reduce-motion override, keyboard shortcut list, and profile.

---

## 8. Motion system

| Where | Motion | Reduced motion |
|---|---|---|
| Landing hero | Headline words reveal upward inside a mask, 640ms, 60ms stagger. Subcopy and CTA fade in 200ms later. | Opacity only, no stagger |
| Magnetic CTA | Within 80px of the pointer, the CTA moves up to 6px toward it (`spring-soft`). Disabled on coarse pointers. | Off |
| Mesh backdrop | Slow drift, 24s loop, transform only. Paused offscreen. | Static |
| Workflow diagram | Connectors draw with stroke-dashoffset on scroll. Nodes reveal in sequence. Parallel branches reveal together. | Static, fully visible |
| Feature cards | Spotlight follows the pointer (radial gradient at cursor). Fine pointers only. | Off |
| Console cards | Border brightens in 120ms. No lift. | Same, no change needed |
| Approve success | Checkmark path draws in 300ms, then the card collapses. | Instant state change |
| New critical incident | One-time background tint, 1.2s. | Static tint for 1.2s, then none |
| Numbers | Tween 300ms on change. | Instant |
| Skeleton | Shimmer, 1.4s loop. | Static pulse at 0.6 opacity |
| Route change | Fade, 160ms. | Instant |

Motion must not block input. No animation may delay a user action by more than 100ms.

---

## 9. Accessibility and responsiveness: tests

- Every route passes axe with zero serious or critical violations at all four widths.
- Playwright keyboard path for each screen: reach every control, operate it, and escape overlays.
- Reduced-motion test: with `prefers-reduced-motion: reduce`, assert no transform animations run on the landing page.
- RTL smoke test: render the incident list with `dir="rtl"` and assert no clipped text or overlapping controls.
- Touch emulation: approve flow works with tap only, with targets at least 44px.
- Contrast unit test: read token pairs in both themes and fail below threshold.
- Manual keyboard checklist written in `docs/a11y-checklist.md`. Complete it in F7.

---

## 10. Data, mocks, and backend contract

- Base URL `/v1`, per doc 04. A typed client lives in `src/api/`. Every response is parsed with a Zod schema at the boundary.
- In development and tests, MSW serves every endpoint in doc 04 plus the approval endpoints. Seed data includes: one critical incident awaiting approval, one escalated incident, one resolved incident with a guardrail-triggered timeline entry, and a short history. Label all seed data "Demo data" in a visible dev banner.
- Do not present the numbers in doc 05 as real data. They are illustrative.
- Real-time: SignalR client connects to a hub at `/hubs/incidents`. The hub is not in doc 04. Define the event contract in F2, mock it, and log it as a spec gap.
- Auth: the `AuthProvider` interface has a mock implementation now. Keep the access token in memory, never in `localStorage`. Doc 04 has no login or refresh endpoints. Log the gap and ask the backend to add them.
- Safety: the frontend displays backend verdicts only. Approve is disabled whenever the backend marks an action as blocked or requires a role the user lacks.

---

## 11. Quality gates

- **Unit:** Vitest and Testing Library for components, hooks, formatters, and the contrast test.
- **Storybook:** every component has stories for every state. addon-a11y reports zero violations.
- **e2e:** one Playwright spec per screen covering load, keyboard path, and the main action. The approve flow is covered in full.
- **axe:** zero serious or critical violations per route.
- **Lighthouse CI:** budgets from section 4.10.
- **Lint:** no raw hex colors, no raw px radii, no raw z-index numbers, no hardcoded user-facing strings outside i18n resources.
- **Visual regression (recommended):** Playwright screenshots of key stories in both themes. Commit baselines from the home device.

---

## 12. Phases

**F0. Remove Angular and scaffold.** Delete the Angular project. Scaffold React with Vite and TypeScript strict. Add ESLint, Prettier, Vitest, Playwright, Storybook, and MSW. Add a frontend CI job. `npm run dev`, `lint`, `test`, and `build` must pass.
*Done when:* the app renders a "Trinetra" placeholder in both themes, and all four scripts pass.

**F1. Design foundations.** Build the three-tier tokens, `tokens.json` export, theme switching with persistence, the type ramp, the icon set, and the base components from section 6 (actions, inputs, display, overlays, feedback). Write their stories. Add the contrast test and the lint rules.
*Done when:* every component story passes axe in both themes, the contrast test passes, and the lint rules are enforced.

**F2. Console shell, routing, and mocks.** Build the sidebar, topbar, shortcuts, route transitions, the login screen with the mock `AuthProvider`, the route guard, and MSW handlers for every endpoint in doc 04. Define the SignalR event contract and mock it.
*Done when:* navigation works by mouse and keyboard at four widths, and route changes cause no layout shift.

**F3. Incidents.** Build the active list (with the real-time mock) and the incident detail (StateProgress, root cause, remediation options, timeline).
*Done when:* the seeded incidents render, the guardrail-triggered timeline entry is distinct and labeled, and the detail e2e spec passes.

**F4. Agents and approvals.** Build the activity monitor, the approval queue with Countdown, the confirm Dialog (typed confirmation for high risk), deny with a reason, and optimistic approve with rollback.
*Done when:* the approve and deny e2e specs pass, a blocked action cannot be approved in the UI, and the countdown behaves correctly at 60 seconds and at zero.

**F5. History, runbooks, settings.** Build the charts with text summaries and table views, the sanitized runbook viewer, and the settings screen.
*Done when:* the charts pass axe, the markdown sanitizer test passes, and settings persist.

**F6. Landing page and signature motion.** Build all landing sections, the magnetic CTA, the mesh backdrop, the workflow diagram, the feature spotlight, and the reduced-motion variants. Add the prerender step if it fits in under one day.
*Done when:* Lighthouse budgets are met, the reduced-motion test passes, and no unverified claim appears on the page.

**F7. Hardening.** Complete the manual keyboard checklist. Run cross-browser e2e. Confirm i18n extraction is complete. Run the RTL smoke test. Write the frontend README. Finalize HANDOFF.md and list every item that is still pending on a device.
*Done when:* all gates in section 11 pass on the home device, and pending items are recorded with the device that needs them.

---

## 13. Banned patterns

- Default purple-to-pink gradients on white or black backgrounds.
- The generic hero: a headline, three feature cards, and a stock photo.
- Neumorphism. Glass on data panels. Drop shadows on every card.
- Radii above 16px on anything except badges, avatars, and toggles.
- Emoji as icons. Mixed icon sets. Decorative icons next to every heading.
- Centered multi-line body text. Text over busy imagery without a scrim.
- Animating layout properties. Parallax on text. Scroll hijacking. Looping motion on console screens.
- Information available only on hover. Status conveyed by color alone.
- Fake metrics, fake testimonials, fake logos, lorem ipsum.
- Spinners with no timeout and no error path.
- Stacked modals. Toasts for validation errors.

---

## 14. Spec conflicts to resolve and log in DECISIONS

1. **Framework:** Angular is replaced by React. This is the owner's decision.
2. **Real-time:** doc 01 names Socket.io. Use SignalR instead, because it is native to ASP.NET Core and the backend owns the hub.
3. **State:** doc 01 names Redux Toolkit. Use Zustand for UI state and TanStack Query for server state.
4. **UI kit:** doc 01 names MUI or Bootstrap. Use the custom system built on react-aria-components and Tailwind v4.
5. **Auth:** doc 04 has no login or refresh endpoints. Use the mock `AuthProvider` and log the gap for the backend.
6. **SignalR hub:** not defined in doc 04. Define the event contract in F2 and log it.
7. **Post-mortems:** no endpoint exists. Show a placeholder and log it.
8. **Runbooks:** no console screen is defined in doc 09. Add a read-only view.
9. **Metrics:** doc 09 values are targets. Label them "Target" in the UI. Never present them as measured.
10. **Doc 00:** it references docs 10 to 15, which do not exist. Log it. Do not create those docs in this build.
11. **Doc 05 sample numbers:** illustrative only. Do not use them as fixtures.

---

## 15. Definition of done

- Phases F0 to F7 are complete, or every incomplete item is recorded in DEVICE PENDING WORK with the device that needs it.
- On the home device, `npm run lint`, `npm run test`, `npm run build`, and `npm run e2e` all pass.
- axe, Lighthouse budgets, the reduced-motion test, and the four-width tests all pass.
- `npm run storybook` builds, and every component is documented.
- HANDOFF.md is current, and every phase report has been posted.

---

## Start now

Begin with F0. Stop at the end of F0, post the phase report, and wait for "continue".
