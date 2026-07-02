SELECT TOP 1 [sce].[IsRemoved]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
WHERE [scl].[TrainingCode] = 'ZSC00002'
