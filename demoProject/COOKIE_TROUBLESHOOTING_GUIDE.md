# Cookie Authentication Troubleshooting Guide

## Fixes Applied

### 1. Cookie Configuration Issues Fixed
- **SameSite=None + Secure=true**: Updated cookie configuration to properly handle cross-origin requests
- **Development vs Production**: Different SameSite modes for different environments
- **Explicit Path**: Added explicit cookie path to ensure proper cookie scope

### 2. CORS Configuration Improved
- Removed conflicting manual CORS headers
- Environment-specific CORS origins
- Proper credentials handling

### 3. HTTPS Configuration
- Always enforce HTTPS redirection for secure cookie handling
- Updated launch settings for proper HTTPS development

## Key Changes Made

### AuthController.cs
```csharp
private CookieOptions GetCookieOptions()
{
    var isProduction = _environment.IsProduction();
    return new CookieOptions
    {
        HttpOnly = true,
        Secure = true, // Always true for cross-origin scenarios
        SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
        Expires = DateTime.UtcNow.AddDays(7),
        Path = "/",
        Domain = isProduction ? null : null
    };
}
```

### Program.cs
- Environment-specific CORS origins
- Removed conflicting CORS headers
- Always enforce HTTPS redirection

## Testing Instructions

### Development Environment
1. **Run the application using HTTPS profile**: Use `https://localhost:5000`
2. **Frontend must also use HTTPS**: Update your React app to run on `https://localhost:5173`
3. **Test cookie setting**: Login and check browser dev tools ? Application ? Cookies
4. **Test cookie reading**: Make authenticated requests and verify the cookie is sent

### Production Environment
1. **Update CORS origins**: Replace placeholder URLs in Program.cs with your actual domain
2. **Update appsettings.Production.json**: Set proper connection strings and secrets
3. **Ensure HTTPS**: Production must run on HTTPS for cookies to work properly

## Browser Dev Tools Verification

### Check if cookies are being set:
1. Open browser Developer Tools (F12)
2. Go to Application tab ? Cookies
3. Look for `auth_token` cookie with these properties:
   - HttpOnly: ?
   - Secure: ?
   - SameSite: Lax (dev) or None (prod)
   - Path: /

### Check if cookies are being sent:
1. Go to Network tab
2. Make an authenticated request
3. Check request headers for `Cookie: auth_token=...`
4. Verify the middleware converts it to `Authorization: Bearer ...`

## Common Issues and Solutions

### Issue: Cookie not visible in browser
**Cause**: HttpOnly flag prevents JavaScript access
**Solution**: This is expected - use Network tab to verify cookie is sent

### Issue: Cookie not set in cross-origin scenario
**Cause**: SameSite=None requires Secure=true
**Solution**: Already fixed - ensure both frontend and backend use HTTPS

### Issue: CORS errors
**Cause**: Mismatched origins or missing credentials flag
**Solution**: Verify frontend URL matches CORS policy and uses `credentials: 'include'`

### Issue: Cookie not sent with requests
**Cause**: Frontend not including credentials or wrong domain
**Solution**: Frontend must use `credentials: 'include'` in fetch/axios requests

## Frontend Integration

### Axios Configuration
```javascript
axios.defaults.withCredentials = true;
```

### Fetch API
```javascript
fetch('/api/endpoint', {
    credentials: 'include'
});
```

### React Development Server (for HTTPS)
Add to your React app's package.json:
```json
{
  "scripts": {
    "start": "HTTPS=true react-scripts start"
  }
}
```

Or create `.env` file in React app root:
```
HTTPS=true
```