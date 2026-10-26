SELECT CAST([em].[Amount] AS DECIMAL(15, 2)) AS [Amount]
FROM [EnglishAndMaths] [em]
INNER JOIN [ApprenticeshipLearning] [al] ON [al].[Key] = [em].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [al].[LearnerKey]
WHERE [l].[Uln] = '44444463'
  AND [em].[LearnAimRef] = 'DECIMAL'
