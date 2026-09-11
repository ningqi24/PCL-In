using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Authentication.Utils;
using PCL.Core.Minecraft.Profile.Authentication.Utils.Models;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

/// <summary>
/// Microsoft consumer account provider. OAuth, Xbox Live, XSTS and Minecraft service calls
/// are kept here so launch code never needs to understand the six-step protocol.
/// </summary>
public sealed class MicrosoftProvider : IAuthenticateProvider
{
    // Secrets.MSOAuthClientId 指向微软自己的旧版 MSA 应用（00000000402b5328），这类应用只注册在
    // login.live.com：送到 login.microsoftonline.com（Entra）会直接得到 AADSTS700016
    //「Application ... was not found in the directory」，连设备码都拿不到。因此这里走 live.com 的
    // MSA 端点，scope 也必须用 MSA 形式（service::...::MBI_SSL）而不是 Entra 的 XboxLive.signin。
    // 若将来改用自己注册的 Entra 应用（PCL_MS_CLIENT_ID），端点与 scope 需要一并换回。
    private const string DeviceEndpoint = "https://login.live.com/oauth20_connect.srf";
    private const string TokenEndpoint = "https://login.live.com/oauth20_token.srf";
    private const string Scope = "service::user.auth.xboxlive.com::MBI_SSL";

    private readonly string _clientId;

    public MicrosoftProvider(string? clientId = null)
    {
        _clientId = string.IsNullOrWhiteSpace(clientId) ? Secrets.MSOAuthClientId : clientId;
        if (string.IsNullOrWhiteSpace(_clientId))
            throw new InvalidOperationException("Microsoft OAuth client id is not configured.");
    }

