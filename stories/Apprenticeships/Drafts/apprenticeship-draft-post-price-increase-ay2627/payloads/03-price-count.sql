SELECT COUNT(*) AS Count
FROM [EpisodePrice] ep
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[Key] = ep.[EpisodeKey]
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444461'
