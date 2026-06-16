SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseEarningsProfile] [ep]
INNER JOIN [Domain].[ShortCourseEpisode] [sce] ON [sce].[Key] = [ep].[EpisodeKey]
WHERE [sce].[Ukprn] = 10005077
  AND [sce].[IsRemoved] = 0
  AND [ep].[IsApproved] = 1
