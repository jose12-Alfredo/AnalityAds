import type { DashboardComponent, DashboardDefinition, DashboardPage } from "@/lib/dashboards";

export const EDITOR_GRID_SIZE = 12;
export const EDITOR_HISTORY_LIMIT = 50;

export function cloneDefinition(definition: DashboardDefinition) {
  return structuredClone(definition);
}

export function normalizePageOrders(definition: DashboardDefinition) {
  definition.pages.forEach((page, index) => { page.order = index; });
  return definition;
}

export function createPage(index: number, source?: DashboardPage): DashboardPage {
  const groupIds = new Map<string, string>();
  return {
    id: crypto.randomUUID(),
    name: source ? `${source.name} copia` : `Página ${index + 1}`,
    order: index,
    width: source?.width ?? 1440,
    height: source?.height ?? 900,
    background: source?.background ?? "#eef0f3",
    components: source ? source.components.map((item) => {
      const copy = { ...structuredClone(item), id: crypto.randomUUID() };
      if (copy.groupId) {
        if (!groupIds.has(copy.groupId)) groupIds.set(copy.groupId, crypto.randomUUID());
        copy.groupId = groupIds.get(copy.groupId);
      }
      return copy;
    }) : [],
  };
}

export function selectedComponents(page: DashboardPage, ids: ReadonlySet<string>) {
  return page.components.filter((item) => ids.has(item.id));
}

export function updateComponents(definition: DashboardDefinition, pageId: string, ids: ReadonlySet<string>, update: (item: DashboardComponent) => DashboardComponent) {
  const next = cloneDefinition(definition);
  const page = next.pages.find((item) => item.id === pageId);
  if (page) page.components = page.components.map((item) => ids.has(item.id) ? update(item) : item);
  return next;
}

export function deleteComponents(definition: DashboardDefinition, pageId: string, ids: ReadonlySet<string>) {
  const next = cloneDefinition(definition);
  const page = next.pages.find((item) => item.id === pageId);
  if (page) page.components = page.components.filter((item) => !ids.has(item.id) || item.isLocked);
  return next;
}

export function alignComponents(definition: DashboardDefinition, pageId: string, ids: ReadonlySet<string>, mode: "left" | "center" | "right" | "top" | "middle" | "bottom") {
  const next = cloneDefinition(definition);
  const page = next.pages.find((item) => item.id === pageId);
  if (!page) return next;
  const items = selectedComponents(page, ids).filter((item) => !item.isLocked);
  if (items.length < 2) return next;
  const left = Math.min(...items.map((item) => item.x));
  const right = Math.max(...items.map((item) => item.x + item.width));
  const top = Math.min(...items.map((item) => item.y));
  const bottom = Math.max(...items.map((item) => item.y + item.height));
  for (const item of items) {
    if (mode === "left") item.x = left;
    if (mode === "center") item.x = Math.round((left + right - item.width) / 2);
    if (mode === "right") item.x = right - item.width;
    if (mode === "top") item.y = top;
    if (mode === "middle") item.y = Math.round((top + bottom - item.height) / 2);
    if (mode === "bottom") item.y = bottom - item.height;
  }
  return next;
}

export function distributeComponents(definition: DashboardDefinition, pageId: string, ids: ReadonlySet<string>, axis: "horizontal" | "vertical") {
  const next = cloneDefinition(definition);
  const page = next.pages.find((item) => item.id === pageId);
  if (!page) return next;
  const items = selectedComponents(page, ids).filter((item) => !item.isLocked)
    .sort((a, b) => axis === "horizontal" ? a.x - b.x : a.y - b.y);
  if (items.length < 3) return next;
  if (axis === "horizontal") {
    const start = items[0].x;
    const end = items.at(-1)!.x + items.at(-1)!.width;
    const gap = (end - start - items.reduce((sum, item) => sum + item.width, 0)) / (items.length - 1);
    let cursor = start;
    for (const item of items) { item.x = Math.round(cursor); cursor += item.width + gap; }
  } else {
    const start = items[0].y;
    const end = items.at(-1)!.y + items.at(-1)!.height;
    const gap = (end - start - items.reduce((sum, item) => sum + item.height, 0)) / (items.length - 1);
    let cursor = start;
    for (const item of items) { item.y = Math.round(cursor); cursor += item.height + gap; }
  }
  return next;
}

export function groupComponents(definition: DashboardDefinition, pageId: string, ids: ReadonlySet<string>, group: boolean) {
  const groupId = group ? crypto.randomUUID() : undefined;
  return updateComponents(definition, pageId, ids, (item) => ({ ...item, groupId }));
}

export function duplicateComponents(definition: DashboardDefinition, pageId: string, ids: ReadonlySet<string>, offset = 24) {
  const next = cloneDefinition(definition);
  const page = next.pages.find((item) => item.id === pageId);
  if (!page) return { definition: next, ids: new Set<string>() };
  const originals = selectedComponents(page, ids);
  const groupMap = new Map<string, string>();
  const top = Math.max(0, ...page.components.map((item) => item.zIndex));
  const copies = originals.map((item, index) => {
    const copy = structuredClone(item);
    copy.id = crypto.randomUUID(); copy.x = Math.min(page.width - copy.width, copy.x + offset); copy.y = Math.min(page.height - copy.height, copy.y + offset);
    copy.zIndex = top + index + 1; copy.isLocked = false;
    if (copy.groupId) { if (!groupMap.has(copy.groupId)) groupMap.set(copy.groupId, crypto.randomUUID()); copy.groupId = groupMap.get(copy.groupId); }
    return copy;
  });
  page.components.push(...copies);
  return { definition: next, ids: new Set(copies.map((item) => item.id)) };
}

export function expandGroupSelection(page: DashboardPage, ids: ReadonlySet<string>) {
  const expanded = new Set(ids);
  const groups = new Set(page.components.filter((item) => ids.has(item.id) && item.groupId).map((item) => item.groupId));
  page.components.forEach((item) => { if (item.groupId && groups.has(item.groupId)) expanded.add(item.id); });
  return expanded;
}
