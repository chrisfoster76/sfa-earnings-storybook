SELECT
    SUM(CASE WHEN ae.[IsApproved] = 1 THEN 1 ELSE 0 END) AS ApprovedCount,
    SUM(CASE WHEN ae.[IsApproved] = 0 THEN 1 ELSE 0 END) AS UnapprovedCount
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444444'
