SELECT CONVERT(INT, [Amount]) AS [Amount], [DeliveryPeriod]
FROM [Domain].[ApprenticeshipAdditionalPayment]
WHERE [AdditionalPaymentType] = 'ProviderIncentive' AND [AcademicYear] = 2526
