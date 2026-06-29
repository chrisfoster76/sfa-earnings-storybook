SELECT COUNT(*) AS [Count]
FROM [ShortCourseLearning] [scl]
INNER JOIN [Learner] [l] ON [l].[Key] = [scl].[LearnerKey]
WHERE [l].[Uln] = '23456734'
