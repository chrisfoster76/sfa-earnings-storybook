SELECT
    COUNT(CASE WHEN h.[Operation] = 'Updated' THEN 1 END) AS [Updates],
    COUNT(CASE WHEN h.[Operation] = 'Updated' AND h.[Changes] = '["ForceEarningsSync"]' THEN 1 END) AS [ForcedUpdates]
FROM [History].[ApprenticeshipLearningHistory] h
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[LearningKey] = h.[LearningKey]
WHERE ae.[ApprovalsApprenticeshipId] = 200039
