SELECT TOP 1 [DeliveryPeriod]
FROM [Domain].[ShortCourseInstalment]
WHERE [Type] = 'LearningComplete'
  AND [IsPayable] = 1
