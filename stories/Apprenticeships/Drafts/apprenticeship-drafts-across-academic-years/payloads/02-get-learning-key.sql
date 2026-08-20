SELECT TOP 1 al.[Key] AS LearningKey
FROM [ApprenticeshipLearning] al
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444450'
