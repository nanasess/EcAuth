import { test, expect, request } from '@playwright/test';

test.describe.serial('認可コードフローのテストをします', () => {

  const authorizationEndpoint = 'https://localhost:9091/authorization';
  const tokenEndpoint = 'https://localhost:9091/token';
  const userInfoEndpoint = 'https://localhost:9091/userinfo';
  const redirectUri = 'https://localhost:8081/auth/callback';
  const clientId = 'mockclientid';
  const clientSecret = 'mock-client-secret';
  const scopes = 'openid profile email';

  test.use({
    httpCredentials: {
      username: 'defaultuser@example.com',
      password: 'password',
    },
  });

  test('MockOpenIdProvider の認可コードフローをテストをします', async ({ page }) => {
    const tokenRequest = await request.newContext();

    await page.goto(`${authorizationEndpoint}?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=${encodeURIComponent(scopes)}`);
    const url = new URL(page.url());
    const response = await tokenRequest.post(tokenEndpoint, {
      form: {
        client_id: clientId,
        client_secret: clientSecret,
        code: url.searchParams.get('code') ?? '',
        scope: scopes,
        redirect_uri: redirectUri,
        grant_type: 'authorization_code'
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
