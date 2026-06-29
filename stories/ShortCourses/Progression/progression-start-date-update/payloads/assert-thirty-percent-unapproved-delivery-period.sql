SELECT TOP 1 [DeliveryPeriod]
FROM [Domain].[ShortCourseInstalment]
WHERE [Type] = 'ThirtyPercentLearningComplete'
  AND [IsPayable] = 0
