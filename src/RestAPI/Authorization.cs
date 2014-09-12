﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using TwitterOAuth.RestAPI.Models;

namespace TwitterOAuth.RestAPI
{
    public class Authorization
    {
        public Secret Secret { get; private set; }

        public Authorization(Secret secret)
        {
            Secret = secret;
        }

        public string GetHeader(Uri uri)
        {
            var nonce = Guid.NewGuid().ToString();
            var timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(CultureInfo.InvariantCulture);

            var oauthParameters = new SortedDictionary<string, string>
            {
                {"oauth_consumer_key", Secret.ApiKey},
                {"oauth_nonce", nonce},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_timestamp", timestamp},
                {"oauth_token", Secret.AccessToken},
                {"oauth_version", "1.0"}
            };

            var signingParameters = new SortedDictionary<string, string>(oauthParameters);

            var query = HttpUtility.ParseQueryString(uri.Query);
            foreach (var k in query.AllKeys)
            {
                signingParameters.Add(k, query[k]);
            }

            var builder = new UriBuilder(uri) { Query = "" };
            var baseUrl = builder.Uri.AbsoluteUri;

            var parameterString = string.Join("&",
                                    from k in signingParameters.Keys
                                    select Uri.EscapeDataString(k) + "=" +
                                           Uri.EscapeDataString(signingParameters[k]));

            var stringToSign = string.Join("&", new[] {"GET", baseUrl, parameterString}.Select(Uri.EscapeDataString));
            var signingKey = Secret.ApiSecret + "&" + Secret.AccessTokenSecret;
            var signature = Convert.ToBase64String(new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)).ComputeHash(Encoding.ASCII.GetBytes(stringToSign)));

            oauthParameters.Add("oauth_signature", signature);

            var authHeader = string.Join(", ", from k in oauthParameters.Keys
                                               select string.Format(@"{0}=""{1}""",
                                                   Uri.EscapeDataString(k),
                                                   Uri.EscapeDataString(oauthParameters[k])));

            return authHeader;
        }
    }
}
