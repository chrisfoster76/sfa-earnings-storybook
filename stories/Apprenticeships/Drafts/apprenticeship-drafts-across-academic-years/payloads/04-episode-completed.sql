SELECT
    ae.[IsApproved] AS IsApproved,
    ae.[IsRemoved] AS IsRemoved,
    CONVERT(VARCHAR(10), ae.[CompletionDate], 23) AS CompletionDate
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444450'
AND RTRIM(al.[TrainingCode]) = '21'
