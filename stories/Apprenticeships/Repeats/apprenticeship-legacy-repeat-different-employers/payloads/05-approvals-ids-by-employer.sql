SELECT
    MAX(CASE WHEN ae.[EmployerAccountId] = 111 THEN ae.[ApprovalsApprenticeshipId] END) AS EmployerA_ApprovalsId,
    MAX(CASE WHEN ae.[EmployerAccountId] = 222 THEN ae.[ApprovalsApprenticeshipId] END) AS EmployerB_ApprovalsId,
    MAX(CASE WHEN ae.[EmployerAccountId] = 333 THEN ae.[ApprovalsApprenticeshipId] END) AS EmployerC_ApprovalsId
FROM [ApprenticeshipEpisode] ae
INNER JOIN [ApprenticeshipLearning] al ON al.[Key] = ae.[LearningKey]
INNER JOIN [Learner] l ON l.[Key] = al.[LearnerKey]
WHERE l.[Uln] = '55555555'
