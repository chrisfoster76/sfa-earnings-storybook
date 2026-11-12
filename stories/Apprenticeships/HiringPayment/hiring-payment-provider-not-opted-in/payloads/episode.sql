SELECT
    CONVERT(INT, e.[UsingEarnAndPayCalc]) AS [UsingEarnAndPayCalc],
    e.[FundingPlatform] AS [FundingPlatform],
    CONVERT(INT, p.[IsApproved]) AS [IsApproved],
    (SELECT COUNT(*) FROM [Domain].[ApprenticeshipInstalment] i WHERE i.[EarningsProfileId] = p.[EarningsProfileId]) AS [Instalments]
FROM [Domain].[ApprenticeshipEpisode] e
INNER JOIN [Domain].[ApprenticeshipLearning] l ON l.[LearningKey] = e.[LearningKey]
INNER JOIN [Domain].[ApprenticeshipEarningsProfile] p ON p.[EpisodeKey] = e.[Key]
WHERE l.[Uln] = '11111202'
