using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace JwtHelpers
{
	public static class JwtHelpers
	{
		public static string GetToken(HttpContext httpContext)
		{
			var token = httpContext.Request.Headers["Authorization"].ToString().Split(' ').LastOrDefault();
			if (string.IsNullOrWhiteSpace(token))
			{
				throw new ArgumentException("Missing bearer token");
			}

			return token;
		}

		public static async Task ValidateToken(string token, string audience, string issuer)
		{
			var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
				$"{issuer}/.well-known/openid-configuration",
				new OpenIdConnectConfigurationRetriever());
			var configuration = await configurationManager.GetConfigurationAsync();

			var tokenParams = new TokenValidationParameters
			{
				RequireSignedTokens = true,
				ValidAudience = audience,
				ValidateAudience = true,
				ValidIssuer = issuer,
				ValidateIssuer = true,
				ValidateIssuerSigningKey = true,
				ValidateLifetime = true,
				IssuerSigningKeys = configuration.SigningKeys,
			};

			var handler = new JwtSecurityTokenHandler();
			handler.ValidateToken(token, tokenParams, out _);
		}
	}
}
