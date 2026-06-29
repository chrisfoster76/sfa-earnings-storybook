SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseEpisode]
WHERE [StartDate] = '2026-01-01'
  AND [IsRemoved] = 0
