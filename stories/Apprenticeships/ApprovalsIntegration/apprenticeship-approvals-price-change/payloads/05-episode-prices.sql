SELECT
    COUNT(*) AS PriceCount,
    CONVERT(VARCHAR(10), MIN(ep.[StartDate]), 23) AS FirstPriceStartDate,
    CONVERT(VARCHAR(10), MAX(ep.[StartDate]), 23) AS LastPriceStartDate,
    CONVERT(INT, MAX(ep.[TrainingPrice])) AS LatestTrainingPrice,
    CONVERT(INT, MAX(ep.[EndPointAssessmentPrice])) AS LatestEndPointAssessmentPrice
FROM [ApprenticeshipEpisode] ae
INNER JOIN [EpisodePrice] ep ON ep.[EpisodeKey] = ae.[Key]
WHERE ae.[ApprovalsApprenticeshipId] = 200031
