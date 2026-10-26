SELECT COUNT(*) AS Count
FROM [ApprenticeshipLearning] al
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444444'
