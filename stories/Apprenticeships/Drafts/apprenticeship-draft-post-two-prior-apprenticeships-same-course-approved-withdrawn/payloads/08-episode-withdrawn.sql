SELECT COUNT(*) AS [Count]
FROM [ApprenticeshipEpisode] [ae]
INNER JOIN [ApprenticeshipLearning] [al] ON [al].[Key] = [ae].[LearningKey]
WHERE [al].[TrainingCode] = '21'
  AND [ae].[IsApproved] = 1
  AND [ae].[IsRemoved] = 0
  AND [ae].[WithdrawalDate] IS NULL
