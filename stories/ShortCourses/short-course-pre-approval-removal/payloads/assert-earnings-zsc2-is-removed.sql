SELECT TOP 1 [sce].[IsRemoved]
FROM [Domain].[ShortCourseEpisode] [sce]
INNER JOIN [Domain].[ShortCourseLearning] [scl] ON [scl].[LearningKey] = [sce].[LearningKey]
WHERE [scl].[TrainingCode] = 'ZSC00002'
