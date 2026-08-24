SELECT TOP 1
    ae.[IsApproved] AS IsApproved,
    ae.[ApprovalsApprenticeshipId] AS ApprovalsApprenticeshipId
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444444'
  AND RTRIM(ae.[TrainingCode]) = '30'
