SELECT TOP 1 [l].[Key] AS [LearnerKey]
FROM [ShortCourseLearning] [scl]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
INNER JOIN [ShortCourseEpisode] [sce] ON [sce].[LearningKey] = [scl].[Key]
WHERE [l].[Uln] = 7430758303
  AND [sce].[UKPRN] = 10005077
  AND [scl].[TrainingCode] = 'ZSC00004'
  AND [sce].[StartDate] = '2026-06-22'