using LibraryManagement.API.Configuration; // Namespace for JwtSettings
using LibraryManagement.API.Services.Implementations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LibraryManagement.API.Tests.Services
{
    [TestFixture]
    public class TokenServiceTests
    {
        private JwtSettings _jwtSettings;
        private TokenService _tokenService;

        // Helper method to generate a sufficiently long and complex secret key for tests
        //private string GenerateTestSecretKey()
        //{
        //    return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64 chars, > 32 bytes
        //}

        [SetUp]
        public void SetUp()
        {
            _jwtSettings = new JwtSettings
            {
                // THAY ĐỔI Ở ĐÂY: Sử dụng một secret key tĩnh, đơn giản và đủ dài
                SecretKey = "MySuperSecureTestKeyThatIsLongEnoughForHS256!12345", // > 32 ký tự ASCII
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                AccessTokenExpirationMinutes = 15
            };
            _tokenService = new TokenService(_jwtSettings);
        }

        #region GenerateAccessToken Tests

        [Test]

        public void GenerateAccessToken_NullClaims_StillGeneratesTokenWithNoSubjectClaims()
        {
            // Arrange
            IEnumerable<Claim> claims = null; // Service's new ClaimsIdentity(claims) handles null

            // Act
            var (accessToken, jti) = _tokenService.GenerateAccessToken(claims);

            // Assert
            // User believes JTI should be null/empty if input claims are null.
            // Standard library behavior is that token.Id (JTI) is auto-generated (e.g., a GUID)
            // and will not be null or empty. These assertions will likely fail if the library
            // behaves as standard, meaning 'jti' will have a value.
            Assert.IsNull(jti, "User expects JTI to be null when input claims are null.");

            // The original error reported by the user was that 'accessToken' was null.
            // If 'accessToken' is still null, the test will fail here or on the next line.
            Assert.IsNotNull(accessToken, "Access Token should not be null. If it is, token signing/writing failed silently.");
            Assert.IsNotEmpty(accessToken, "Access Token should not be empty.");

            var handler = new JwtSecurityTokenHandler();
            // This check is good practice before attempting to read the token.
            Assert.IsTrue(handler.CanReadToken(accessToken), "Token Handler must be able to read the generated accessToken.");
            var decodedToken = handler.ReadJwtToken(accessToken);

            Assert.AreEqual(_jwtSettings.Issuer, decodedToken.Issuer, "Issuer should match settings.");
            Assert.IsTrue(decodedToken.Audiences.Contains(_jwtSettings.Audience), "Audience should match settings.");

            // If the above assertions for jti (IsNull, IsEmpty) pass because jti is indeed null/empty,
            // then the following assertions comparing jti with decodedToken.Id will also need to align with jti being null/empty.
            // However, decodedToken.Id (which is the 'jti' claim from the token payload) will also likely be a generated GUID.
            Assert.AreEqual(jti, decodedToken.Id, "Token JTI (Id) should match the returned JTI (which user expects to be null/empty).");
            Assert.AreEqual(jti, decodedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value, "JTI claim in token should match (user expects null/empty).");

            // Verify that only standard claims (iat, nbf, exp, jti, iss, aud) are present
            // when the input 'claims' is null.
            var subjectClaims = decodedToken.Claims.Where(c =>
                c.Type != JwtRegisteredClaimNames.Nbf &&
                c.Type != JwtRegisteredClaimNames.Exp &&
                c.Type != JwtRegisteredClaimNames.Iat &&
                c.Type != JwtRegisteredClaimNames.Jti && // JTI is a standard claim, so it should be here if generated.
                c.Type != JwtRegisteredClaimNames.Iss &&
                c.Type != JwtRegisteredClaimNames.Aud).ToList();
            Assert.IsEmpty(subjectClaims, "There should be no subject-specific claims when input claims are null.");
        }

        [Test]
        public void GenerateAccessToken_WhenSecretKeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            _jwtSettings.SecretKey = null;
            // Re-initialize service with modified settings
            var tokenServiceWithNullKey = new TokenService(_jwtSettings);
            var claims = new List<Claim> { new Claim("type", "value") };

            // Act & Assert
            // Encoding.UTF8.GetBytes(null) throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => tokenServiceWithNullKey.GenerateAccessToken(claims));
        }

        [Test]
        public void GenerateAccessToken_WhenSecretKeyIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            _jwtSettings.SecretKey = ""; // Empty key
            var tokenServiceWithEmptyKey = new TokenService(_jwtSettings);
            var claims = new List<Claim> { new Claim("type", "value") };

            // Act & Assert
            // SymmetricSecurityKey constructor throws if key length is too small (e.g. ArgumentException for 0 length)
            // "IDX10703: Unable to create a 'System.IdentityModel.Tokens.SymmetricSecurityKey', key length is zero."
            Assert.Throws<ArgumentException>(() => tokenServiceWithEmptyKey.GenerateAccessToken(claims), "Key length must be greater than 0.");
        }

        [Test]
        public void GenerateAccessToken_WhenSecretKeyIsTooShort_ThrowsArgumentOutOfRangeExceptionOrArgumentException()
        {
            // Arrange
            _jwtSettings.SecretKey = "short"; // Too short for HMACSHA256 which usually wants at least 16 bytes (128 bits)
            var tokenServiceWithShortKey = new TokenService(_jwtSettings);
            var claims = new List<Claim> { new Claim("type", "value") };

            // Act & Assert
            // SymmetricSecurityKey might throw if key is too short for algorithm.
            // For HMACSHA256, it might accept shorter keys but it's not secure.
            // The actual error can be:
            // "IDX10603: The algorithm 'HS256' requires the key size to be greater than '128' bits. Key 'System.Byte[]' has size 'XXX'."
            // Let's check for general ArgumentException or specific SecurityTokenInvalidSigningKeyException if it wraps.
            // Microsoft.IdentityModel.Tokens.SecurityKey.EnsureMinKeySize usually throws ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => tokenServiceWithShortKey.GenerateAccessToken(claims));
        }


        #endregion

        #region GenerateRefreshToken Tests

        [Test]
        public void GenerateRefreshToken_ReturnsNonEmptyString()
        {
            // Act
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Assert
            Assert.IsNotNull(refreshToken);
            Assert.IsNotEmpty(refreshToken);
        }

        [Test]
        public void GenerateRefreshToken_ReturnsBase64String()
        {
            // Act
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Assert
            Assert.DoesNotThrow(() => Convert.FromBase64String(refreshToken), "Refresh token should be a valid Base64 string.");
        }

        [Test]
        public void GenerateRefreshToken_ConsecutiveCallsReturnDifferentTokens()
        {
            // Act
            var token1 = _tokenService.GenerateRefreshToken();
            var token2 = _tokenService.GenerateRefreshToken();

            // Assert
            Assert.AreNotEqual(token1, token2);
        }

        [Test]
        public void GenerateRefreshToken_HasCorrectLength()
        {
            // Arrange
            // byte[64] -> Base64 string length = Math.Ceiling(64 / 3.0) * 4 = 21.333 * 4 = Math.Ceiling(21.333) * 4 (incorrect calc)
            // Correct: ((4 * n / 3) + 3) & ~3. For n=64: ((4 * 64 / 3) + 3) & ~3 = (85 + 3) & ~3 = 88 & ~3 = 88
            int expectedLength = 88;

            // Act
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Assert
            Assert.AreEqual(expectedLength, refreshToken.Length);
        }

        #endregion

        #region GetPrincipalFromExpiredToken Tests

        private string GenerateTestToken(IEnumerable<Claim> claims, JwtSettings settings, TimeSpan? expiry = null, string signingAlgorithm = SecurityAlgorithms.HmacSha256, SecurityKey signingKey = null)
        {
            var actualSigningKey = signingKey ?? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = settings.Issuer,
                Audience = settings.Audience,
                Expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(settings.AccessTokenExpirationMinutes)),
                SigningCredentials = new SigningCredentials(actualSigningKey, signingAlgorithm),
                Subject = new ClaimsIdentity(claims)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        [Test]
        public void GetPrincipalFromExpiredToken_WithValidNonExpiredToken_ReturnsClaimsPrincipal()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user456") };
            var nonExpiredToken = GenerateTestToken(claims, _jwtSettings, TimeSpan.FromMinutes(5)); // Token expires in 5 minutes

            // Act
            var principal = _tokenService.GetPrincipalFromExpiredToken(nonExpiredToken);

            // Assert
            Assert.IsNotNull(principal);
            Assert.AreEqual("user456", principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
        }

        [Test]
        public void GetPrincipalFromExpiredToken_WithMalformedToken_ReturnsNull()
        {
            // Arrange
            var malformedToken = "this.is.not.a.jwt";

            // Act
            var principal = _tokenService.GetPrincipalFromExpiredToken(malformedToken);

            // Assert
            Assert.IsNull(principal);
        }

        //[Test]
        //public void GetPrincipalFromExpiredToken_WithTokenSignedByDifferentAlgorithm_ReturnsNull()
        //{
        //    // Arrange
        //    var claims = new List<Claim> { new Claim("sub", "test") };
        //    // For this test, we'd ideally need to generate a token with a different algo.
        //    // Simpler: Let's assume a token was somehow generated with a different algorithm but passes initial validation
        //    // then fails the explicit Alg check in the service.
        //    // To test this specific check: create a token with HS256, then try to validate as if it was, say, None (not possible directly with helper)
        //    // Or, create a token that the handler *can* read but has wrong alg in header after reading
        //    // This specific check in service `!jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256...` is tricky to isolate if ValidateToken already fails
        //    // A better way: generate a valid token, then manually tamper its header if possible, or generate a token specifically with a different algorithm.
        //    // For simplicity, if ValidateToken already catches algo mismatch, this is covered.
        //    // If ValidateToken succeeds but algo is different (e.g. "none" alg if allowed by some mistake in validation params, which it isn't here), then the service's check is useful.

        //    // Test the explicit check:
        //    // Create a token where ValidateToken *might* pass if algo check was loose, but our service tightens it.
        //    // Here, we'll make a token that IS HmacSha256, so the internal check should pass,
        //    // and if ValidateToken also passes, we get a principal.
        //    // To make it fail the service's specific check, we'd have to mock JwtSecurityToken.Header.Alg or craft such a token.
        //    // Let's assume ValidateToken itself already ensures the algorithm from the token is consistent with the key.
        //    // If ValidateToken succeeds and the algorithm is NOT HmacSha256, SecurityToken will be a JwtSecurityToken, but the check will fail.

        //    // This test becomes more about if the token passed basic validation but had wrong algo.
        //    // The `ValidateToken` itself will likely fail if the algorithm doesn't match the key type or if it's 'none' and not allowed.
        //    // The explicit check in `GetPrincipalFromExpiredToken` is an additional safeguard.
        //    // Let's try to create a token with a different Symmetric algo, e.g. HmacSha512
        //    var differentAlgoSettings = new JwtSettings
        //    {
        //        SecretKey = _jwtSettings.SecretKey + GenerateTestSecretKey(), // Longer key for HS512
        //        Issuer = _jwtSettings.Issuer,
        //        Audience = _jwtSettings.Audience
        //    };
        //    var tokenWithDifferentAlgo = GenerateTestToken(claims, differentAlgoSettings, TimeSpan.FromMinutes(-5), SecurityAlgorithms.HmacSha512);


        //    // Act
        //    var principal = _tokenService.GetPrincipalFromExpiredToken(tokenWithDifferentAlgo);

        //    // Assert
        //    // This will fail at `ValidateToken` because the service's TokenValidationParameters expects HS256 due to its own SymmetricSecurityKey.
        //    // If ValidateToken were to magically pass with a token signed by HS512 against an HS256 key (it won't),
        //    // then the `if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256...`
        //    // check would catch it. The current test will result in null because ValidateToken fails due to signature mismatch.
        //    // To purely test the `!jwtSecurityToken.Header.Alg.Equals` part, one would need to mock `tokenHandler.ValidateToken`
        //    // to return a `JwtSecurityToken` with a different algorithm in its header.
        //    // Given the current structure, a signature failure due to algorithm/key mismatch is the expected outcome.
        //    Assert.IsNull(principal, "Token signed with a different algorithm should not be validated by parameters expecting HS256.");
        //}


        [Test]
        public void GetPrincipalFromExpiredToken_NullTokenString_ReturnsNull()
        {
            // Act
            var principal = _tokenService.GetPrincipalFromExpiredToken(null);

            // Assert
            Assert.IsNull(principal); // ValidateToken likely throws ArgumentNullException, caught by service
        }

        [Test]
        public void GetPrincipalFromExpiredToken_EmptyTokenString_ReturnsNull()
        {
            // Act
            var principal = _tokenService.GetPrincipalFromExpiredToken("");

            // Assert
            Assert.IsNull(principal); // ValidateToken likely throws ArgumentException, caught by service
        }

        #endregion
    }
}