SELECT TOP 1
  CONVERT(varchar(10), [sce].[StartDate], 23) AS [StartDate],
  CONVERT(varchar(10), [sce].[WithdrawalDate], 23) AS [WithdrawalDate]
FROM [ShortCourseEpisode] [sce]
INNER JOIN [ShortCourseLearning] [scl] ON [scl].[Key] = [sce].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = '23456734'
  AND [sce].[UKPRN] = 10005077
  AND [scl].[TrainingCode] = 'ZSC00001'
