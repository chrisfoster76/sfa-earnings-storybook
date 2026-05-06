SELECT CONVERT(INT,[Amount])
FROM [Domain].[ApprenticeshipInstalment]
WHERE [Type] = 'Balancing'
AND [DeliveryPeriod] = 12
