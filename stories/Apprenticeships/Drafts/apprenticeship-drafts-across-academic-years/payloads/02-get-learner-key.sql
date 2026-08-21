SELECT TOP 1 al.[LearnerKey] AS LearnerKey
FROM [ApprenticeshipLearning] al
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444450'
