SELECT
    MAX(CASE WHEN RTRIM(al.[TrainingCode]) = '30' THEN CAST(ae.[IsRemoved] AS INT) END) AS Standard30Removed,
    MAX(CASE WHEN RTRIM(al.[TrainingCode]) = '21' THEN CAST(ae.[IsRemoved] AS INT) END) AS Standard21Removed
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444444'
