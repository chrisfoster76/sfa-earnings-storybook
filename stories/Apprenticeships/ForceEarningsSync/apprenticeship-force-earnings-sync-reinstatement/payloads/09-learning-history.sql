SELECT
    COUNT(CASE WHEN h.[Operation] = 'Updated' AND h.[Changes] LIKE '%"Reinstated"%' AND h.[Changes] LIKE '%"ForceEarningsSync"%' THEN 1 END) AS [ForcedReinstatements]
FROM [History].[ApprenticeshipLearningHistory] h
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[LearningKey] = h.[LearningKey]
WHERE ae.[ApprovalsApprenticeshipId] = 200040
