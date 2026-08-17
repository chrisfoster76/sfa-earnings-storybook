SELECT
    ae.[IsApproved] AS IsApproved,
    ae.[ApprovalsApprenticeshipId] AS ApprovalsApprenticeshipId,
    ae.[LegalEntityName] AS LegalEntityName
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '44444444'
