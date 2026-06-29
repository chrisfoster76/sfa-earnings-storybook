SELECT COUNT(*) AS [Count]
FROM [Domain].[ShortCourseEpisode] [sce]
INNER JOIN [Domain].[ShortCourseLearning] [scl] ON [scl].[LearningKey] = [sce].[LearningKey]
WHERE [sce].[Ukprn] IN (10005077, 10005078)
  AND [scl].[TrainingCode] = 'ZSC00001'
