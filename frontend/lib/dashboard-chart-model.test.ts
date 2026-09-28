import assert from "node:assert/strict";
import test from "node:test";
import { buildPieStops, chartDimensions, chartMetricLimit, chartRequiresSameUnit, DATA_COMPONENT_TYPES } from "./dashboard-chart-model.ts";

test("visualization types use the persisted backend contract", () => {
  assert.ok(DATA_COMPONENT_TYPES.includes("horizontalBar"));
  assert.ok(DATA_COMPONENT_TYPES.includes("column"));
  assert.ok(DATA_COMPONENT_TYPES.includes("goalGauge"));
  for (const type of ["map","bullet","treemap","sankey","waterfall","boxPlot","candlestick","timeline"] as const)
    assert.ok(DATA_COMPONENT_TYPES.includes(type), `${type} must be available`);
  assert.equal(DATA_COMPONENT_TYPES.includes("barChart" as never), false);
});

test("chart rules constrain dimensions and metric counts", () => {
  assert.deepEqual(chartDimensions("combo"), ["date"]);
  assert.deepEqual(chartDimensions("pie"), ["campaign"]);
  assert.deepEqual(chartDimensions("goalGauge"), ["none"]);
  assert.equal(chartMetricLimit("bubble"), 3);
  assert.equal(chartMetricLimit("scatter"), 2);
  assert.equal(chartMetricLimit("donut"), 1);
  assert.equal(chartMetricLimit("candlestick"), 3);
  assert.deepEqual(chartDimensions("timeline"), ["date"]);
  assert.deepEqual(chartDimensions("bullet"), ["none"]);
  assert.equal(chartRequiresSameUnit("stacked100Bar"), true);
  assert.equal(chartRequiresSameUnit("combo"), false);
});

test("pie stops ignore negative values and cover the full circle", () => {
  assert.equal(buildPieStops([25, -4, 75], ["red", "blue", "green"]), "red 0% 25%, blue 25% 25%, green 25% 100%");
  assert.equal(buildPieStops([0, 0], ["red"]), "#e8ebed 0% 100%");
});
