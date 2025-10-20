# Cookie-Based Authentication Implementation

## Overview

This project has been updated to use HTTP-only cookies for authentication instead of sending JWT tokens directly to the frontend. This approach provides better security by preventing XSS attacks from accessing the authentication token.

## Changes Made

### 1. AuthController Updates

- **Modified `AuthResponse` DTO**: Removed the `Token` property and added a `Message` property
- **Updated Login/Register endpoints**: Now set HTTP-only cookies instead of returning tokens
- **Added Logout endpoint**: Clears the authentication cookie
- **Added `/api/auth/me` endpoint**: Returns current user information

### 2. Cookie Configuration

- **HttpOnly**: `true` - Prevents JavaScript access to the cookie
- **Secure**: `true` - Required for cross-origin requests (React frontend)
- **SameSite**: `None` - Required for cross-origin cookies to work with React frontends
- **Expires**: 7 days from creation
- **Domain**: `null` - Let browser handle domain automatically for cross-origin scenarios

### 3. Middleware Implementation

Created `CookieToHeaderMiddleware` that:
- Extracts the JWT token from the `auth_token` cookie
- Adds it to the `Authorization` header as `Bearer {token}`
- Allows existing JWT validation to work seamlessly

### 4. CORS Configuration

Updated CORS policy to include `AllowCredentials()` which is required for cookie-based authentication.

## API Endpoints

### Authentication Endpoints

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| POST | `/api/auth/login` | Login user | `{ user: UserResponse, message: string }` |
| POST | `/api/auth/register` | Register user | `{ user: UserResponse, message: string }` |
| POST | `/api/auth/logout` | Logout user | `{ message: string }` |
| GET | `/api/auth/me` | Get current user | `UserResponse` |

### Cookie Details

- **Name**: `auth_token`
- **Value**: JWT token
- **HttpOnly**: Yes
- **Secure**: Yes (required for cross-origin)
- **SameSite**: None (required for React frontend cross-origin)
- **Path**: `/`
- **Expires**: 7 days

## Frontend Implementation

### Key Points for Frontend Developers

1. **Include Credentials**: Always set `credentials: 'include'` in fetch requests
2. **No Token Management**: The browser handles cookies automatically
2. **CORS**: Ensure your frontend origin is listed in the CORS policy
4. **Error Handling**: Check for 401 responses to detect expired sessions

### Example Frontend Code

```javascript
// Login request
const response = await fetch('/api/auth/login', {
    method: 'POST',
    credentials: 'include', // Important!
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({ email, password })
});

// Authenticated request
const userResponse = await fetch('/api/auth/me', {
    credentials: 'include' // Important!
});
```

### React/Axios Example

```javascript
// Configure axios to always include cookies
axios.defaults.withCredentials = true;

// Or per request
const response = await axios.post('/api/auth/login', 
    { email, password }, 
    { withCredentials: true }
);
```

## Security Benefits

1. **XSS Protection**: Tokens can't be accessed by malicious JavaScript
2. **Automatic Management**: Browser handles cookie lifecycle
3. **CSRF Protection**: SameSite=Strict prevents cross-site requests
4. **Secure Transport**: Cookies only sent over HTTPS in production

## Development Notes

- The example HTML file is available at `/cookie-auth-example.html`
- In development, cookies work over HTTP
- Ensure your frontend origin is in the CORS allowlist
- Use browser developer tools to inspect cookies

## Testing

You can test the authentication using:

1. **Swagger UI**: Available at `/swagger`
2. **Example HTML**: Available at `/cookie-auth-example.html`
3. **Postman**: Enable "Send cookies" in request settings

## Migration from Token-Based Auth

If migrating from token-based authentication:

1. Update frontend to remove token storage (localStorage/sessionStorage)
2. Remove Authorization headers from requests
3. Add `credentials: 'include'` to all authenticated requests
4. Update error handling for 401 responses
5. Use `/api/auth/me` to check authentication status on app load