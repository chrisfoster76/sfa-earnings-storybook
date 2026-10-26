SELECT TOP 1
    CONVERT(INT, ep.[TrainingPrice]) AS TrainingPrice,
    CONVERT(VARCHAR(10), ep.[StartDate], 23) AS StartDate
FROM [EpisodePrice] ep
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[Key] = ep.[EpisodeKey]
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444461'
ORDER BY ep.[StartDate] ASC
