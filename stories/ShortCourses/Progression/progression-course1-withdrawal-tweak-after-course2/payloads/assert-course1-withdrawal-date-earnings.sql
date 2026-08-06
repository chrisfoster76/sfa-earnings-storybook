SELECT TOP 1 CONVERT(VARCHAR(10), [sce].[WithdrawalDate], 120) AS [WithdrawalDate]
FROM [Domain].[ShortCourseEpisode] [sce]
INNER JOIN [Domain].[ShortCourseLearning] [scl] ON [scl].[LearningKey] = [sce].[LearningKey]
WHERE [scl].[TrainingCode] = 'ZSC00001'
  AND [sce].[IsRemoved] = 0
