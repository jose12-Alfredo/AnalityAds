import { expect, test, type Page } from "@playwright/test";

const session = {
  accessToken: "e2e-access-token",
  expiresAtUtc: "2099-01-01T00:00:00Z",
  userId: "00000000-0000-0000-0000-000000000001",
  email: "owner@example.test",
  agencyId: "00000000-0000-0000-0000-000000000002",
  agencyName: "Agencia E2E",
  role: "Owner",
};

async function mockEmptyWorkspace(page: Page) {
  await page.route("**/api/v1/auth/me", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        userId: session.userId,
        email: session.email,
        agencyId: session.agencyId,
        agencyName: session.agencyName,
        role: session.role,
      }),
    });
  });
  await page.route("**/api/v1/clients", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
  });
}

test("login and registration pages link to each other", async ({ page }) => {
  await page.goto("/login");
  await expect(page.getByRole("heading", { name: /Inicia sesi/ })).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.getByRole("link", { name: /Reg.strate/ }).click();
  await expect(page).toHaveURL(/\/registro$/);
  await expect(page.getByRole("heading", { name: "Registra tu agencia" })).toBeVisible();
});

test("shows the API error when credentials are rejected", async ({ page }) => {
  await page.route("**/api/v1/auth/login", async (route) => {
    await route.fulfill({
      status: 401,
      contentType: "application/problem+json",
      body: JSON.stringify({ status: 401, title: "Unauthorized" }),
    });
  });

  await page.goto("/login");
  await page.getByLabel(/Correo electr/).fill("owner@example.test");
  await page.getByLabel(/Contrase/).fill("Incorrecta123");
  await page.getByRole("button", { name: /Iniciar sesi/ }).click();

  await expect(page.locator(".notice[role='alert']")).toContainText("no son correctos");
});

test("stores a successful session, opens the private shell, and signs out", async ({ page }) => {
  await page.route("**/api/v1/auth/login", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(session) });
  });
  await mockEmptyWorkspace(page);

  await page.goto("/login");
  await page.getByLabel(/Correo electr/).fill(session.email);
  await page.getByLabel(/Contrase/).fill("Correcta12345");
  await page.getByRole("button", { name: /Iniciar sesi/ }).click();

  await expect(page).toHaveURL(/\/app$/);
  if (await page.evaluate(() => window.innerWidth <= 1000)) {
    await page.getByRole("button", { name: /Men/ }).click();
  }
  await expect(page.getByRole("navigation", { name: /Navegaci/ })).toBeVisible();
  await expect(page.getByRole("heading", { name: /no tienes clientes asignados/ })).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem("analitiads.auth.session"))).not.toBeNull();

  await page.getByRole("button", { name: /Cerrar sesi/ }).first().click();
  await expect(page).toHaveURL(/\/login$/);
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem("analitiads.auth.session"))).toBeNull();
});
