SELECT CONVERT(VARCHAR(10), [sce].[StartDate], 23) AS [StartDate],
       CONVERT(VARCHAR(10), [sce].[CompletionDate], 23) AS [CompletionDate]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = '23456734'
  AND [scl].[TrainingCode] = 'ZSC00002'
