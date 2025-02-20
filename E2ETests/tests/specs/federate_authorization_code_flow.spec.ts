import { test, expect, request } from '@playwright/test';

test.describe.serial('認可コードフローフェデレーションのテストをします', () => {

  const authorizationEndpoint = 'https://localhost:8081/authorization';
  const tokenEndpoint = 'https://localhost:8081/token';
  const userInfoEndpoint = 'https://localhost:9091/userinfo';
  const redirectUri = 'https://localhost:8081/auth/callback';
  const clientId = 'client_id';
  const clientSecret = 'client_secret';
  const scopes = 'openid profile email';
  const providerName = 'federate-oauth2';
  const state = 'state';

  test.use({
    httpCredentials: {
      username: 'defaultuser@example.com',
      password: 'password',
    },
  });

  test('フェデレーションをテストをします', async ({ page }) => {
    const tokenRequest = await request.newContext();
    const authUrl = `${authorizationEndpoint}?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=${encodeURIComponent(scopes)}&provider_name=${providerName}&state=${state}`;
    console.log(authUrl);
    await page.goto(authUrl);
    await expect(page).toHaveURL(/auth\/callback/);
    const url = new URL(page.url());
    console.log(`url:${url}`);
    console.log(`code: ${url.searchParams.get('code')}`);
    console.log(`state: ${url.searchParams.get('state')}`);
    const response = await tokenRequest.post(tokenEndpoint, {
      form: {
        client_id: clientId,
        client_secret: clientSecret,
        code: url.searchParams.get('code') ?? '',
        scope: scopes,
        redirect_uri: redirectUri,
        grant_type: 'authorization_code',
        state: (url.searchParams.get('state') ?? '').replace(/ /g, '+')
      }
    });
    console.log(await response.json());
    expect((await response.json()).access_token).toBeTruthy();
    expect((await response.json()).token_type).toBe('Bearer');

    const userInfoRequest = await request.newContext();
    const userInfoResponse = await userInfoRequest.get(userInfoEndpoint, {
      headers: {
        Authorization: `Bearer ${(await response.json()).access_token}`
      }
    });

    console.log(await userInfoResponse.json());
    expect((await userInfoResponse.json()).sub).toBeTruthy();
  });
});
