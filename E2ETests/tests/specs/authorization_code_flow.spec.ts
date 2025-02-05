import { test, expect, request } from '@playwright/test';

test.describe.serial('認可コードフローのテストをします', () => {

  test('認可コードを取得します', async ({ page }) => {
    const tokenRequest = await request.newContext();
    await page.goto('https://localhost:8081/authorization?client_id=client_id&provider_name=amazon-oauth2&redirect_uri=https%3a%2f%2flocalhost%3a8081%2fauth%2fcallback');
    await expect(page).toHaveURL(/na\.account\.amazon\.com/);
    await page.getByRole('textbox', { name: 'Email or mobile phone number' }).fill('ohkouchi@skirnir.dev');
    await page.getByRole('button', { name: 'Continue' }).click();
    await page.getByRole('textbox', { name: 'Password' }).fill('wkexk.950');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await page.pause();
    const url = new URL(page.url());
    const response = await tokenRequest.post('https://api.amazon.co.jp/auth/o2/token', {
      data: {
        client_id: 'amzn1.application-oa2-client.8178bf9a24044f3d83c664f0f99c38e5',
        client_secret: '9ebf3d0a7139a5e20b758c8d94a7651c9d385757c0b91c82b48ddfb9749c1be0',
        code: url.searchParams.get('code'),
        scope: 'profile postal_code profile:user_id',
        redirect_uri: 'https://localhost:8081/auth/callback',
        grant_type: 'authorization_code'
      }
    });
    console.log(await response.json());
    await page.pause();
  });
});
