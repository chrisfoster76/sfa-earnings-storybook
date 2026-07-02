SELECT TOP 1 [sce].[IsRemoved] AS [IsRemoved]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = 23456734
  AND [sce].[UKPRN] = 10005077
  AND [sce].[TrainingCode] = 'ZSC00001'
