SELECT TOP 1 al.[LearnerKey] AS LearnerKey, al.[Key] AS LearningKey
FROM [ApprenticeshipLearning] al
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[LearningKey] = al.[Key]
WHERE ae.[ApprovalsApprenticeshipId] = 200037
