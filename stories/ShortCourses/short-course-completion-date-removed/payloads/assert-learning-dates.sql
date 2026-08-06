SELECT COUNT(*) AS [Count]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = '23456734'
  AND [scl].[TrainingCode] = 'ZSC00001'
  AND [sce].[StartDate] = '2025-08-01'
  AND [sce].[CompletionDate] IS NULL
