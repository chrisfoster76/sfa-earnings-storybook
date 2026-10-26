SELECT
    CONVERT(VARCHAR(10), MIN(ep.[StartDate]), 23) AS StartDate,
    COUNT(*) AS PriceCount
FROM [ApprenticeshipEpisode] ae
INNER JOIN [EpisodePrice] ep ON ep.[EpisodeKey] = ae.[Key]
WHERE ae.[ApprovalsApprenticeshipId] = 200034
