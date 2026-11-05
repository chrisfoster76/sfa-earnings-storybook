SELECT
    COUNT(CASE WHEN h.[Operation] = 'Updated' THEN 1 END) AS [Updates],
    COUNT(CASE WHEN h.[Operation] = 'Updated' AND h.[Changes] LIKE '%"ExpectedEndDate"%' AND h.[Changes] LIKE '%"EnglishAndMaths"%' AND h.[Changes] LIKE '%"ForceEarningsSync"%' THEN 1 END) AS [ForcedChangedUpdates]
FROM [History].[ApprenticeshipLearningHistory] h
INNER JOIN [ApprenticeshipEpisode] ae ON ae.[LearningKey] = h.[LearningKey]
WHERE ae.[ApprovalsApprenticeshipId] = 200042
