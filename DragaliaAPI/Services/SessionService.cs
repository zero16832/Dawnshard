﻿using DragaliaAPI.Models.Database;
using DragaliaAPI.Models.Nintendo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DragaliaAPI.Services;

/// <summary>
/// SessionService interfaces with Redis to store the information about current sessions in-memory.
/// The basic flow looks like this:
/// 
/// 1. The NintendoLoginController calls PrepareSession with DeviceAccount information and an ID
///    token, and a session is created and stored in the cache indexed by the ID token. The
///    controller sends back the ID token.
///    
/// 2. The client *may* later send that ID token in a request to SignupController, in which case it
///    just needs to be sent the ViewerId of the DeviceAccount's associated savefile. This does not
///    involve any cache writes.
///    
/// 3. The client will later send the ID token in a request to AuthController, where ActivateSession
///    is called, which moves the key of the session from the id_token (hereafter unused) to the
///    session ID. The session ID is returned and sent in the response from AuthController.
///    
/// 4. All subsequent requests will contain the session ID in the header, and this can be used to
///    retrieve the savefile and update it if necessary.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IApiRepository _apiRepository;
    private readonly IDistributedCache _cache;

    public SessionService(IApiRepository repository, IDistributedCache cache)
    {
        _apiRepository = repository;
        _cache = cache;
    }

    private static class Schema
    {
        public static string Session_IdToken(string idToken)
            => $":session:id_token:{idToken}";

        public static string Session_SessionId(string sessionId)
            => $":session:session_id:{sessionId}";
        
        public static string SessionId_DeviceAccountId(string deviceAccountId)
            => $":session_id:device_account_id:{deviceAccountId}";
    }

    private static readonly DistributedCacheEntryOptions cacheOptions = new() { SlidingExpiration = TimeSpan.FromMinutes(5) };
    
    public async Task PrepareSession(DeviceAccount deviceAccount, string idToken)
    {
        // Check if there is an existing session, and if so, remove it
        string existingSessionId = await _cache.GetStringAsync(Schema.SessionId_DeviceAccountId(deviceAccount.id));
        if (!string.IsNullOrEmpty(existingSessionId))
        {
            // TODO: Consider abstracting this into a RemoveSession method, in case it needs to be done elsewhere
            await _cache.RemoveAsync(Schema.Session_SessionId(existingSessionId));
            await _cache.RemoveAsync(Schema.SessionId_DeviceAccountId(deviceAccount.id));
        }

        IQueryable<DbPlayerSavefile> savefile = _apiRepository.GetSavefile(deviceAccount.id);
        long viewerId = await savefile.Select(x => x.ViewerId).SingleAsync();
        string sessionId = Guid.NewGuid().ToString();

        Session session = new(sessionId, deviceAccount.id, viewerId);
        await _cache.SetStringAsync(Schema.Session_IdToken(idToken), JsonSerializer.Serialize(session), cacheOptions);
    }

    public async Task<string> ActivateSession(string idToken)
    {
        Session session = await LoadSession(Schema.Session_IdToken(idToken));

        // Move key to sessionId
        await _cache.RemoveAsync(Schema.Session_IdToken(idToken));
        await _cache.SetStringAsync(Schema.Session_SessionId(session.SessionId), JsonSerializer.Serialize(session), cacheOptions);
        // Register in existent sessions
        await _cache.SetStringAsync(Schema.SessionId_DeviceAccountId(session.DeviceAccountId), session.SessionId, cacheOptions);

        return session.SessionId;
    }

    public async Task<bool> ValidateSession(string sessionId)
    {
        string sessionJson = await _cache.GetStringAsync(Schema.Session_SessionId(sessionId));
        return !string.IsNullOrEmpty(sessionJson);
    }

    public async Task<IQueryable<DbPlayerSavefile>> GetSavefile_SessionId(string sessionId)
    {
        Session session = await LoadSession(Schema.Session_SessionId(sessionId));

        return _apiRepository.GetSavefile(session.DeviceAccountId);
    }

    public async Task<IQueryable<DbPlayerSavefile>> GetSavefile_IdToken(string idToken)
    {
        Session session = await LoadSession(Schema.Session_IdToken(idToken));

        return _apiRepository.GetSavefile(session.DeviceAccountId);
    }

    private async Task<Session> LoadSession(string key)
    {
        string sessionJson = await _cache.GetStringAsync(key);
        _cache.Refresh(key);
        if (string.IsNullOrEmpty(sessionJson)) { throw new ArgumentException($"Could not load session for key {key}"); }

        return JsonSerializer.Deserialize<Session>(sessionJson) ?? throw new JsonException($"Loaded session JSON {sessionJson} could not be deserialized.");
    }

    private record Session(string SessionId, string DeviceAccountId, long ViewerId);
}