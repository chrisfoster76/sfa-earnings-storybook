SELECT TOP 1
  [sce].[UKPRN] AS [Ukprn],
  CONVERT(varchar(10), [sce].[CompletionDate], 23) AS [CompletionDate]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = '23456734'
  AND [scl].[TrainingCode] = 'ZSC00001'
