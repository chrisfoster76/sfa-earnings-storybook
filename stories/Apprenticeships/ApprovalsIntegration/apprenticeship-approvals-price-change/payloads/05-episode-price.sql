SELECT
    CONVERT(VARCHAR(10), MIN(ep.[StartDate]), 23) AS StartDate,
    COUNT(*) AS PriceCount,
    CONVERT(INT, MIN(ep.[TrainingPrice])) AS TrainingPrice,
    CONVERT(INT, MIN(ep.[EndPointAssessmentPrice])) AS EndPointAssessmentPrice,
    CONVERT(INT, MIN(ep.[TotalPrice])) AS TotalPrice
FROM [ApprenticeshipEpisode] ae
INNER JOIN [EpisodePrice] ep ON ep.[EpisodeKey] = ae.[Key]
WHERE ae.[ApprovalsApprenticeshipId] = 200031
