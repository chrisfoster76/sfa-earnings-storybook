SELECT CONVERT(INT, [Amount]) AS [Amount], [DeliveryPeriod]
FROM [Domain].[ApprenticeshipAdditionalPayment]
WHERE [AdditionalPaymentType] = 'EmployerIncentive' AND [AcademicYear] = 2627
