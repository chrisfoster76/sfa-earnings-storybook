SELECT
    SUM(CASE WHEN ap.[AdditionalPaymentType] = 'EmployerHiringPayment' THEN 1 ELSE 0 END) AS [HiringPayments],
    SUM(CASE WHEN ap.[AdditionalPaymentType] = 'EmployerHiringPayment' AND ap.[AcademicYear] = 2627 AND ap.[DeliveryPeriod] = 5 AND CONVERT(INT, ap.[Amount]) = 1000 THEN 1 ELSE 0 END) AS [Day90],
    SUM(CASE WHEN ap.[AdditionalPaymentType] = 'EmployerHiringPayment' AND ap.[AcademicYear] = 2728 AND ap.[DeliveryPeriod] = 2 AND CONVERT(INT, ap.[Amount]) = 1000 THEN 1 ELSE 0 END) AS [Day365],
    SUM(CASE WHEN ap.[AdditionalPaymentType] IN ('ProviderIncentive', 'EmployerIncentive') THEN 1 ELSE 0 END) AS [Incentives]
FROM [Domain].[ApprenticeshipEpisode] e
INNER JOIN [Domain].[ApprenticeshipLearning] l ON l.[LearningKey] = e.[LearningKey]
INNER JOIN [Domain].[ApprenticeshipEarningsProfile] p ON p.[EpisodeKey] = e.[Key]
INNER JOIN [Domain].[ApprenticeshipAdditionalPayment] ap ON ap.[EarningsProfileId] = p.[EarningsProfileId]
WHERE l.[Uln] = '11111201'
