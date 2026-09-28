import test from "node:test";
import assert from "node:assert/strict";
import { alignComponents, createPage, deleteComponents, distributeComponents, duplicateComponents, groupComponents } from "./dashboard-editor-model.ts";
import type { DashboardComponent, DashboardDefinition } from "./dashboards.ts";

function component(id: string, x: number, width = 100, locked = false): DashboardComponent {
  return { id, type: "shape", x, y: x, width, height: 60, zIndex: x, isLocked: locked, data: { sources: [] }, style: {} };
}

function definition(): DashboardDefinition {
  return { schemaVersion: 1, pages: [{ id: "00000000-0000-0000-0000-000000000001", name: "Página 1", order: 0, width: 1000, height: 800, components: [component("a", 10), component("b", 250), component("c", 600)] }] };
}

test("align and distribute only the selected unlocked components", () => {
  const ids = new Set(["a", "b", "c"]);
  const aligned = alignComponents(definition(), definition().pages[0].id, ids, "top");
  assert.deepEqual(aligned.pages[0].components.map((item) => item.y), [10, 10, 10]);
  const distributed = distributeComponents(definition(), definition().pages[0].id, ids, "horizontal");
  assert.deepEqual(distributed.pages[0].components.map((item) => item.x), [10, 305, 600]);
});

test("delete keeps locked components and duplicate assigns new ids", () => {
  const source = definition(); source.pages[0].components[1].isLocked = true;
  const removed = deleteComponents(source, source.pages[0].id, new Set(["a", "b"]));
  assert.deepEqual(removed.pages[0].components.map((item) => item.id), ["b", "c"]);
  const duplicated = duplicateComponents(source, source.pages[0].id, new Set(["a", "b"]));
  assert.equal(duplicated.ids.size, 2);
  assert.equal(new Set(duplicated.definition.pages[0].components.map((item) => item.id)).size, 5);
});

test("groups persist when duplicating a page but receive new identifiers", () => {
  const source = definition();
  const grouped = groupComponents(source, source.pages[0].id, new Set(["a", "b"]), true);
  const originalGroup = grouped.pages[0].components[0].groupId;
  const copy = createPage(1, grouped.pages[0]);
  assert.ok(originalGroup);
  assert.equal(copy.components[0].groupId, copy.components[1].groupId);
  assert.notEqual(copy.components[0].groupId, originalGroup);
  assert.equal(copy.components[2].groupId, undefined);
});
