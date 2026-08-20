SELECT TOP 1 al.[Key] AS LearningKey, al.[LearnerKey] AS LearnerKey
FROM [ApprenticeshipLearning] al
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[LearningKey] = al.[Key]
WHERE ae.[ApprovalsApprenticeshipId] = 400001
