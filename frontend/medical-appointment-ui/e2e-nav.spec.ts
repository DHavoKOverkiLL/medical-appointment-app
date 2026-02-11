import { test, expect } from '@playwright/test';

function base64url(obj: unknown): string {
  return Buffer.from(JSON.stringify(obj))
    .toString('base64')
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
}

test('patient navigation links should change route', async ({ page }) => {
  const now = Math.floor(Date.now() / 1000);
  const token = `${base64url({ alg: 'HS256', typ: 'JWT' })}.${base64url({
    exp: now + 3600,
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Patient',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'test@medio.local',
    clinic_name: 'Test Clinic',
    clinic_id: '00000000-0000-0000-0000-000000000001'
  })}.sig`;

  await page.goto('http://localhost:4200/login');
  await page.evaluate((value) => localStorage.setItem('token', value), token);

  await page.goto('http://localhost:4200/dashboard/patient');

  await page.getByRole('link', { name: 'Profile' }).click();
  await expect(page).toHaveURL(/\/dashboard\/patient\/profile/);

  await page.getByRole('link', { name: 'Settings' }).click();
  await expect(page).toHaveURL(/\/dashboard\/patient\/settings/);

  await page.getByRole('link', { name: 'Book' }).click();
  await expect(page).toHaveURL(/\/dashboard\/appointments\/book/);

  await page.getByRole('link', { name: 'Dashboard' }).click();
  await expect(page).toHaveURL(/\/dashboard\/patient$/);
});
