# JWT Authentication for Gateway

## Overview

JWT Bearer authentication has been successfully implemented for the YARP Gateway service. This implementation ensures all API routes are protected while allowing anonymous access to health check endpoints.

## Implementation Details

### Package Added
- **Microsoft.AspNetCore.Authentication.JwtBearer** (version 9.0.10)

### Configuration

#### appsettings.json
```json
"Authentication": {
  "Jwt": {
    "Issuer": "http://localhost:5000",
    "Audience": "coding-agent-api",
    "SecretKey": "CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG"
  }
}
```

**⚠️ IMPORTANT**: Change the `SecretKey` to a secure, randomly generated string in production environments. The key should be at least 32 characters long.

### Features Implemented

1. **JWT Bearer Authentication**: All requests to protected routes must include a valid JWT token in the `Authorization` header
2. **Anonymous Access for Health Endpoint**: `/health` endpoint is accessible without authentication for monitoring purposes
3. **Protected API Routes**: Both `/api/chat/*` and `/api/orchestration/*` routes require authentication
4. **Token Validation**: Validates issuer, audience, lifetime, and signing key
5. **Header Propagation**: YARP automatically forwards the `Authorization` header to downstream services via the `RequestHeaderOriginalHost` transform

## Testing

### Prerequisites
- .NET 9.0 SDK
- Python 3 with PyJWT library (for token generation): `pip install PyJWT`

### Running the Gateway
```bash
cd src/Gateway/CodingAgent.Gateway
dotnet run
```

### Test Scripts

#### Bash/Linux/macOS
See the test script in `/tmp/test_jwt_auth.sh` for comprehensive testing.

#### PowerShell/Windows
See the test script in `/tmp/test_jwt_auth.ps1` for comprehensive testing.

### Manual Testing

#### 1. Test Health Endpoint (No Auth Required)
```bash
curl -i http://localhost:5000/health
```
Expected: HTTP 200 OK

#### 2. Test Protected Route Without Token
```bash
curl -i http://localhost:5000/api/chat/conversations
```
Expected: HTTP 401 Unauthorized with `WWW-Authenticate: Bearer` header

#### 3. Generate a Valid JWT Token
```python
import jwt
import datetime

secret_key = "CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG"
issuer = "http://localhost:5000"
audience = "coding-agent-api"

payload = {
    "sub": "test-user-id",
    "name": "Test User",
    "email": "test@example.com",
    "iat": datetime.datetime.now(datetime.UTC),
    "exp": datetime.datetime.now(datetime.UTC) + datetime.timedelta(hours=1),
    "iss": issuer,
    "aud": audience
}

token = jwt.encode(payload, secret_key, algorithm="HS256")
print(token)
```

#### 4. Test Protected Route With Valid Token
```bash
TOKEN="<your-token-here>"
curl -i -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/chat/conversations
```
Expected: HTTP 502 Bad Gateway (authentication passed, but backend service not running)

#### 5. Test With Invalid Token
```bash
curl -i -H "Authorization: Bearer invalid.token" http://localhost:5000/api/chat/conversations
```
Expected: HTTP 401 Unauthorized with `WWW-Authenticate: Bearer error="invalid_token"` header

## Test Results

All tests passed successfully:

✅ Health endpoint is accessible without authentication (HTTP 200)
✅ Protected routes require authentication (HTTP 401 without token)
✅ Invalid tokens are rejected (HTTP 401)
✅ Valid tokens pass authentication successfully (HTTP 502 - backend not running)

## Security Considerations

1. **Secret Key**: The JWT secret key in `appsettings.json` is a placeholder. In production:
   - Use a strong, randomly generated key (at least 32 characters)
   - Store it securely (Azure Key Vault, AWS Secrets Manager, or environment variables)
   - Never commit the production secret key to source control

2. **HTTPS**: In production, always use HTTPS to prevent token interception

3. **Token Expiration**: Tokens are validated for expiration. Ensure clients handle token refresh appropriately

4. **Issuer/Audience Validation**: The implementation validates both issuer and audience claims to prevent token misuse

## Next Steps

1. **Integration with Identity Provider**: Consider integrating with an identity provider (Azure AD, Auth0, etc.) for production use
2. **Token Refresh Mechanism**: Implement a token refresh endpoint for long-lived sessions
3. **Rate Limiting**: Add rate limiting per user/IP as specified in the architecture documentation
4. **Logging**: Add structured logging for authentication events (successful/failed attempts)

## References

- [ASP.NET Core JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- Project Documentation: `docs/01-SERVICE-CATALOG.md` section 1.2
