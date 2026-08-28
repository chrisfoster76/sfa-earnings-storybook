SELECT
    ae.[IsApproved] AS IsApproved,
    ae.[IsRemoved] AS IsRemoved
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
WHERE ae.[ApprovalsApprenticeshipId] = 200010
