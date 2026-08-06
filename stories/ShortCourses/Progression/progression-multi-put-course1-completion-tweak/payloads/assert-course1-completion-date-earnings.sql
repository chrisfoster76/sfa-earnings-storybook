SELECT TOP 1 CONVERT(VARCHAR(10), [sce].[CompletionDate], 120) AS [CompletionDate]
FROM [Domain].[ShortCourseEpisode] [sce]
INNER JOIN [Domain].[ShortCourseLearning] [scl] ON [scl].[LearningKey] = [sce].[LearningKey]
WHERE [scl].[TrainingCode] = 'ZSC00001'
  AND [sce].[IsRemoved] = 0
