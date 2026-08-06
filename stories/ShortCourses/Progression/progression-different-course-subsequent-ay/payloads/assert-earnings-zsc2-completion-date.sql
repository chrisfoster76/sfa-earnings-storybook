SELECT CONVERT(VARCHAR(10), [CompletionDate], 23) AS [CompletionDate]
FROM [Domain].[ShortCourseEpisode]
WHERE [Ukprn] = 10005078
  AND [StartDate] = '2026-09-01'
  AND [IsRemoved] = 0
