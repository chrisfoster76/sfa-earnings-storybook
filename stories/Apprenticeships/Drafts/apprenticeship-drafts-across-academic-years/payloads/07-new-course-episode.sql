SELECT
    RTRIM(ae.[TrainingCode]) AS TrainingCode,
    ae.[IsApproved] AS IsApproved,
    ae.[IsRemoved] AS IsRemoved,
    CONVERT(VARCHAR(10), MIN(ep.[StartDate]), 23) AS StartDate
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
INNER JOIN [EpisodePrice] ep ON ep.[EpisodeKey] = ae.[Key]
WHERE l.[Uln] = '44444450'
AND RTRIM(ae.[TrainingCode]) = '30'
GROUP BY ae.[TrainingCode], ae.[IsApproved], ae.[IsRemoved]
