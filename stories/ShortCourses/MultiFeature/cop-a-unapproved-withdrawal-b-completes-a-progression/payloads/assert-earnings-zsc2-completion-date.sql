SELECT CONVERT(VARCHAR(10), [CompletionDate], 23) AS [CompletionDate]
FROM [Domain].[ShortCourseEpisode]
WHERE [StartDate] = '2025-10-01'
  AND [IsRemoved] = 0
