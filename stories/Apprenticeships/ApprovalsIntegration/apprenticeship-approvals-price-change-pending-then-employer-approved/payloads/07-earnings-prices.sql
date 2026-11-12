SELECT
    COUNT(*) AS PriceCount,
    CONVERT(VARCHAR(10), MAX(ep.[StartDate]), 23) AS LastPriceStartDate,
    CONVERT(INT, MAX(ep.[AgreedPrice])) AS LatestAgreedPrice
FROM [Domain].[ApprenticeshipEpisodePrice] ep
