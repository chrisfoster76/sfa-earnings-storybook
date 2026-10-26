SELECT CONVERT(VARCHAR(10), MIN(ep.[StartDate]), 23) AS StartDate
FROM [EpisodePrice] ep
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[Key] = ep.[EpisodeKey]
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444447'
