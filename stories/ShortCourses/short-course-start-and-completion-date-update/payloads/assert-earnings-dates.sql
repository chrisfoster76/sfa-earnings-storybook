SELECT CONVERT(VARCHAR(10), [StartDate], 23) AS [StartDate],
       CONVERT(VARCHAR(10), [CompletionDate], 23) AS [CompletionDate]
FROM [Domain].[ShortCourseEpisode]
WHERE [Ukprn] = 10005077
  AND [IsRemoved] = 0
