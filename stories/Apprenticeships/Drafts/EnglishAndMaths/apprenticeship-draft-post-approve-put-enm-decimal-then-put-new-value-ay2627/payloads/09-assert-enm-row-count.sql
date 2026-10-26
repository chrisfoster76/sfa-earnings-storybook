SELECT COUNT(*) AS [Count], SUM(CASE WHEN [em].[LearnAimRef] = 'DECIMAL' THEN 1 ELSE 0 END) AS [DecimalCount]
FROM [EnglishAndMaths] [em]
INNER JOIN [ApprenticeshipLearning] [al] ON [al].[Key] = [em].[LearningKey]
INNER JOIN [Learner] [l] ON [l].[Key] = [al].[LearnerKey]
WHERE [l].[Uln] = '44444463'
