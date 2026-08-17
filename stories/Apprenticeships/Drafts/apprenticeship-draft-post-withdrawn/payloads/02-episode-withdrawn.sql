SELECT
    ae.[IsApproved] AS IsApproved,
    ae.[IsRemoved] AS IsRemoved,
    CONVERT(VARCHAR(10), ae.[WithdrawalDate], 23) AS WithdrawalDate
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444444'
