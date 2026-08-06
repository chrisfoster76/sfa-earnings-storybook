SELECT TOP 1 CONVERT(VARCHAR(10), [sce].[CompletionDate], 120) AS [CompletionDate]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = 23456734
  AND [sce].[UKPRN] = 10005077
  AND [scl].[TrainingCode] = 'ZSC00001'
