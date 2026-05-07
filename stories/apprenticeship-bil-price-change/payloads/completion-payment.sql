SELECT CONVERT(INT, [Amount]) AS [Amount], [DeliveryPeriod]
FROM [Domain].[ApprenticeshipInstalment]
WHERE [Type] = 'Completion'
