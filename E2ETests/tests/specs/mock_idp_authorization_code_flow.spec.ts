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
    expect((await response.json()).refresh_token).toBeTruthy();
    const refreshToken = (await response.json()).refresh_token;
    expect((await response.json()).token_type).toBe('Bearer');

    const userInfoRequest = await request.newContext();
    const userInfoResponse = await userInfoRequest.get(userInfoEndpoint, {
      headers: {
        Authorization: `Bearer ${(await response.json()).access_token}`
      }
    });

    console.log(await userInfoResponse.json());
    expect((await userInfoResponse.json()).sub).toBeTruthy();

    const refreshTokenResponse = await tokenRequest.post(tokenEndpoint, {
      form: {
        client_id: clientId,
        client_secret: clientSecret,
        scope: scopes,
        grant_type: 'refresh_token',
        refresh_token: refreshToken
      }
    });

    console.log(await refreshTokenResponse.json());
    expect((await refreshTokenResponse.json()).access_token).toBeTruthy();
    expect((await refreshTokenResponse.json()).refresh_token).toBeTruthy();
    expect((await refreshTokenResponse.json()).token_type).toBe('Bearer');

    // TODO: refresh_token が更新されていることを確認する
    // TODO: 古い access_token でユーザー情報を取得できないことを確認する
    test.step('Refresh Token で更新したアクセストークンでユーザー情報を取得します', async () => {
      const refreshTokenUserInfoRequest = await request.newContext();
      const refreshTokenUserInfoResponse = await refreshTokenUserInfoRequest.get(userInfoEndpoint, {
        headers: {
          Authorization: `Bearer ${(await refreshTokenResponse.json()).access_token}`
        }
      });
      console.log(await refreshTokenUserInfoResponse.json());
      expect((await refreshTokenUserInfoResponse.json()).sub).toBeTruthy();
      expect((await refreshTokenUserInfoResponse.json()).sub).toBe((await userInfoResponse.json()).sub);
    });
  });
});
