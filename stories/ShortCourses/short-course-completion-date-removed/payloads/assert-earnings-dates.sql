SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseEpisode]
WHERE [Ukprn] = 10005077
  AND [IsRemoved] = 0
  AND [StartDate] = '2025-08-01'
  AND [CompletionDate] IS NULL