    public async Task<DeviceCodeData> GetDeviceCodeAsync(CancellationToken token)
    {
        using var response = await HttpRequest.CreatePost(DeviceEndpoint)
            .WithContent(new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["scope"] = Scope,
                ["response_type"] = "device_code"
            }))
            .SendAsync(NetworkService.GetClient(NetworkService.MicrosoftEntraId), cancellationToken: token)
            .ConfigureAwait(false);
        var result = await response.AsJsonAsync<DeviceCodeData>(cancellationToken: token).ConfigureAwait(false)
                     ?? throw new InvalidOperationException("Microsoft device authorization response was empty.");
        if (result.IsError) throw new IdentityModelAuthenticationException(result.Error, result.ErrorDescription);
        result.Validate();
        return result;
    }

    public async Task<AuthorizeResult?> PollDeviceCodeAsync(DeviceCodeData data, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(data);
        using var response = await HttpRequest.CreatePost(TokenEndpoint)
            .WithContent(new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = data.DeviceCode ?? string.Empty
            }))
            .SendAsync(NetworkService.GetClient(NetworkService.MicrosoftEntraId), cancellationToken: token)
            .ConfigureAwait(false);
        var result = await response.AsJsonAsync<AuthorizeResult>(cancellationToken: token).ConfigureAwait(false);
        result?.Validate();
        return result;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        var oauth = request.OAuthResult;
        if (oauth is null && !string.IsNullOrWhiteSpace(request.RefreshToken))
            oauth = await RefreshOAuthAsync(request.RefreshToken, token).ConfigureAwait(false);
        if (oauth is null && request.DeviceCodeHandler is not null)
        {
            var device = await GetDeviceCodeAsync(token).ConfigureAwait(false);
            oauth = await request.DeviceCodeHandler(
                new DeviceCodeAuthenticationContext(device, pollToken => PollDeviceCodeAsync(device, pollToken)), token)
                .ConfigureAwait(false);
        }
        if (oauth is null)
            throw new IdentityModelConfigurationException("Microsoft login requires an OAuth result or device-code handler.");
        return await CompleteAsync(oauth, token).ConfigureAwait(false);
    }

    public async Task<AuthenticationResult> RefreshAsync(McProfile profile, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(profile.RefreshToken))
            throw new IdentityModelAuthenticationException("invalid_grant", "Microsoft refresh token is empty.");
        return await AuthenticateAsync(new AuthenticationRequest { RefreshToken = profile.RefreshToken }, token)
            .ConfigureAwait(false);
    }

    public async Task<bool> ValidateAsync(McProfile profile, CancellationToken token)
    {
        if (profile.IsExpired || string.IsNullOrWhiteSpace(profile.AccessToken)) return false;
        return await MojangUtils.CheckLicenseAsync(profile, token).ConfigureAwait(false);
    }

    public async Task<AuthorizeResult> RefreshOAuthAsync(string refreshToken, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        using var response = await HttpRequest.CreatePost(TokenEndpoint)
            .WithContent(new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["scope"] = Scope
            }))
            .SendAsync(NetworkService.GetClient(NetworkService.MicrosoftEntraId), cancellationToken: token)
            .ConfigureAwait(false);
        var result = await response.AsJsonAsync<AuthorizeResult>(cancellationToken: token).ConfigureAwait(false)
                     ?? throw new InvalidOperationException("Microsoft token response was empty.");
        if (result.IsError) throw new IdentityModelAuthenticationException(result.Error, result.ErrorDescription);
        result.Validate();
        return result with { RefreshToken = result.RefreshToken ?? refreshToken };
    }

    public async Task<AuthenticationResult> CompleteAsync(AuthorizeResult oauth, CancellationToken token)
    {
        if (oauth.IsError) throw new IdentityModelAuthenticationException(oauth.Error, oauth.ErrorDescription);
        if (string.IsNullOrWhiteSpace(oauth.AccessToken))
            throw new IdentityModelAuthenticationException("invalid_token", "Microsoft OAuth did not return an access token.");

        var xbl = await XboxUtils.AuthenticateAsync(new XboxAuthenticate<XboxProperty>
        {
            Properties = new XboxProperty { RpsTicket = $"d={oauth.AccessToken}" },
            TokenType = "JWT"
        }, isXsts: false, token).ConfigureAwait(false)
                  ?? throw new IdentityModelAuthenticationException("xbox_authentication_failed", "Xbox Live returned no token.");

        var xsts = await XboxUtils.AuthenticateAsync(new XboxAuthenticate<XSTSProperty>
        {
            Properties = new XSTSProperty { UserTokens = [xbl.Token] },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        }, isXsts: true, token).ConfigureAwait(false)
                   ?? throw new IdentityModelAuthenticationException("xsts_authentication_failed", "XSTS returned no token.");

        var userHash = xsts.DisplayClaims?.Xui is { Count: > 0 } ? xsts.DisplayClaims.Xui[0].UserHash : null;
        if (string.IsNullOrWhiteSpace(userHash))
            throw new IdentityModelAuthenticationException("xsts_missing_user_hash", "XSTS response did not contain a user hash.");

        var minecraft = await MojangUtils.AuthenticateAsync(xsts.Token, userHash, token).ConfigureAwait(false)
                        ?? throw new IdentityModelAuthenticationException("minecraft_authentication_failed", "Minecraft authentication returned no token.");
        if (string.IsNullOrWhiteSpace(minecraft.AccessToken))
            throw new IdentityModelAuthenticationException("minecraft_authentication_failed", "Minecraft access token is empty.");

        var safe = new SafeProfile
        {
            UserName = minecraft.UserName,
            AccessToken = minecraft.AccessToken,
            Uuid = string.Empty,
            TokenType = minecraft.TokenType
        };
        if (!await MojangUtils.CheckLicenseAsync(safe, token).ConfigureAwait(false))
            throw new IdentityModelAuthenticationException("minecraft_not_owned", "The Microsoft account does not own Minecraft.");

        var profile = await MojangUtils.GetProfileAsync(safe, token).ConfigureAwait(false);
        return new AuthenticationResult
        {
            ProfileType = ProfileType.Microsoft,
            UserName = profile.Name,
            Uuid = profile.Uuid,
            AccessToken = minecraft.AccessToken,
            RefreshToken = oauth.RefreshToken ?? string.Empty,
            ClientToken = profile.Uuid,
            TokenType = minecraft.TokenType,
            ExpiresAt = minecraft.ExpiresIn > 0 ? DateTimeOffset.UtcNow.AddSeconds(minecraft.ExpiresIn) : null,
            RawJson = System.Text.Json.JsonSerializer.Serialize(profile),
            Provider = "microsoft"
        };
    }
}
