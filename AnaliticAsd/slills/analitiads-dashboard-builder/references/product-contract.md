# Product contract: AnalitiAds dashboard builder

## Confirmed outcome

AnalitiAds is a Spanish-language, multi-tenant web application for advertising agencies. An agency organizes clients in folders, connects Meta Ads, Google Ads, TikTok Ads, and Google Analytics 4 sources, builds freely editable dashboards for each client, and shares published dashboards through secure read-only links. This is a dashboard builder, not a fixed KPI screen.

The required hierarchy is: Agency → Clients → folders/subfolders → dashboards → pages → components. Clients have branding, description, several accounts from one or more providers, their own dashboards, templates, and configuration.

An agency must operate many client workspaces concurrently. Each client can have any number of customized dashboards for different goals, campaigns, audiences, periods, brands, and recipients. Creating or editing one client's dashboard must not lock, overwrite, filter, publish, or otherwise affect another client's work.

Every client workspace owns its sources, folder tree, dashboards, drafts, published versions, branding, templates, permissions, share links, exports, and delivery schedules. A dashboard can use only sources assigned to its client. Agency-level templates contain source placeholders rather than client account identifiers.

The first functional version requires all three real advertising integrations plus Google Analytics 4. CSV import may complement them but cannot replace them.

## Roles and access

- Agency administrator manages users, clients, connections, templates, dashboards, and permissions.
- Agency editor works only with assigned clients and dashboards.
- Client/visitor only reads the dashboard shared with them.

All authorization is server-side. Agencies and clients are strictly isolated. Client access never grants editor, other-client, folder, secret, or excluded-data access.

Internal editors receive explicit client assignments. Owners and administrators may work across the agency; editors, client users, and link visitors see only the clients or publications granted to them.

## Dashboard lifecycle and organization

Users can create, rename, search, move, duplicate, archive, and recover folders and dashboards with confirmations where appropriate. Copying a dashboard to another client must require replacement of its data sources and must never retain original-client data or account access.

Dashboards support blank canvases and templates, multiple pages, components, free layout, grid/guides, multi-select, alignment, distribution, groups, locks, layers, copy/paste, undo/redo, zoom, preview, autosave, and recovery after failed saves. Layout must persist. Draft edits and published versions are distinct.

Initial components: KPI cards, tables, pivot tables, time series/lines, horizontal and vertical bars, stacked and 100% stacked bars, pie/donut, areas, line-column combinations, funnels, scatter/bubbles, goal gauges, text, images, logos, shapes, and separators.

Future components that must remain explicitly pending until implemented: maps, bullet charts, treemaps, Sankey, waterfalls, box plots, candlesticks, and timelines.

Every chart config separates data settings from visual settings and can specify source(s), account, dimensions, breakdown, metrics, aggregation, date range/comparison, filters, sorting/limit, title/description, numeric format, currency, decimals, colors, labels, legends, axes, and goals/reference lines where compatible.

## Connectors and storage

Each official provider connector supports provider-specific authorization, source discovery/selection, account-or-property-to-client assignment, authorization status, available date interval, last successful sync, errors, and reconnection. Use least-privilege reporting access only; never create campaigns, modify budgets, change Analytics configuration, or write website events.

Google Analytics is specifically GA4. Use its Admin API only to list authorized accounts/properties and its Data API to read reports; use the `analytics.readonly` scope. Do not use Measurement Protocol, property administration, tag creation, audience changes, or access-binding management.

Persist historical data. Initial imports respect provider limits. Scheduled and manual synchronization require background jobs, pagination/batches, provider limits, controlled retries, recovery after interruption, audit logs, idempotent upserts, and recent-period refreshes for delayed attribution. Clearly show freshness.

Secrets live only on the server and receive suitable protection. Do not assume provider OAuth, app review, API version, reporting dimensions, rate limits, or production approval flows are interchangeable. Verify them from current official documentation at implementation time.

## Data semantics

The catalog states each metric's provider, definition, unit, aggregation, and limitations. Include available spend, impressions, reach, frequency, click types, CTR, CPC, CPM, conversions, leads, purchases, messages, cost per result/lead/purchase, conversion value, ROAS, and applicable video/engagement metrics.

Support platform/account/campaign/ad group/ad, date/week/month, objective, status, geographic/device/placement, and permitted age/gender dimensions. Validate provider-compatible metric/dimension combinations.

Filters may target a component, page, or dashboard; their scope is visible. Support date controls, lists, search, multiple selection, comparisons, drilldown, and chart-driven filtering when supported.

Formula fields include validation and clear errors. Ratios are calculated from compatible totals. Handle zero denominators and absence. Do not sum unique reach across days, campaigns, or providers. Do not combine currencies without an explicit documented conversion. Preserve timezone, currency, click type, conversion definition, and attribution model/window where available. Cross-channel totals do not imply deduplicated people.

## Branding, sharing, and delivery

Agency/client logos, palettes, typography, backgrounds, borders, text, notes, covers, summaries, reusable dashboard/page templates, and initial editable templates for sales, lead generation, messages, awareness, and multichannel summary are required.

Published dashboards use read-only links that can be activated, revoked, regenerated, optionally expired, password-protected, recipient-restricted, and configured for visitor filters and exports. Tokens and authorization protect views, APIs, exports, and PDFs. Visitors cannot enter the editor or change published definitions.

Provide readable PDF export, authorized table CSV/Excel export, and server-side scheduled email delivery with recipients, frequency, period, and generation time. PDFs respect pages, charts, filters, and permissions.

## Delivery standard

The project must run from a clean Windows checkout in VS Code, document dependencies and configuration, apply database changes safely, and never erase data to resolve setup. The delivery includes source, migrations, verified setup instructions, tests run, and an explicit done/pending list.

Acceptance requires two isolated clients, a real authorized account from each advertising provider and a real authorized GA4 property, validated equivalent figures, editable persisted dashboards, background synchronization, secure external access/revocation, PDF export, no browser or repository secrets, and demonstrated authorization failures for cross-client/edit attempts.
