SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseEpisode]
WHERE [StartDate] = '2025-10-01'
  AND [IsRemoved] = 0
