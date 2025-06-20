import { test, expect, APIRequestContext } from '@playwright/test';
import { URLSearchParams } from 'url';

// Test configuration
const ECAUTH_BASE = 'http://localhost:8081';
const MOCK_IDP_BASE = 'http://localhost:9091';
const ECAUTH_CLIENT_ID = 'test-ecauth-client';
const MOCK_CLIENT_ID = 'test-mock-federate-client';

test.describe.serial('User Management Flow', () => {
  let request: APIRequestContext;
  let authorizationCode: string;
  let ecAuthCode: string;
  let idToken: string;
  let accessToken: string;

  test.beforeAll(async ({ playwright }) => {
    request = await playwright.request.newContext({
      ignoreHTTPSErrors: true,
      httpCredentials: {
        username: 'alice',
        password: 'password'
      }
    });
  });

  test.afterAll(async () => {
    await request.dispose();
  });

  test('Step 1: Start authorization flow with EcAuth', async () => {
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: ECAUTH_CLIENT_ID,
      redirect_uri: 'https://example.com/callback',
      scope: 'openid profile email',
      state: 'test-state-123',
      nonce: 'test-nonce-456'
    });

    const response = await request.get(`${ECAUTH_BASE}/authorization?${params.toString()}`);
    
    // EcAuth should redirect to MockIdP for authentication
    expect(response.status()).toBe(200);
    const redirectUrl = response.url();
    expect(redirectUrl).toContain(MOCK_IDP_BASE);
    expect(redirectUrl).toContain('/authorization');
    
    // Extract the state parameter from MockIdP redirect
    const mockIdpUrl = new URL(redirectUrl);
    const mockIdpState = mockIdpUrl.searchParams.get('state');
    expect(mockIdpState).toBeTruthy();
    
    // Follow the redirect to MockIdP
    const mockIdpResponse = await request.get(redirectUrl);
    expect(mockIdpResponse.status()).toBe(302);
    
    // MockIdP should redirect back with authorization code
    const callbackUrl = new URL(mockIdpResponse.headers()['location']);
    expect(callbackUrl.hostname).toBe('localhost');
    expect(callbackUrl.port).toBe('8081');
    expect(callbackUrl.pathname).toBe('/authorization-callback');
    
    authorizationCode = callbackUrl.searchParams.get('code')!;
    expect(authorizationCode).toBeTruthy();
  });

  test('Step 2: EcAuth receives callback and creates/retrieves user', async () => {
    // EcAuth processes the callback from MockIdP
    const callbackUrl = `${ECAUTH_BASE}/authorization-callback?code=${authorizationCode}&state=test-state-123`;
    const response = await request.get(callbackUrl);
    
    // Should redirect back to client with new authorization code
    expect(response.status()).toBe(302);
    const clientRedirectUrl = new URL(response.headers()['location']);
    expect(clientRedirectUrl.hostname).toBe('example.com');
    expect(clientRedirectUrl.pathname).toBe('/callback');
    
    ecAuthCode = clientRedirectUrl.searchParams.get('code')!;
    expect(ecAuthCode).toBeTruthy();
    expect(ecAuthCode).not.toBe(authorizationCode); // Should be different code
  });

  test('Step 3: Exchange EcAuth authorization code for tokens', async () => {
    const params = new URLSearchParams({
      grant_type: 'authorization_code',
      code: ecAuthCode,
      redirect_uri: 'https://example.com/callback',
      client_id: ECAUTH_CLIENT_ID
    });

    const response = await request.post(`${ECAUTH_BASE}/token`, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'Authorization': 'Basic ' + Buffer.from(`${ECAUTH_CLIENT_ID}:secret`).toString('base64')
      },
      data: params.toString()
    });

    expect(response.status()).toBe(200);
    const tokenResponse = await response.json();
    
    expect(tokenResponse).toHaveProperty('access_token');
    expect(tokenResponse).toHaveProperty('id_token');
    expect(tokenResponse).toHaveProperty('token_type', 'Bearer');
    expect(tokenResponse).toHaveProperty('expires_in');
    expect(tokenResponse).toHaveProperty('scope');
    
    idToken = tokenResponse.id_token;
    accessToken = tokenResponse.access_token;
  });

  test('Step 4: Verify ID token contains user information', async () => {
    // Parse JWT without verification (for testing)
    const idTokenParts = idToken.split('.');
    expect(idTokenParts).toHaveLength(3);
    
    const payload = JSON.parse(Buffer.from(idTokenParts[1], 'base64').toString());
    
    // Verify required OIDC claims
    expect(payload).toHaveProperty('iss', `${ECAUTH_BASE}/test-tenant`);
    expect(payload).toHaveProperty('sub'); // EcAuth user subject
    expect(payload).toHaveProperty('aud', ECAUTH_CLIENT_ID);
    expect(payload).toHaveProperty('exp');
    expect(payload).toHaveProperty('iat');
    expect(payload).toHaveProperty('nonce', 'test-nonce-456');
    
    // Verify user claims if profile scope was requested
    expect(payload).toHaveProperty('name');
    
    // Email claim should be present if email scope was requested
    // Note: EcAuth stores hashed email, so actual email might not be in token
  });

  test('Step 5: Verify same user is returned on subsequent logins', async () => {
    // Start another authorization flow
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: ECAUTH_CLIENT_ID,
      redirect_uri: 'https://example.com/callback',
      scope: 'openid profile',
      state: 'test-state-789',
      nonce: 'test-nonce-012'
    });

    // Go through the flow again
    const authResponse = await request.get(`${ECAUTH_BASE}/authorization?${params.toString()}`);
    const mockIdpRedirect = authResponse.url();
    
    const mockIdpResponse = await request.get(mockIdpRedirect);
    const callbackUrl = new URL(mockIdpResponse.headers()['location']);
    const newAuthCode = callbackUrl.searchParams.get('code')!;
    
    const ecAuthCallbackResponse = await request.get(
      `${ECAUTH_BASE}/authorization-callback?code=${newAuthCode}&state=test-state-789`
    );
    const clientRedirect = new URL(ecAuthCallbackResponse.headers()['location']);
    const newEcAuthCode = clientRedirect.searchParams.get('code')!;
    
    // Exchange for tokens
    const tokenParams = new URLSearchParams({
      grant_type: 'authorization_code',
      code: newEcAuthCode,
      redirect_uri: 'https://example.com/callback',
      client_id: ECAUTH_CLIENT_ID
    });

    const tokenResponse = await request.post(`${ECAUTH_BASE}/token`, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'Authorization': 'Basic ' + Buffer.from(`${ECAUTH_CLIENT_ID}:secret`).toString('base64')
      },
      data: tokenParams.toString()
    });

    const newTokens = await tokenResponse.json();
    const newIdToken = newTokens.id_token;
    
    // Parse both ID tokens
    const originalPayload = JSON.parse(Buffer.from(idToken.split('.')[1], 'base64').toString());
    const newPayload = JSON.parse(Buffer.from(newIdToken.split('.')[1], 'base64').toString());
    
    // Verify same user subject is returned
    expect(newPayload.sub).toBe(originalPayload.sub);
    expect(newPayload.name).toBe(originalPayload.name);
  });

  test('Step 6: Verify authorization code cannot be reused', async () => {
    // Try to use the same authorization code again
    const params = new URLSearchParams({
      grant_type: 'authorization_code',
      code: ecAuthCode, // Reuse the code from Step 3
      redirect_uri: 'https://example.com/callback',
      client_id: ECAUTH_CLIENT_ID
    });

    const response = await request.post(`${ECAUTH_BASE}/token`, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'Authorization': 'Basic ' + Buffer.from(`${ECAUTH_CLIENT_ID}:secret`).toString('base64')
      },
      data: params.toString()
    });

    expect(response.status()).toBe(400);
    const errorResponse = await response.json();
    expect(errorResponse).toHaveProperty('error', 'invalid_grant');
  });

  test('Step 7: Verify different external users get different EcAuth users', async () => {
    // Create a new request context with different credentials
    const bobRequest = await request.context().playwright.request.newContext({
      ignoreHTTPSErrors: true,
      httpCredentials: {
        username: 'bob',
        password: 'password'
      }
    });

    try {
      // Start authorization flow for Bob
      const params = new URLSearchParams({
        response_type: 'code',
        client_id: ECAUTH_CLIENT_ID,
        redirect_uri: 'https://example.com/callback',
        scope: 'openid profile email',
        state: 'bob-state-123',
        nonce: 'bob-nonce-456'
      });

      // Go through the flow with Bob's credentials
      const authResponse = await bobRequest.get(`${ECAUTH_BASE}/authorization?${params.toString()}`);
      const mockIdpResponse = await bobRequest.get(authResponse.url());
      const callbackUrl = new URL(mockIdpResponse.headers()['location']);
      const bobAuthCode = callbackUrl.searchParams.get('code')!;
      
      const ecAuthCallbackResponse = await bobRequest.get(
        `${ECAUTH_BASE}/authorization-callback?code=${bobAuthCode}&state=bob-state-123`
      );
      const clientRedirect = new URL(ecAuthCallbackResponse.headers()['location']);
      const bobEcAuthCode = clientRedirect.searchParams.get('code')!;
      
      // Exchange for tokens
      const tokenParams = new URLSearchParams({
        grant_type: 'authorization_code',
        code: bobEcAuthCode,
        redirect_uri: 'https://example.com/callback',
        client_id: ECAUTH_CLIENT_ID
      });

      const tokenResponse = await bobRequest.post(`${ECAUTH_BASE}/token`, {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Authorization': 'Basic ' + Buffer.from(`${ECAUTH_CLIENT_ID}:secret`).toString('base64')
        },
        data: tokenParams.toString()
      });

      const bobTokens = await tokenResponse.json();
      const bobIdToken = bobTokens.id_token;
      
      // Parse ID tokens
      const alicePayload = JSON.parse(Buffer.from(idToken.split('.')[1], 'base64').toString());
      const bobPayload = JSON.parse(Buffer.from(bobIdToken.split('.')[1], 'base64').toString());
      
      // Verify different users have different subjects
      expect(bobPayload.sub).not.toBe(alicePayload.sub);
      expect(bobPayload.name).not.toBe(alicePayload.name);
    } finally {
      await bobRequest.dispose();
    }
  });
});