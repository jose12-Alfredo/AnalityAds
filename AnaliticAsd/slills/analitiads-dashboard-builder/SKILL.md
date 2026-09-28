---
name: analitiads-dashboard-builder
description: Build and evolve AnalitiAds as a multi-tenant advertising dashboard builder with an editable visual editor, Meta/Google Ads/TikTok Ads/GA4 connectors, publishing, and secure sharing. Use for product planning or implementation that affects dashboards, connectors, sync, metrics, sharing, exports, or their frontend.
---

# AnalitiAds dashboard builder

Use this skill for the product described in [the product contract](references/product-contract.md) and [the implementation plan](references/implementation-plan.md). It is the current direction for AnalitiAds, not an optional redesign.

When implementing the current foundation phase, also read [phase D1](references/phase-d1-foundation.md).
Before continuing code in a later session, read [the current implementation status](references/current-progress.md) so completed work is not repeated and pending work is taken in order.

## Project boundary

- The sole backend is `AnaliticAsd.sln` and `AnaliticAsd/AnaliticAsd.csproj`; the previous parallel `src/` solution was removed.
- The web client is `frontend/` and targets the backend through `NEXT_PUBLIC_API_URL`. Read `frontend/AGENTS.md` before changing it.
- Preserve user changes and avoid creating a parallel backend, database, or dashboard implementation.
- Existing AnalysisEngine reports and the contract in `../analitiads-frontend-reports/references/report-api.md` are legacy immutable reports. Keep their routes and data working while the new editable dashboard model is introduced separately.

## Before implementation

1. Read [the product contract](references/product-contract.md) and [the implementation plan](references/implementation-plan.md), then inspect the affected code and current migrations.
2. Separate confirmed requirements from technical choices. Do not treat a mock connector or UI button as a real integration.
3. For a provider integration, verify the current official provider documentation before choosing versions, scopes, authorization flow, reporting fields, rate-limit handling, or production prerequisites.
4. Identify external prerequisites precisely. Continue with independent code, tests, and configuration guidance when credentials or approval are absent, but mark real-provider verification as blocked.

## Implementation order

Build vertical, verifiable increments in this order unless the user changes it:

1. Generalize platform accounts and connection records; add folders, editable dashboards, pages, components, draft/published versions, and templates.
2. Deliver the persisted visual editor with a small useful component set before advanced charts.
3. Add a normalized query and metric catalog, then connect the editor to persisted Meta data.
4. Add Google Ads, TikTok Ads, and Google Analytics 4 as independent adapters with real authorization, source assignment, sync, and tests.
5. Add server-side scheduled synchronization, retry/recovery, formulas, safe cross-channel queries, public published views, and controlled exports.
6. Add remaining chart types, delivery schedules, and production verification.

Do not start advanced visualizations, cross-platform joins, or scheduled mail before the underlying persistence, authorization, and data semantics support them.

## Non-negotiable product rules

- One agency manages many clients concurrently. Every client owns an isolated workspace of sources, folders, dashboards, drafts, publications, branding, templates, and share links; all queries must carry and validate that ownership on the server.
- Enforce agency, client, role, assignment, publication, and share-link permissions on the server. Interface visibility is never authorization.
- Keep provider secrets, refresh tokens, share tokens, and export credentials out of the browser, repository, URLs, logs, analytics, and persistent browser storage.
- Store observed platform data with its source platform, account, currency, timezone, attribution context, availability, and synchronization metadata.
- Never substitute missing values with zero. Compute ratios from compatible period totals; do not average daily ratios or sum unique reach across days, campaigns, or platforms.
- Do not combine currencies, attribute cross-platform conversions as deduplicated people, or allow an incompatible chart/query without explaining the missing compatible field.
- A visitor can interact only with the published dashboard and explicitly permitted filters/exports. Their choices must not modify its published definition.
- Keep draft and published dashboard definitions distinct. Publishing is an explicit action and a revoked link must stop working immediately.
- Copying a dashboard between clients must remove its source bindings and require explicit rebinding to sources owned by the destination client.

## Completion evidence

For each increment, add meaningful automated coverage for permissions, tenant isolation, persistence, data calculations, and error handling. Build the backend and relevant frontend checks. Report what was actually run, what requires external credentials, and what remains pending. A first functional version is complete only after real authorized sources from Meta, Google Ads, TikTok Ads, and Google Analytics 4 have been connected, synchronized, and compared against each provider with equivalent dates and criteria.

## End-of-increment handoff

Whenever a work increment finishes:

1. Update `references/current-progress.md` with the completed behavior, files or migrations, verification results, limitations, and external blockers.
2. State clearly to the user what was completed and what was actually tested.
3. Plan the next increment before ending the response: objective, implementation order, expected result, and acceptance checks.
4. Name the exact next increment so the user can authorize continuation without having to ask what is missing or what follows.
5. Keep later phases visible as pending and never describe unexecuted or simulated work as complete.
