using DragaliaAPI.Extensions;
using DragaliaAPI.Features.Dungeon.Start;
using DragaliaAPI.Models.Generated;
using DragaliaAPI.Models.Options;
using DragaliaAPI.Shared.Definitions.Enums.Dungeon;
using DragaliaAPI.Shared.PlayerDetails;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DragaliaAPI.Features.Dungeon.AutoRepeat;

public class AutoRepeatService(
    IDistributedCache distributedCache,
    IPlayerIdentityService playerIdentityService,
    IOptionsMonitor<RedisOptions> options,
    ILogger<AutoRepeatService> logger
) : IAutoRepeatService
{
    private readonly IDistributedCache distributedCache = distributedCache;
    private readonly IPlayerIdentityService playerIdentityService = playerIdentityService;

    private readonly DistributedCacheEntryOptions cacheOptions =
        new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(
                options.CurrentValue.AutoRepeatExpiryTimeMinutes
            )
        };

    public async Task SetRepeatSetting(RepeatSetting repeatSetting)
    {
        RepeatInfo info =
            new()
            {
                Key = Guid.NewGuid(),
                Type = repeatSetting.repeat_type,
                UseItemList = repeatSetting.use_item_list,
                MaxCount = repeatSetting.repeat_count,
                CurrentCount = 0
            };

        logger.LogDebug("Saving auto-repeat setting: {@info}", info);

        await this.WriteRepeatInfo(info);
    }

    public async Task<RepeatData?> RecordRepeat(
        string? repeatKey,
        IngameResultData ingameResultData,
        UpdateDataList updateDataList
    )
    {
        RepeatInfo? info;

        if (repeatKey != null)
        {
            info = await this.GetRepeatInfo(Guid.Parse(repeatKey));
            if (info == null)
            {
                logger.LogWarning("Invalid repeat key: {@key}", repeatKey);
                return null;
            }

            logger.LogTrace("Found repeat info: {@info}", info);
        }
        else
        {
            /*
             * The repeat_key is null on the first auto-repeat iteration before the client knows the repeat_key.
             * During the first auto repeat, it may have been configured in /dungeon_start/start, or during the quest.
             *
             * In the former case, we need to retrieve the pre-configured settings from the start request,
             * and in the latter case we need to create default settings.
             */

            info = await this.GetRepeatInfo();
            logger.LogTrace("Repeat key was null, found repeat info {@info}", info);

            info ??= CreateDefaultRepeatInfo();
        }

        info.CurrentCount += 1;
        info.IngameResultData = info.IngameResultData.CombineWith(ingameResultData);
        info.UpdateDataList = info.UpdateDataList.CombineWith(updateDataList);

        await this.WriteRepeatInfo(info);

        return new RepeatData()
        {
            repeat_count = info.CurrentCount,
            repeat_key = info.Key.ToString(),
            repeat_state = 1,
        };
    }

    public async Task<RepeatInfo?> GetRepeatInfo()
    {
        string? key = await this.distributedCache.GetStringAsync(
            Schema.RepeatKey(this.playerIdentityService.ViewerId)
        );
        if (key == null)
            return null;

        return await this.GetRepeatInfo(Guid.Parse(key));
    }

    public async Task<RepeatInfo?> ClearRepeatInfo()
    {
        RepeatInfo? info = await this.GetRepeatInfo();
        if (info != null)
            await this.distributedCache.RemoveAsync(Schema.RepeatInfo(info.Key));

        await this.distributedCache.RemoveAsync(
            Schema.RepeatKey(this.playerIdentityService.ViewerId)
        );

        return info;
    }

    /// <summary>
    /// Attempts to get a player's <see cref="RepeatInfo"/> via key lookup.
    /// </summary>
    /// <param name="key">The <see cref="RepeatInfo"/>'s key.</param>
    /// <returns>The <see cref="RepeatInfo"/>, or <see langword="null"/> if none was found.</returns>
    private Task<RepeatInfo?> GetRepeatInfo(Guid key) =>
        this.distributedCache.GetJsonAsync<RepeatInfo>(Schema.RepeatInfo(key));

    private static class Schema
    {
        public static string RepeatKey(long viewerId) => $":autorepeat:{viewerId}";

        public static string RepeatInfo(Guid repeatKey) => $":autorepeat:{repeatKey}";
    }

    private static RepeatInfo CreateDefaultRepeatInfo() =>
        new()
        {
            Key = Guid.NewGuid(),
            Type = RepeatSettingType.Specified,
            UseItemList = [],
            MaxCount = 99,
        };

    private async Task WriteRepeatInfo(RepeatInfo info)
    {
        await this.distributedCache.SetStringAsync(
            Schema.RepeatKey(this.playerIdentityService.ViewerId),
            info.Key.ToString(),
            this.cacheOptions
        );

        await this.distributedCache.SetJsonAsync(
            Schema.RepeatInfo(info.Key),
            info,
            this.cacheOptions
        );
    }
}
