SELECT CONVERT(INT,[Amount])
FROM [Domain].[ApprenticeshipInstalment]
WHERE [Type] = 'Completion'
AND [DeliveryPeriod] = 12
