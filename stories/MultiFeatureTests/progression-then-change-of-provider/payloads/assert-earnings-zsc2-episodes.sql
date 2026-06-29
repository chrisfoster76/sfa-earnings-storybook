SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseEpisode]
WHERE [StartDate] IN ('2026-02-01', '2026-04-01')
  AND [IsRemoved] = 0
